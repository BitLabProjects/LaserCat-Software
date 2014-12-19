using bitLab.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.LaserCat.Grbl
{
	public enum EPlannerBlockChangedState
	{
		Reset, BlockAdded, BlockRemoved
	}

	public class CPlannerBlocksChangedEventArgs : EventArgs
	{
		public EPlannerBlockChangedState PlannerBlocksChangedState;
    public DblPoint3 Target;
	}

	unsafe public partial class GrblFirmware
	{
		// The number of linear motions that can be in the plan at any give time
		//#ifndef BLOCK_BUFFER_SIZE
		//  #ifdef USE_LINE_NUMBERS
		//    #define BLOCK_BUFFER_SIZE 16
		//  #else
		//    #define BLOCK_BUFFER_SIZE 18
		//  #endif
		//#endif
		//public const int BLOCK_BUFFER_SIZE = 18;

		//SB! Added event to notify planner blocks changes
		public event EventHandler<CPlannerBlocksChangedEventArgs> PlannerBlocksChanged;
    private void RaisePlannerBlocksChanged(EPlannerBlockChangedState state)
    {
      RaisePlannerBlocksChanged(state, new DblPoint3());
    }
    private void RaisePlannerBlocksChanged(EPlannerBlockChangedState state, DblPoint3 target)
		{
			if (PlannerBlocksChanged != null)
        PlannerBlocksChanged(this, new CPlannerBlocksChangedEventArgs() { PlannerBlocksChangedState = state, Target = target });
		}

		public class plan_block_t
		{
			// Fields used by the bresenham algorithm for tracing the line
			// NOTE: Used by stepper algorithm to execute the block correctly. Do not alter these values.
			public byte direction_bits;    // The direction bit set for this block (refers to *_DIRECTION_BIT in config.h)
			public uint[] steps;    // Step count along each axis
			public uint step_event_count; // The maximum step axis count and number of steps required to complete this block. 

			// Fields used by the motion planner to manage acceleration
			public float entry_speed_sqr;         // The current planned entry speed at block junction in (mm/min)^2
			public float max_entry_speed_sqr;     // Maximum allowable entry speed based on the minimum of junction limit and 
			//   neighboring nominal speeds with overrides in (mm/min)^2
			public float max_junction_speed_sqr;  // Junction entry speed limit based on direction vectors in (mm/min)^2
			public float nominal_speed_sqr;       // Axis-limit adjusted nominal speed for this block in (mm/min)^2
			public float acceleration;            // Axis-limit adjusted line acceleration in (mm/min^2)
			public float millimeters;             // The remaining distance for this block to be executed in (mm)
			// byte max_override;       // Maximum override value based on axis speed limits
			public int line_number;

			public plan_block_t(bool dummy)
			{
				direction_bits = 0;    // The direction bit set for this block (refers to *_DIRECTION_BIT in config.h)
				steps = new uint[NutsAndBolts.N_AXIS];    // Step count along each axis
				step_event_count = 0; // The maximum step axis count and number of steps required to complete this block. 
				entry_speed_sqr = 0f;         // The current planned entry speed at block junction in (mm/min)^2
				max_entry_speed_sqr = 0f;     // Maximum allowable entry speed based on the minimum of junction limit and 
				max_junction_speed_sqr = 0f;  // Junction entry speed limit based on direction vectors in (mm/min)^2
				nominal_speed_sqr = 0f;       // Axis-limit adjusted nominal speed for this block in (mm/min)^2
				acceleration = 0f;            // Axis-limit adjusted line acceleration in (mm/min^2)
				millimeters = 0f;             // The remaining distance for this block to be executed in (mm)
				line_number = 0;
			}
		} ;

		public const float SOME_LARGE_VALUE = 1.0E+38f; // Used by rapids and acceleration maximization calculations. Just needs
		// to be larger than any feasible (mm/min)^2 or mm/sec^2 value.

		private static plan_block_t[] CreateBlockBufferArray(int count)
		{
			var array = new plan_block_t[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = new plan_block_t(true);
			}
			return array;
		}

		//static plan_block_t[] block_buffer = CreateBlockBufferArray(BLOCK_BUFFER_SIZE);  // A ring buffer for motion instructions
    List<plan_block_t> block_buffer = new List<plan_block_t>();
		int block_buffer_tail;     // Index of the block to process now
		int block_buffer_head;     // Index of the next block to be pushed
		int next_buffer_head;      // Index of the next buffer head
		int block_buffer_planned;  // Index of the optimally planned block

		// Define planner variables
		public struct planner_t
		{
			public int[] position;          // The planner position of the tool in absolute steps. Kept separate
			// from g-code position for movements requiring multiple line motions,
			// i.e. arcs, canned cycles, and backlash compensation.
			public float[] previous_unit_vec;   // Unit vector of previous path line segment
			public float previous_nominal_speed_sqr;  // Nominal speed of previous path line segment

			public planner_t(bool dummy)
			{
				position = new int[NutsAndBolts.N_AXIS];
				previous_unit_vec = new float[NutsAndBolts.N_AXIS];
				previous_nominal_speed_sqr = 0.0f;
			}
		};
		static planner_t pl = new planner_t(true);


		// Returns the index of the next block in the ring buffer. Also called by stepper segment buffer.
		public int plan_next_block_index(int block_index)
		{
      return block_index + 1;
		}


		// Returns the index of the previous block in the ring buffer
		int plan_prev_block_index(int block_index)
		{
			block_index--;
			return (block_index);
		}


		/*                            PLANNER SPEED DEFINITION                                              
																				 +--------+   <- current->nominal_speed
																				/          \                                
						 current->entry_speed ->   +            \                               
																			 |             + <- next->entry_speed (aka exit speed)
																			 +-------------+                              
																					 time -->                      
                                                  
			Recalculates the motion plan according to the following basic guidelines:
  
				1. Go over every feasible block sequentially in reverse order and calculate the junction speeds
						(i.e. current->entry_speed) such that:
					a. No junction speed exceeds the pre-computed maximum junction speed limit or nominal speeds of 
						 neighboring blocks.
					b. A block entry speed cannot exceed one reverse-computed from its exit speed (next->entry_speed)
						 with a maximum allowable deceleration over the block travel distance.
					c. The last (or newest appended) block is planned from a complete stop (an exit speed of zero).
				2. Go over every block in chronological (forward) order and dial down junction speed values if 
					a. The exit speed exceeds the one forward-computed from its entry speed with the maximum allowable
						 acceleration over the block travel distance.
  
			When these stages are complete, the planner will have maximized the velocity profiles throughout the all
			of the planner blocks, where every block is operating at its maximum allowable acceleration limits. In 
			other words, for all of the blocks in the planner, the plan is optimal and no further speed improvements
			are possible. If a new block is added to the buffer, the plan is recomputed according to the said 
			guidelines for a new optimal plan.
  
			To increase computational efficiency of these guidelines, a set of planner block pointers have been
			created to indicate stop-compute points for when the planner guidelines cannot logically make any further
			changes or improvements to the plan when in normal operation and new blocks are streamed and added to the
			planner buffer. For example, if a subset of sequential blocks in the planner have been planned and are 
			bracketed by junction velocities at their maximums (or by the first planner block as well), no new block
			added to the planner buffer will alter the velocity profiles within them. So we no longer have to compute
			them. Or, if a set of sequential blocks from the first block in the planner (or a optimal stop-compute
			point) are all accelerating, they are all optimal and can not be altered by a new block added to the
			planner buffer, as this will only further increase the plan speed to chronological blocks until a maximum
			junction velocity is reached. However, if the operational conditions of the plan changes from infrequently
			used feed holds or feedrate overrides, the stop-compute pointers will be reset and the entire plan is  
			recomputed as stated in the general guidelines.
  
			Planner buffer index mapping:
			- block_buffer_tail: Points to the beginning of the planner buffer. First to be executed or being executed. 
			- block_buffer_head: Points to the buffer block after the last block in the buffer. Used to indicate whether
					the buffer is full or empty. As described for standard ring buffers, this block is always empty.
			- next_buffer_head: Points to next planner buffer block after the buffer head block. When equal to the 
					buffer tail, this indicates the buffer is full.
			- block_buffer_planned: Points to the first buffer block after the last optimally planned block for normal
					streaming operating conditions. Use for planning optimizations by avoiding recomputing parts of the 
					planner buffer that don't change with the addition of a new block, as describe above. In addition, 
					this block can never be less than block_buffer_tail and will always be pushed forward and maintain 
					this requirement when encountered by the plan_discard_current_block() routine during a cycle.
  
			NOTE: Since the planner only computes on what's in the planner buffer, some motions with lots of short 
			line segments, like G2/3 arcs or complex curves, may seem to move slow. This is because there simply isn't
			enough combined distance traveled in the entire buffer to accelerate up to the nominal speed and then 
			decelerate to a complete stop at the end of the buffer, as stated by the guidelines. If this happens and
			becomes an annoyance, there are a few simple solutions: (1) Maximize the machine acceleration. The planner
			will be able to compute higher velocity profiles within the same combined distance. (2) Maximize line 
			motion(s) distance per block to a desired tolerance. The more combined distance the planner has to use,
			the faster it can go. (3) Maximize the planner buffer size. This also will increase the combined distance
			for the planner to compute over. It also increases the number of computations the planner has to perform
			to compute an optimal plan, so select carefully. The Arduino 328p memory is already maxed out, but future
			ARM versions should have enough memory and speed for look-ahead blocks numbering up to a hundred or more.

		*/
		public void planner_recalculate()
		{
			// Initialize block index to the last block in the planner buffer.
			int block_index = plan_prev_block_index(block_buffer_head);

			// Bail. Can't do anything with one only one plan-able block.
			if (block_index == block_buffer_planned) { return; }

			// Reverse Pass: Coarsely maximize all possible deceleration curves back-planning from the last
			// block in buffer. Cease planning when the last optimal planned or tail pointer is reached.
			// NOTE: Forward pass will later refine and correct the reverse pass to create an optimal plan.
			float entry_speed_sqr;
			//plan_block_t *next;
			//plan_block_t *current = &block_buffer[block_index];
			int currentIdx = block_index;
			int nextIdx = -1;

			// Calculate maximum entry speed for last block in buffer, where the exit speed is always zero.
			block_buffer[currentIdx].entry_speed_sqr = (float)System.Math.Min(block_buffer[currentIdx].max_entry_speed_sqr, 2 * block_buffer[currentIdx].acceleration * block_buffer[currentIdx].millimeters);

			block_index = plan_prev_block_index(block_index);
			if (block_index == block_buffer_planned)
			{ // Only two plannable blocks in buffer. Reverse pass complete.
				// Check if the first block is the tail. If so, notify stepper to update its current parameters.
				if (block_index == block_buffer_tail) { st_update_plan_block_parameters(); }
			}
			else
			{ // Three or more plan-able blocks
				while (block_index != block_buffer_planned)
				{
					//next = current;
					//current = &block_buffer[block_index];
					nextIdx = currentIdx;
					currentIdx = block_index;

					block_index = plan_prev_block_index(block_index);

					// Check if next block is the tail block(=planned block). If so, update current stepper parameters.
					if (block_index == block_buffer_tail) { st_update_plan_block_parameters(); }

					// Compute maximum entry speed decelerating over the current block from its exit speed.
					if (block_buffer[currentIdx].entry_speed_sqr != block_buffer[currentIdx].max_entry_speed_sqr)
					{
						entry_speed_sqr = block_buffer[nextIdx].entry_speed_sqr + 2 * block_buffer[currentIdx].acceleration * block_buffer[currentIdx].millimeters;
						if (entry_speed_sqr < block_buffer[currentIdx].max_entry_speed_sqr)
						{
							block_buffer[currentIdx].entry_speed_sqr = entry_speed_sqr;
						}
						else
						{
							block_buffer[currentIdx].entry_speed_sqr = block_buffer[currentIdx].max_entry_speed_sqr;
						}
					}
				}
			}

			// Forward Pass: Forward plan the acceleration curve from the planned pointer onward.
			// Also scans for optimal plan breakpoints and appropriately updates the planned pointer.
			//next = &block_buffer[block_buffer_planned]; // Begin at buffer planned pointer
			nextIdx = block_buffer_planned;

			block_index = plan_next_block_index(block_buffer_planned);
			while (block_index != block_buffer_head)
			{
				//current = next;
				//next = &block_buffer[block_index];
				currentIdx = nextIdx;
				nextIdx = block_index;

				// Any acceleration detected in the forward pass automatically moves the optimal planned
				// pointer forward, since everything before this is all optimal. In other words, nothing
				// can improve the plan from the buffer tail to the planned pointer by logic.
				if (block_buffer[currentIdx].entry_speed_sqr < block_buffer[nextIdx].entry_speed_sqr)
				{
					entry_speed_sqr = block_buffer[currentIdx].entry_speed_sqr + 2 * block_buffer[currentIdx].acceleration * block_buffer[currentIdx].millimeters;
					// If true, current block is full-acceleration and we can move the planned pointer forward.
					if (entry_speed_sqr < block_buffer[nextIdx].entry_speed_sqr)
					{
						block_buffer[nextIdx].entry_speed_sqr = entry_speed_sqr; // Always <= max_entry_speed_sqr. Backward pass sets this.
						block_buffer_planned = block_index; // Set optimal plan pointer.
					}
				}

				// Any block set at its maximum entry speed also creates an optimal plan up to this
				// point in the buffer. When the plan is bracketed by either the beginning of the
				// buffer and a maximum entry speed or two maximum entry speeds, every block in between
				// cannot logically be further improved. Hence, we don't have to recompute them anymore.
				if (block_buffer[nextIdx].entry_speed_sqr == block_buffer[nextIdx].max_entry_speed_sqr) { block_buffer_planned = block_index; }
				block_index = plan_next_block_index(block_index);
			}
		}


		public void plan_reset()
		{
			//memset(&pl, 0, sizeof(pl)); // Clear planner struct
			pl = new planner_t(true);

			block_buffer_tail = 0;
			block_buffer_head = 0; // Empty = tail
			next_buffer_head = 1; // plan_next_block_index(block_buffer_head)
			block_buffer_planned = 0; // = block_buffer_tail;
			RaisePlannerBlocksChanged(EPlannerBlockChangedState.Reset);
		}


		public void plan_discard_current_block()
		{
			if (block_buffer_head != block_buffer_tail)
			{ // Discard non-empty buffer.
				int block_index = plan_next_block_index(block_buffer_tail);
				// Push block_buffer_planned pointer, if encountered.
				if (block_buffer_tail == block_buffer_planned) { block_buffer_planned = block_index; }
				block_buffer_tail = block_index;
        RaisePlannerBlocksChanged(EPlannerBlockChangedState.BlockRemoved);
			}
		}


		public int plan_get_current_block()
		{
			if (block_buffer_head == block_buffer_tail) { return (-1); } // Buffer empty  
			//return(&block_buffer[block_buffer_tail]);
			return block_buffer_tail;
		}


		float plan_get_exec_block_exit_speed()
		{
			int block_index = plan_next_block_index(block_buffer_tail);
			if (block_index == block_buffer_head) { return (0.0f); }
			return ((float)System.Math.Sqrt(block_buffer[block_index].entry_speed_sqr));
		}


		// Returns the availability status of the block ring buffer. True, if full.
		public bool plan_check_full_buffer()
		{
			if (block_buffer_tail == next_buffer_head) { return (true); }
			return (false);
		}


		/* Add a new linear movement to the buffer. target[NutsAndBolts.N_AXIS] is the signed, absolute target position
			 in millimeters. Feed rate specifies the speed of the motion. If feed rate is inverted, the feed
			 rate is taken to mean "frequency" and would complete the operation in 1/feed_rate minutes.
			 All position data passed to the planner must be in terms of machine position to keep the planner 
			 independent of any coordinate system changes and offsets, which are handled by the g-code parser.
			 NOTE: Assumes buffer is available. Buffer checks are handled at a higher level by motion_control.
			 In other words, the buffer head is never equal to the buffer tail.  Also the feed rate input value
			 is used in three ways: as a normal feed rate if invert_feed_rate is false, as inverse time if
			 invert_feed_rate is true, or as seek/rapids rate if the feed_rate value is negative (and
			 invert_feed_rate always false). */
		//#ifdef USE_LINE_NUMBERS   
		//  void plan_buffer_line(float *target, float feed_rate, byte invert_feed_rate, int line_number) 
		//#else
		public void plan_buffer_line(float[] target, float feed_rate, bool invert_feed_rate)
		//#endif
		{
      //plan_lines.Add(new plan_line_t(pl.position[0] * settings.steps_per_mm[0],
      //                               pl.position[1] * settings.steps_per_mm[1],
      //                               pl.position[2] * settings.steps_per_mm[2],
      //                               target[0] * settings.steps_per_mm[0],
      //                               target[1] * settings.steps_per_mm[1],
      //                               target[2] * settings.steps_per_mm[2]));

			// Prepare and initialize new block	
			int blockIdx = block_buffer_head;
      block_buffer.Add(new plan_block_t(true));
			//plan_block_t *block = &block_buffer[block_buffer_head];
			block_buffer[blockIdx].step_event_count = 0;
			block_buffer[blockIdx].millimeters = 0;
			block_buffer[blockIdx].direction_bits = 0;
			block_buffer[blockIdx].acceleration = SOME_LARGE_VALUE; // Scaled down to maximum acceleration later
			//#ifdef USE_LINE_NUMBERS
			//  block_buffer[blockIdx].line_number = line_number;
			//#endif

			// Compute and store initial move distance data.
			// TODO: After this for-loop, we don't touch the stepper algorithm data. Might be a good idea
			// to try to keep these types of things completely separate from the planner for portability.
			int[] target_steps = new int[NutsAndBolts.N_AXIS];
			float[] unit_vec = new float[NutsAndBolts.N_AXIS];
			float delta_mm;
			byte idx;
			for (idx = 0; idx < NutsAndBolts.N_AXIS; idx++)
			{
				// Calculate target position in absolute steps. This conversion should be consistent throughout.
				target_steps[idx] = (int)System.Math.Round(target[idx] * settings.steps_per_mm[idx]);

				// Number of steps for each axis and determine max step events
				block_buffer[blockIdx].steps[idx] = (uint)System.Math.Abs(target_steps[idx] - pl.position[idx]);
				block_buffer[blockIdx].step_event_count = (uint)System.Math.Max(block_buffer[blockIdx].step_event_count, block_buffer[blockIdx].steps[idx]);

				// Compute individual axes distance for move and prep unit vector calculations.
				// NOTE: Computes true distance from converted step values.
				delta_mm = (target_steps[idx] - pl.position[idx]) / settings.steps_per_mm[idx];
				unit_vec[idx] = delta_mm; // Store unit vector numerator. Denominator computed later.

				// Set direction bits. Bit enabled always means direction is negative.
				if (delta_mm < 0) { block_buffer[blockIdx].direction_bits |= get_direction_pin_mask(idx); }

				// Incrementally compute total move distance by Euclidean norm. First add square of each term.
				block_buffer[blockIdx].millimeters += delta_mm * delta_mm;
			}
			block_buffer[blockIdx].millimeters = (float)System.Math.Sqrt(block_buffer[blockIdx].millimeters); // Complete millimeters calculation with sqrt()

			// Bail if this is a zero-length block. Highly unlikely to occur.
			if (block_buffer[blockIdx].step_event_count == 0) { return; }

			// Adjust feed_rate value to mm/min depending on type of rate input (normal, inverse time, or rapids)
			// TODO: Need to distinguish a rapids vs feed move for overrides. Some flag of some sort.
			if (feed_rate < 0) { feed_rate = SOME_LARGE_VALUE; } // Scaled down to absolute max/rapids rate later
			else if (invert_feed_rate) { feed_rate = block_buffer[blockIdx].millimeters / feed_rate; }
			if (feed_rate < MINIMUM_FEED_RATE) { feed_rate = MINIMUM_FEED_RATE; } // Prevents step generation round-off condition.

			// Calculate the unit vector of the line move and the block maximum feed rate and acceleration scaled 
			// down such that no individual axes maximum values are exceeded with respect to the line direction. 
			// NOTE: This calculation assumes all axes are orthogonal (Cartesian) and works with ABC-axes,
			// if they are also orthogonal/independent. Operates on the absolute value of the unit vector.
			float inverse_unit_vec_value;
			float inverse_millimeters = 1.0f / block_buffer[blockIdx].millimeters;  // Inverse millimeters to remove multiple float divides	
			float junction_cos_theta = 0;
			for (idx = 0; idx < NutsAndBolts.N_AXIS; idx++)
			{
				if (unit_vec[idx] != 0)
				{  // Avoid divide by zero.
					unit_vec[idx] *= inverse_millimeters;  // Complete unit vector calculation
					inverse_unit_vec_value = (float)System.Math.Abs(1.0 / unit_vec[idx]); // Inverse to remove multiple float divides.

					// Check and limit feed rate against max individual axis velocities and accelerations
					feed_rate = (float)System.Math.Min(feed_rate, settings.max_rate[idx] * inverse_unit_vec_value);
					block_buffer[blockIdx].acceleration = (float)System.Math.Min(block_buffer[blockIdx].acceleration, settings.acceleration[idx] * inverse_unit_vec_value);

					// Incrementally compute cosine of angle between previous and current path. Cos(theta) of the junction
					// between the current move and the previous move is simply the dot product of the two unit vectors, 
					// where prev_unit_vec is negative. Used later to compute maximum junction speed.
					junction_cos_theta -= pl.previous_unit_vec[idx] * unit_vec[idx];
				}
			}

			// TODO: Need to check this method handling zero junction speeds when starting from rest.
			if (block_buffer_head == block_buffer_tail)
			{

				// Initialize block entry speed as zero. Assume it will be starting from rest. Planner will correct this later.
				block_buffer[blockIdx].entry_speed_sqr = 0.0f;
				block_buffer[blockIdx].max_junction_speed_sqr = 0.0f; // Starting from rest. Enforce start from zero velocity.

			}
			else
			{
				/* 
					 Compute maximum allowable entry speed at junction by centripetal acceleration approximation.
					 Let a circle be tangent to both previous and current path line segments, where the junction 
					 deviation is defined as the distance from the junction to the closest edge of the circle, 
					 colinear with the circle center. The circular segment joining the two paths represents the 
					 path of centripetal acceleration. Solve for max velocity based on max acceleration about the
					 radius of the circle, defined indirectly by junction deviation. This may be also viewed as 
					 path width or max_jerk in the previous grbl version. This approach does not actually deviate 
					 from path, but used as a robust way to compute cornering speeds, as it takes into account the
					 nonlinearities of both the junction angle and junction velocity.

					 NOTE: If the junction deviation value is finite, Grbl executes the motions in an exact path 
					 mode (G61). If the junction deviation value is zero, Grbl will execute the motion in an exact
					 stop mode (G61.1) manner. In the future, if continuous mode (G64) is desired, the math here
					 is exactly the same. Instead of motioning all the way to junction point, the machine will
					 just follow the arc circle defined here. The Arduino doesn't have the CPU cycles to perform
					 a continuous mode path, but ARM-based microcontrollers most certainly do. 
       
					 NOTE: The max junction speed is a fixed value, since machine acceleration limits cannot be
					 changed dynamically during operation nor can the line move geometry. This must be kept in
					 memory in the event of a feedrate override changing the nominal speeds of blocks, which can 
					 change the overall maximum entry speed conditions of all blocks.
				*/
				// NOTE: Computed without any expensive trig, sin() or acos(), by trig half angle identity of cos(theta).
				float sin_theta_d2 = (float)System.Math.Sqrt(0.5 * (1.0 - junction_cos_theta)); // Trig half angle identity. Always positive.

				// TODO: Technically, the acceleration used in calculation needs to be limited by the minimum of the
				// two junctions. However, this shouldn't be a significant problem except in extreme circumstances.
				block_buffer[blockIdx].max_junction_speed_sqr = (float)System.Math.Max(MINIMUM_JUNCTION_SPEED * MINIMUM_JUNCTION_SPEED,
																		 (block_buffer[blockIdx].acceleration * settings.junction_deviation * sin_theta_d2) / (1.0 - sin_theta_d2));
			}

			// Store block nominal speed
			block_buffer[blockIdx].nominal_speed_sqr = feed_rate * feed_rate; // (mm/min). Always > 0

			// Compute the junction maximum entry based on the minimum of the junction speed and neighboring nominal speeds.
			block_buffer[blockIdx].max_entry_speed_sqr = (float)System.Math.Min(block_buffer[blockIdx].max_junction_speed_sqr,
																			 (float)System.Math.Min(block_buffer[blockIdx].nominal_speed_sqr, pl.previous_nominal_speed_sqr));

			// Update previous path unit_vector and nominal speed (squared)
			copyArray(pl.previous_unit_vec, unit_vec); // pl.previous_unit_vec[] = unit_vec[]
			pl.previous_nominal_speed_sqr = block_buffer[blockIdx].nominal_speed_sqr;

			// Update planner position
			copyArray(pl.position, target_steps); // pl.position[] = target_steps[]

			// New block is all set. Update buffer head and next buffer head indices.
			block_buffer_head = next_buffer_head;
			next_buffer_head = plan_next_block_index(block_buffer_head);

			// Finish up by recalculating the plan with the new block.
			planner_recalculate();
      RaisePlannerBlocksChanged(EPlannerBlockChangedState.BlockAdded, new DblPoint3(target[0] * settings.steps_per_mm[0],
                                                                                    target[1] * settings.steps_per_mm[1],
                                                                                    target[2] * settings.steps_per_mm[2]));
		}                                                                               

		// Reset the planner position vectors. Called by the system abort/initialization routine.
		public void plan_sync_position()
		{
			byte idx;
			for (idx = 0; idx < NutsAndBolts.N_AXIS; idx++)
			{
				pl.position[idx] = sys.position[idx];
			}
		}


		// Returns the number of active blocks are in the planner buffer.
		public int plan_get_block_buffer_count()
		{
      return block_buffer.Count;
		}


		// Re-initialize buffer plan with a partially completed block, assumed to exist at the buffer tail.
		// Called after a steppers have come to a complete stop for a feed hold and the cycle is stopped.
		public void plan_cycle_reinitialize()
		{
			// Re-plan from a complete stop. Reset planner entry speeds and buffer planned pointer.
			st_update_plan_block_parameters();
			block_buffer_planned = block_buffer_tail;
			planner_recalculate();
			RaisePlannerBlocksChanged(EPlannerBlockChangedState.Reset);
		}
	}
}

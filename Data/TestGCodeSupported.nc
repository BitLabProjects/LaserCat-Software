( Made using CamBam - http://www.cambam.co.uk )
( Untitled 11/5/2014 10:53:24 PM )
( T0 : 0.0 )
(G21 G90 G64 G40 <-- G64 and 40 are not supported)
G21 G90
G0 Z3.0
( T0 : 0.0 )
(T0 M6 <-- M6 not supported, it's a tool change)
( Profile1 )
G17
M3 S1000
G0 X0.0 Y0.0
G0 Z1.4
G1 F300.0 Z0.0
G1 F800.0 X-6.0 Y14.0
G1 X-15.0 Y22.0
G1 X-26.0 Y8.0
G1 X-18.0 Y-16.0
G1 X3.0 Y-29.0
G1 X26.0 Y-14.0
G1 X30.0 Y8.0
G1 X19.0 Y22.0
G1 X7.0 Y17.0
G1 X0.0 Y0.0
G0 Z3.0
M5
M30

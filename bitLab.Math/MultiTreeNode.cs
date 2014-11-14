using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public class MultiTreeNode<TValue>
  {
    public readonly TValue[] Values;
    public readonly MultiTreeNode<TValue> Parent;
    public readonly List<MultiTreeNode<TValue>> Children;

    public MultiTreeNode(MultiTreeNode<TValue> parent, TValue[] values)
    {
      Values = values;
      Parent = parent;
      Children = new List<MultiTreeNode<TValue>>();
    }

    private Int32 mLength = Int32.MinValue;
    public Int32 Length
    {
      get
      {
        //Calcola una sola vola la lunghezza, potrebbe volerci tanto (ifffff..)
        if (mLength == Int32.MinValue)
        {
          mLength = Values.Length;
          if (Children.Count != 0)
            mLength += Children.Select((x) => x.Length).Max();
        }
        return mLength;
      }
    }

    public MultiTreeNode<TValue> GetLongestChild()
    {
      MultiTreeNode<TValue> longestChild = null;
      int longestChildLength = 0;
      for (int i = 0; i < Children.Count; i++)
      {
        var child = Children[i];
        if (child.Length > longestChildLength)
        {
          longestChild = child;
          longestChildLength = child.Length;
        }
      }
      return longestChild;
    }
  }
}

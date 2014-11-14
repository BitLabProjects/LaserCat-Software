using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.Math
{
  public class Closures
  {
    private Closures() { }

    public static MultiTreeNode<T> WalkMany<T>(T startPoint, Func<T, List<T>> stepFunc) where T: struct
    {
      Func<T, T?> stepFuncForWalk = (x) =>
      {
        var next = stepFunc(x);
        if (next == null || next.Count > 1)
          return null;
        return next[0];
      };
      var firstWalk = new MultiTreeNode<T>(null, Walk(startPoint, stepFuncForWalk));
      var openWalks = new List<MultiTreeNode<T>>();
      openWalks.Add(firstWalk);
      while (openWalks.Count > 0)
      {
        var currWalk = openWalks.First();
        openWalks.RemoveAt(0);

        //Siccome currWalk era nella lista aperta, significa che stepFunc ha restituito più risultati per il suo punto finale (oppure null)
        //Quindi trovo le diramazioni di partenza e le uso per fare le prossime
        var firstPoints = stepFunc(currWalk.Values.Last());
        if (firstPoints != null)
        {
          for (int i = 0; i < firstPoints.Count; i++)
          {
            var nextPoints = Walk(firstPoints[i], stepFuncForWalk).ToList();
            nextPoints.Insert(0, currWalk.Values.Last());
            var nextWalk = new MultiTreeNode<T>(currWalk, nextPoints.ToArray());
            currWalk.Children.Add(nextWalk);
            openWalks.Add(nextWalk);
          }
        }
      }

      return firstWalk;
    }

    public static T[] Walk<T>(T startPoint, Func<T, T?> stepFunc) where T: struct
    {
      var result = new List<T>();
      T? currPoint = startPoint;
      while (currPoint != null)
      {
        result.Add(currPoint.Value);
        currPoint = stepFunc(currPoint.Value);
      }
      return result.ToArray();
    }
  }
}

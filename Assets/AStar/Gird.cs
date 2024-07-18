using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Gird : MonoBehaviour
{
    

    //public static List<NodeBase> FindPath(NodeBase startNode, NodeBase targetNode)
    //{
    //    var toSearch = new List<NodeBase>() { startNode };
    //    var processed = new List<NodeBase>();

    //    while(toSearch.Any())
    //    {
    //        //获取F值最小（相等时H最小）的节点
    //        var current = toSearch[0];
    //        foreach (var t in toSearch)
    //            if (t.F < current.F || t.F == current.F && t.H < current.H) 
    //                current = t;

    //        //添加到Processed中，同时在Search中移除保证不重复搜寻
    //        processed.Add(current);
    //        toSearch.Remove(current);

    //        foreach(var neighbor in current.Neighbors.Where(n => n.Walkable && !processed.Contains(n)))
    //        {
    //            bool InSearch = toSearch.Contains(neighbor);
    //            float costToNeighbor = current.G + current.GetDistance(neighbor);

    //            if(!InSearch)
    //            {
    //                neighbor.SetH(neighbor.GetDistance(targetNode));
    //                toSearch.Add(neighbor);
    //            }
    //            if()
    //        }
    //    }
    //}
}

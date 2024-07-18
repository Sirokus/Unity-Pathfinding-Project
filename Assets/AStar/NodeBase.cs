using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class NodeBase
{
    #region 属性
    public Vector2 position;
    #endregion

    #region 公式相关
    public NodeBase Connection { get; private set; }
    public float G {  get; private set; }
    public float H { get; private set; }
    public float F => G + H;
    
    public void SetConnection(NodeBase nodeBase) => Connection = nodeBase;
    public void SetG(float g) => G = g;
    public void SetH(float h) => H = h;
    #endregion

    public List<NodeBase> Neighbors;
    public bool Walkable = true;

    public float GetDistance(NodeBase node) => Vector2.Distance(position, node.position);
}

using System;
using UnityEngine;

public enum EdgeType
{
    WALK,
    JUMP_ATTACH,
    JUMP_GAP,
    WALL_WALK,
    DROP,
    CORNER
}

[Serializable]
public struct JumpParams
{
    public Vector3 takeoffPos;
    public Vector3 heading;
    public float runSpeed;

    public JumpParams(Vector3 takeoffPos, Vector3 heading, float runSpeed)
    {
        this.takeoffPos = takeoffPos;
        this.heading = heading;
        this.runSpeed = runSpeed;
    }
}

[Serializable]
public class RatEdge
{
    public RatNode from;
    public RatNode to;
    public EdgeType type = EdgeType.WALK;
    public float cost = 1f;
    public bool blocked;
    public JumpParams jumpParams;

    public RatEdge() { }

    public RatEdge(RatNode from, RatNode to, EdgeType type, float cost)
    {
        this.from = from;
        this.to = to;
        this.type = type;
        this.cost = cost;
    }
}

using UnityEngine;

public interface IRatInput
{
    Vector3 MoveDir { get; }
    bool Jump { get; set; }
    bool Sprint { get; }
    bool IsJumpHeld => false;
}

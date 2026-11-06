# Rat NPC Navigation & Physics Architecture

An AI Rat NPC that pursues targets across floors and climbs onto walls using the **exact same physics controller** (`MovementController`) as the player, driven purely through the `IRatInput` interface and a graph-based brain.

---

## 1. Architecture Overview

```
               +-------------------+
               |    IRatInput      |
               +-------------------+
                 /               \
                /                 \
    +--------------------+   +--------------------+
    |    PlayerInput     |   |      RatBrain      |
    | (Keyboard/Touch)   |   | (Graph Pathfinding)|
    +--------------------+   +--------------------+
                \                 /
                 \               /
               +-------------------+
               | MovementController| (Shared Physics Controller)
               +-------------------+
```

- **Zero Direct Manipulation**: The AI brain never touches `transform.position`, `MovePosition`, or sets velocity directly. All movement is enacted through simulated player inputs (`MoveDir`, `Jump`, `Sprint`).
- **Decoupled Input (`IRatInput`)**:
  - `Vector3 MoveDir`: Desired surface movement direction.
  - `bool Jump`: Edge-triggered jump request, consumed by the controller in `FixedUpdate`.
  - `bool Sprint`: Sprint modifier.
  - `bool IsJumpHeld`: Jump button held indicator for variable jump height.
- **Controller Read-Only State**: Exposes `IsGrounded`, `IsAttached`, `SurfaceNormal`, `SurfaceCollider`, and `Velocity`.

---

## 2. Authoring Graph Nodes

### Step 1: Create a Graph Container
1. Create an empty GameObject in the scene and attach the `RatGraph` component (`Assets/Project/Scripts/Navigation/RatGraph.cs`).

### Step 2: Add Nodes
1. Create child GameObjects under `RatGraph` and attach `RatNode` (`Assets/Project/Scripts/Navigation/RatNode.cs`).
2. Position the node on the desired surface.
3. Right-click the `RatNode` component in the Inspector and select **"Snap to Surface"** to automatically align the node position, surface normal, and surface collider.
4. For vertical walls, ensure `normal` points outwards away from the wall face (e.g. `(0, 0, -1)` for a wall facing -Z).

### Step 3: Link Walking Edges
1. In the `RatGraph` Inspector, use **"Rebuild Node List From Children"** to register all nodes.
2. Walking edges between adjacent nodes on the same surface can be linked via `graph.AddEdge(from, to, EdgeType.WALK, cost)`.

---

## 3. Baking Jump Edges (`RatGraphBaker`)

Jump edges (e.g. `JUMP_ATTACH` ground-to-wall or `JUMP_GAP`) are generated automatically using headless ghost physics simulation to guarantee determinism and realizability:

1. Select your `RatGraph` GameObject in the scene hierarchy.
2. In the custom Inspector, click **"Bake All Jump Edges (Ghost Sim)"**.
3. The baker will:
   - Identify candidate pairs between different surface colliders within maximum jump reach (~2.2m).
   - Instantiate a stripped ghost rat prefab with `GhostInput`.
   - Run physics simulations (`Physics.Simulate(dt)`) across speed sweeps (`0.0`, `0.5x`, `1.0x` max run) and yaw angles (`-10°`, `0°`, `+10°`).
   - If the ghost reaches and attaches to the target wall/surface, records the optimal `JumpParams` (`takeoffPos`, `heading`, `runSpeed`) and estimated transit seconds as cost.
   - If an edge is outside the rat's physical capabilities (too high or too far), no edge is created.

---

## 4. Runtime State Machine & Execution

### Ground Navigation (WALK)
- `RatBrain` queries `RatPathfinder` (A*) from nearest start node to nearest target node.
- Follows path waypoints with exponential direction smoothing (`1 - exp(-k * dt)`) and turn-rate limiting.
- Repaths dynamically when the target moves to a different graph region, without repathing mid-air.

### Jump Execution (`JUMP_ATTACH` / `JUMP_GAP`)
When encountering a jump edge, `RatBrain` transitions through a deterministic state machine:
1. **APPROACH**: Walks toward `jumpParams.takeoffPos` matching target run speed.
2. **ALIGN**: Orients toward `jumpParams.heading` until angular error is within tolerance (<15°).
3. **LAUNCH**: Triggers `Jump = true` for a single physics step.
4. **AIR**: Sustains directional momentum without repathing mid-air.
5. **LAND**: Detects wall attachment (`controller.IsAttached`) or ground contact on the target surface; advances to next path edge. On failure, increases edge cost (`cost *= 1.5`) and repaths.

### Robustness & Stuck Recovery
Every 0.5s:
- `stuckTime > 1.0s`: Forces immediate repath.
- `stuckTime > 2.0s`: Triggers jump impulse with lateral nudge.
- `stuckTime > 4.0s`: Temporarily marks current edge as `blocked = true` (auto-decays after 10s) and repaths.

---

## 5. Editor Verification & Test Suites

Automated test suites can be run directly from the Unity Editor menu bar under **`Tools`**:

| Menu Command | Test Scope | Acceptance Criteria |
| --- | --- | --- |
| `Tools > Run Pathfinder Tests` | Edit-mode A* unit tests | Shortest path optimal, blocked edge avoided, disconnected returns null |
| `Tools > Run Phase 3 Tests` | Obstacle avoidance soak test | 10/10 runs reach player around obstacle using only input, zero teleporting |
| `Tools > Run Phase 4 Tests` | Ground-to-wall jump attach | Hand-tuned jump execution succeeds >= 8/10 runs |
| `Tools > Run Phase 5 Tests` | Baker & runtime playback | Ghost-sim baker rejects impossible jumps; baked edges pass >= 9/10 runs |
| `Tools > Build Phase 3 Test Scene` | Builds `Assets/Project/Scenes/RatNpcTest.unity` | Runnable flat obstacle test scene |
| `Tools > Build Phase 4 Jump Test Scene` | Builds `Assets/Project/Scenes/RatNpcJumpTest.unity` | Runnable wall climb test scene |
| `Tools > Build Phase 5 Bake Test Scene` | Builds `Assets/Project/Scenes/RatNpcBakeTest.unity` | Runnable baker verification scene |

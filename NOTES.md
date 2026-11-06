# Rat Controller Recon Notes (Phase 0)

Analysis of `MovementController` and associated state machine (`Assets/Project/Scripts/MovementController.cs`, `Assets/Project/Scripts/MovementStates/*`, and `Assets/Project/Prefabs/Rat Player.prefab`).

---

### 1. Surface Detection
- **Ground Detection**:
  - `Physics.SphereCast(collider.bounds.center, 0.01f, -Vector3.up, out GroundHitInfo, 2f, ~_layerMask)` performed every `FixedUpdate`.
  - `IsGrounded` is true when hit occurs and `GroundHitInfo.distance < 0.035f`.
  - Transition to `FallingState` occurs when `GroundHitInfo.distance > 0.45f`.
  - Landing occurs when `GroundHitInfo.distance < 0.4f` and `linearVelocity.y <= -0.0001f`.
- **Wall Detection**:
  - `Physics.Raycast(collider.bounds.center + 0.5f * extents.y * Vector3.up, transform.forward, out WallHitInfo, extents.z + 0.15f, ~_layerMask, QueryTriggerInteraction.Collide)` performed in `FixedUpdate`.
  - Sets `IsWallInFront = true` when within ~0.184m forward reach of the rat.
- **Surface Normals & Contact**:
  - Ground normal is stored in `GroundHitInfo.normal`.
  - Wall climb stores `_wallNormal = WallHitInfo.normal` on enter. During climb, maintains contact via raycast backward along `-_wallDir` (distance `extents.z + 0.2f`) and checks for adjacent walls with `Vector3.Dot(newNormal, _wallNormal) < 0.8f`.

---

### 2. Input Reading
- **Where & Which Fields**:
  - `MovementController.MoveInput` (`Vector2`): Polled every `Update()` via `TouchInputManager.InputMain.move.ReadValue<Vector2>()`.
  - `MovementController.JumpAction` (`InputAction`): Initialized in `Awake()` via `InputSystem.actions.FindAction("jump")`.
- **Consumption in States**:
  - `MoveState`: Reads `MoveInput` in `FixedUpdateState()` relative to camera yaw. Subscribes `JumpAction.performed += OnJumpPerformed` in `EnterState()` (unsubscribes in `ExitState()`).
  - `IdleState`: Subscribes `JumpAction.performed += SwitchToJumpState`.
  - `JumpState`: Reads `MoveInput` in `FixedUpdateState()` for in-air steering. Subscribes `JumpAction.canceled += OnJumpCancelAction` in `EnterState()` (sets `_jumpButtonUp = true`).
  - `WallClimbState`: Reads `MoveInput.x` in `FixedUpdateState()` to steer laterally on the wall plane.
  - `WireClimbState`: Reads `MoveInput` to travel along spline tangent. Subscribes `JumpAction.performed` for dismount jump.

---

### 3. Physics Parameters (Jump Impulse, Run Speed, Drag, Gravity)
- **Jump Impulse**:
  - `_jumpForce` is configured to `2.5` in `Rat Player.prefab` (code default is `3.5`).
  - Upon takeoff in `JumpState` (after `_jumpDelay = 0.05s`), applies `StateMachine.Rb.AddForce(_jumpForce * Vector3.up, ForceMode.Impulse)`.
  - While airborne in `JumpState`:
    - Constant forward acceleration: `2.8f * direction` (`ForceMode.Acceleration`).
    - Held-jump upward boost (while `!_jumpButtonUp`): `0.4f * _jumpForce * (Vector3.up + 0.3f * forward)` (`ForceMode.Force`).
- **Max Run Speed & Acceleration**:
  - `_walkSpeed` = `1.4` m/s in prefab (code default `0.7`), `_speedMultiplier` = `1.0`.
  - Target velocity = `1.4 * InputSpeedCurve(input.sqrMagnitude)` m/s (curve evaluates 0 to 1).
  - Acceleration lerp factor: `_acceleration` = `0.4` in prefab (`Vector3.Lerp(currentVel, targetVel, Acceleration)` in `FixedUpdate`).
- **Drag**:
  - Rigidbody `LinearDamping = 0` (no linear drag), `AngularDamping = 0.08`.
- **Gravity**:
  - Standard Unity gravity: `(0, -9.8, 0)`. Rigidbody uses standard gravity. Kinematic mode is enabled only during `WireClimbState`.

---

### 4. Wall Attach Mid-Air Trigger
- **Source State**: Only triggers from `JumpState` (`AddTransition(WallClimbState, _ => IsWallInFront && !IsGrounded)`). Wall attach does not trigger from `MoveState`, `IdleState`, or `FallingState`.
- **Range**: `collider.bounds.extents.z + 0.15f` (~0.184m forward from collider center).
- **Angle**: Raycast points along `transform.forward`. Transitions if any collider in `~_layerMask` is struck.
- **Input Needed**: None required at trigger time; only requires rat orientation facing the wall while airborne in `JumpState`.

---

### 5. Wall Attached Behavior
- **Movement**: Does not stick or slide down; automatically climbs upward at `_climbForce = 1.5` m/s continuously (`rb.linearVelocity = _climbForce * climbDir * moveDir`).
- **Held Input**: None needed to climb vertically. `MoveInput.x` steers laterally.
- **Timeout & Detach**:
  - Times out when `_timer > _climbTimeout` (`100s` in prefab).
  - Automatically dismounts to `MoveState` when reaching the top or side edge (`!_currentWallExists && !_newWallInFront`).
  - No jump-off or drop-off action currently exists in `WallClimbState`.

---

### 6. Physics Timing & Input Latching
- **Physics Execution**: Raycasts, velocity updates, and forces run in `FixedUpdate` / `FixedUpdateState()`.
- **Transitions**: Evaluated both in `Update()` (`UpdateState()`) and `FixedUpdate()` (`FixedUpdateState()`).
- **Input Latching**: No physics-step latching exists. `JumpAction` is event-driven in `Update()`. For brain control, jump must be edge-triggered and latched so setting `Jump = true` for one physics step reliably triggers the jump in `FixedUpdate`.

---

### 7. Exposed Controller State
- `IsGrounded`: Exposed (`public bool IsGrounded { get; private set; }`).
- `IsAttached`: **Not exposed** (currently implicit via `CurrentState is WallClimbState or WireClimbState`).
- `SurfaceNormal`: **Not exposed as a unified property** (`GroundHitInfo.normal` exists when grounded, but wall normal is internal to `WallClimbState`).
- `SurfaceCollider`: **Not exposed as a unified property** (`GroundHitInfo.collider` exists for ground).
- `Velocity`: Exposed (`public Vector3 Velocity;` read from `_rb.linearVelocity`).
- `Collider`: Exposed (`public CapsuleCollider PlayerCollider => _playerCollider;`).

# Color Ants Jam - Game Rules & Mechanics Reference

## 1. Core Objective & Rescue Rules
- Ants are organized into colored queues on a discrete grid.
- Tapping a queue initiates its forward movement toward a color-matching collection hole.
- Each queue has an assigned ant color and moves head-first in its designated forward orientation.
- When an ant queue reaches a matching hole with available capacity, it enters the hole and is rescued.
- Rescuing all active queues on the board before the timer expires completes the level.

## 2. Wrong-Move Feedback System (Stationary Queues & Wrong-Color Holes)
- **Classification & Triggers**:
  - When a queue is tapped, move validation classifies whether the move is valid or blocked.
  - Locked queues and full same-color holes maintain static feedback (padlock shake or blocked shake and immediate life deduction).
  - Queues blocked by an eligible **stationary queue** OR a **wrong-color hole** trigger a dynamic failed-move attempt.
- **Physical Approach, Impact, & Reverse Motion**:
  - **Strict Straight Approach**:
    - Outside valid pipes, the leader advances strictly along its original cardinal heading ($0^\circ, 90^\circ, 180^\circ, 270^\circ$). Perpendicular coordinates remain 100% constant (zero lateral drift, steering, or alignment toward hole center).
    - Followers follow the existing ordered queue path and the leader's straight extension. Bent queues slither naturally and do not translate as a rigid block.
  - **Stop Before Visual Overlap & Outer Rim Boundaries**:
    - **Stationary Queues**: First contact is computed dynamically using authoritative ant visual dimensions (leader extents $\approx [0.31, 0.52]$, worker extents $\approx [0.29, 0.415]$) and visual scale with continuous swept 2D OBB (SAT) testing and 0.04-unit safety clearance.
    - **Wrong-Color Holes**: Outer rim boundary is computed using floor-level bounds (stadium mesh or cell footprint minus margin). The ant's leading visual tip stops precisely at the outer rim with 0.04 clearance. The ant's head pivot remains strictly outside the hole at all times ($> 0.56$m from rim). Continuous swept testing guarantees fast speeds never skip the rim.
    - Zero mesh or proxy penetration occurs at any frame, regardless of approach speed or frame rate.
  - **Impact, Audio & Life Loss**:
    - At contact with a stationary queue or wrong-color hole rim, forward movement halts immediately.
    - Exactly **one heart is deducted** and the "ouch" sound plays once (never on initial tap).
    - Blocker (queue or hole) remains **100% stationary** (no translation, punch, or positional shift).
    - All ants remain **100% visible** throughout the failed attempt: zero swallowing, descent, clipping, transparency, or scale reduction.
    - If recoil is applied, it moves the approaching queue backward along its trail.
  - **Reverse Retracing & Pose Restoration**:
    - Ants reverse back by retracing their traveled path backward.
    - Follower and leader ants face forward as they back up (no $180^\circ$ rotation, teleportation, or diagonal shortcuts).
    - Upon completing the return, the queue restores its exact initial coordinates, orientation, follower spacing, and idle state.
- **Dynamic Blocker Recheck**:
  - If the blocking queue begins moving or clears before contact, the collision penalty is canceled: no heart is lost, no ouch sound plays, and the queue returns safely.
- **Moving Queue Non-Collision**:
  - Moving queues never collide with each other or cause "ouch" penalties; paths and cells may be shared during transit.

## 3. Linked Ant Queues
- Queues tied together with leader ropes act as an atomic group.
- If any member is blocked by a stationary obstacle, the entire group advances to the earliest genuine collision point in parallel lockstep.
- Exactly **one heart** and **one ouch sound** are applied for the entire group action.
- All members reverse in unison with leader ropes aligned throughout the motion.

## 4. Special Mechanics & Safety Constraints
- **Stacked Ants**: A failed move attempt on an upper layer never reveals or activates the dormant lower layer.
- **Pipes**: Failed routes traversing pipes do not consume pipe usage counts; temporary path reservations are cleanly released on return.
- **Holes**: Hole available capacity is never consumed by a failed move attempt.
- **Locks**: Padlock counters are never decremented on failed move attempts.
- **Last Heart**: If an impact depletes the player's last heart (lives = 0), the impact and return animation finish completely before the Level Lost failure overlay is displayed. Input is blocked during this final return.
- **Pause & Resume**: Opening the Settings menu freezes in-flight movement smoothly; resuming restores motion cleanly without duplicate events or corrupted coordinates.

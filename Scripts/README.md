# PoC 2 — Core Functionality Documentation

## Overview

First-person action demo built in Unity (URP). The player explores a 3D sandbox, casts elemental abilities, interacts with objects, and fights AI enemies.

---

## Architecture

```
Scripts/
│
│  ── Core Interfaces ──
│  IInteractable.cs           Contract for objects the player can interact with
│
│  ── Player ──
│  PlayerMovement.cs          CharacterController movement, jumping, knockback
│  CameraMovement.cs          First-person camera (yaw → body, pitch → camera)
│  InteractionSystem.cs       Raycast interaction, ability casting, HUD, health
│  MouseLock.cs               Cursor visibility / lock state
│
│  ── Abilities ──
│  AbilityDefinition.cs       ScriptableObject: name, icon, cooldown
│  AbilityProjectile.cs       Base projectile: movement, lifetime, collision routing
│  ├─ FireballProjectile.cs   Direct damage + burn DoT
│  ├─ FrostballProjectile.cs  AoE slow + random freeze
│  └─ TeleportballProjectile.cs  Teleports player to impact point
│
│  ── Enemies ──
│  Enemy.cs                   Health, melee attack, status effects, visual feedback
│  EnemyAI.cs                 Rigidbody chase AI (reads Enemy.MoveSpeed)
│
│  ── Environment ──
│  ButtonInteraction.cs       IInteractable button that spawns enemies
│  AutoTileTexture.cs         Auto-tiles material UVs to match object scale
```

---

## Player Systems

### Movement (`PlayerMovement`)
- WASD input via Unity Input System callbacks (`OnMove`, `OnJump`).
- Momentum-based acceleration/deceleration for smooth feel.
- Gravity and grounded check via `CharacterController`.
- External knockback support (`ApplyKnockback`) decays over time.

### Camera (`CameraMovement`)
- Child of the player capsule.
- Yaw rotates the parent body; pitch rotates only the camera.
- Clamped vertical look (±89°).

### Cursor (`MouseLock`)
- Locks and hides cursor on start. Configurable via Inspector.

---

## Interaction System (`InteractionSystem`)

### Raycasting
- Every frame, a screen-centre ray detects `IInteractable` objects.
- An "E" prompt appears when an interactable is in view.

### Abilities
| Slot | Ability        | Key | Effect                                   |
|------|----------------|-----|------------------------------------------|
| 0    | Fireball       | 1   | Direct damage + burn-over-time           |
| 1    | Frostball      | 2   | AoE slow; chance to freeze               |
| 2    | Teleportball   | 3   | Teleports the player to impact location  |

- Selected via number keys (1/2/3) or mouse scroll wheel.
- Cast with left-click. Respects per-ability cooldowns.
- A single projectile prefab is instantiated and configured at runtime with the matching sub-type component.

### HUD (procedurally created if absent)
- **Crosshair** — white dot, screen centre.
- **Ability bar** — three slots along bottom, highlight follows selection.
- **Health bar** — top centre, fill shrinks from right; flashes red/yellow on damage.

### Health & Death
- Enemies deal damage + knockback via `TakeDamage`.
- On death: all enemies destroyed, player respawns at origin with full HP.

---

## Ability Projectiles

### Base Class — `AbilityProjectile`
- Self-destructs after `lifetime` seconds.
- `Launch(direction)` sets Rigidbody velocity.
- `SetAbilityColor(index)` tints the material.
- `OnCollisionEnter` ignores the player tag; delegates to `OnImpact`.

### Fireball (`FireballProjectile`)
- Deals instant `damage` to the hit enemy.
- Applies `ApplyBurn(tickDamage, duration, tickInterval)` — periodic damage over time.

### Frostball (`FrostballProjectile`)
- `Physics.OverlapSphere` AoE on impact.
- Applies `ApplySlow(amount, duration)` to all enemies in radius.
- Random `freezeChance` per enemy → `ApplyFreeze(duration)`.

### Teleportball (`TeleportballProjectile`)
- Finds the player's `InteractionSystem` and calls `TeleportToPosition`.
- Temporarily disables the `CharacterController` for the position change.

---

## Enemy Systems

### `Enemy`
| Feature       | Details                                                    |
|---------------|------------------------------------------------------------|
| Health        | `ApplyDamage` → red flash → `Die` on ≤ 0                  |
| Burn DoT      | Periodic ticks; re-castable (restarts timer)               |
| Slow          | Reduces `MoveSpeed`; non-stacking; tints blue              |
| Freeze        | Sets `MoveSpeed` to 0, pauses animation, flashes cyan      |
| Melee attack  | Range/cooldown-based; deals damage + knockback to player   |
| Animation     | Coroutine-driven squish loop; pauses when frozen           |

- `MoveSpeed` (property) is the single source of truth for `EnemyAI`.

### `EnemyAI`
- Reads `Enemy.MoveSpeed` each `FixedUpdate` so slow/freeze effects work automatically.
- Rigidbody-based chase; preserves vertical velocity (gravity).
- Stops within `stoppingDistance`; smoothly rotates to face the player.

---

## Environment

### `ButtonInteraction`
- Implements `IInteractable`.
- On interact: flashes green, then spawns `enemiesToSpawn` enemies at a random distance/angle around the player.

### `AutoTileTexture`
- Adjusts `mainTextureScale` based on `lossyScale / baseScale`.
- Runs in Edit Mode for immediate preview; optional continuous runtime update.

---

## Coding Conventions

| Aspect              | Convention                                      |
|---------------------|-------------------------------------------------|
| Private fields      | `m_PascalCase` prefix                           |
| Serialized fields   | `[SerializeField] private`                      |
| Public API          | Properties / methods with XML `<summary>` docs  |
| Access modifiers    | Always explicit (`private`, `protected`, etc.)   |
| Regions             | `#region` blocks: Configuration, Private Fields, Lifecycle, Public API, Validation, etc. |
| Debug logging       | Only `Debug.LogError` for genuine error states   |
| Coroutines          | Named `…Routine` suffix                         |

---

## ScriptableObject Assets

Located in `Assets/Abilities/`:

| Asset                    | Ability     |
|--------------------------|-------------|
| Fireball_Ability.asset   | Fireball    |
| Frostball_Ability.asset  | Frostball   |
| Teleport_Ability.asset   | Teleportball|

Created via **Assets → Create → Abilities → AbilityDefinition**.

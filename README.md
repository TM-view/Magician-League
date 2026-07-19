# Magician League

Multiplayer top-down arena game where players create their own magic instead of selecting predefined abilities.

Players assemble spells by combining different magical components type such as Active, Effect and Powerup. These combinations produce unique spells with different behaviors, allowing players to experiment with various combat strategies and playstyles.

During battle, players fight against other players, gain experience, unlock stronger magic, and continuously improve their character by adjust and revise spells.

> **Status:** In Development

---

## GameLoop

```
Start Game
     │
     ▼
Enter Player Name (Lobby)
     │
     ▼
Join Multiplayer Room
     │
     ▼
Explore & Fight
     │
     ├──────────────────────┐
     ▼                      │
Defeat Enemies or Players   │
     │                      │
     ▼                      │
Gain EXP & Score            │
     │                      │
     ▼                      │
Upgrade Status              │
     │                      │
     ▼                      │
Unlock New Spell Components │
     │                      │
     ▼                      │
Create / Edit Spell         │
(Combine Spell Components)  │
     │                      │
     └──────┬───────────────┘
            ▼
     Continue Fighting
            │
            ▼
      End Match / Died
            │
            ▼
      Scoreboard update
            │
            ▼
      Return to Lobby

```
---

## Gameplay Demo

> **Web Demo:** Coming Soon

---

# Core Features

## Spell Creation

The spell system is built using reusable Spell Components rather than hardcoded abilities. Every spell is generated dynamically from a collection of independent components, making it easy to introduce new spells without rewriting existing gameplay logic.

Example

```
Linear (Active)
      +
Damage (Effect)
      +
Burn (Powerup)
      ↓
   Fireball
```

Active
- Around (Circle projectile around) (Short Distance)
- Cone (Squares projectile made into Cone) (lower middle)
- Donut (Circle projectile Drill a hole in the center) (middle Distance)
- Laser (Square projectile in a long line) (Upper middle Distance) (Pierce + 2)
- Linear (Circle projectile rush forward) (Long Distance)
- Self (No projectile) (no Distance)
- Touch (Circle projectile It happens at the moment of clicking, but within a limited range) (middle Distance)

Effect
- Barrier (Creates a barrier against attacks. Block count equals the number of Barrier components and lasts 30 seconds. Pierce is consumed by barrier first; if pierce remains after the barrier is removed, the spell penetrates normally)
- Cure (Remove bad states based on the number of Cure components, using the first-in, first-out (FIFO) method)
- Damage (Increases damage to spells)
- Duplicate (Increases simultaneous magic projectiles by the number of Duplicate components. For example, Duplicate x2 fires 3 projectiles total)
- Heal (Add healing to magic)
- Ironbody (Reduces new incoming damage by 90%, prevents new debuffs from that hit, prevents movement, and makes incoming projectiles bounce off without piercing the user. Existing debuffs before Ironbody remain. Effect lasts 4 seconds)
- Magnet (Attracts projectiles, players, and monsters toward the magical center for 5 seconds. Force and radius increase with the number of components, but targets should still be able to escape unless many components are used)
- Repeat (Uses the spell repeatedly according to the number of components, with 0.3 seconds between repeats)
- Shift (If the spell hits a non-self target, it immediately flings the hit target once in a random direction. More Shift components increase the fling distance. When used with Self, it flings the caster toward the mouse cursor once; Accelerate increases fling speed and additional Shift increases fling distance. Repeat with Self + Shift adds more separate flings, but does not increase the force or distance of each fling)
- Smog (Create a circular mist in the center of the magic circle, effect for 10 seconds. It blocks vision by being placed at z = 20 and applies the spell powerup/debuff rolls once per second inside the mist)
- Wolf (When used with a Self-type Active, it temporarily transforms you into a wolf placeholder for 30 seconds: movement speed x1.6, charge damage x2.5, and magic disabled. When used with other Actives, it summons wolves equal to the number of components for 30 seconds if a pet prefab is assigned)

Powerup
- Accelerate (Speeding up the movement of magic per component)
- Bounce (Increases the magic bounce based on the number of components. Pierce is used before Bounce. If Pierce remains, penetration happens first; when Pierce is exhausted, Bounce can occur. Bounce applies effects when it hits wall/object/player/unit)
- Burn (Increases damage slightly and inflicts burn on spells, with a 15% chance to affect them per component)
- Delay1 & Delay2 (It fires a delayed spell that maintains its direction and position per component. Mana is spent immediately, but cooldown does not start until the spell activates. Delay1 = 0.5s per component, Delay2 = 1s per component)
- Explode (A small explosion occurs around the person hit by the magic attack. Explosion damage = 5, radius equals Touch radius, and powerup/debuff effects are also applied. Explosion does not need to pass barrier)
- Freeze (Slightly increases damage and has a 15% chance to freezing enemies per component)
- Leech (Take actual damage dealt after barrier and mitigation, then heal yourself by 15% per component)
- Pierce (Slightly increases the damage and penetration effectiveness of magic per component, such as spells used on objects, players, and defensive magic)
- Poison (Increases damage slightly and has a 15% chance to inflict poison on enemies per component)
- Powerup1 & Powerup2 & Powerup3 (Enhance Effects only. Powerup1 increases size/radius, Powerup2 increases strength, and Powerup3 increases duration/range/lifetime)
- Reverse (Projectile magic fires backward. It has no effect on non-projectile Actives)
- Shock (Slightly increases damage and has a 15% chance to Shocking enemies per component)
- Slaughter (Instant kill chance = 1% per Slaughter component. It must pass barrier first)
- Transfer (If mana runs out, health will be used as the remaining cost instead of mana. The spell cannot be used if the health cost would kill the caster)
- Wet (Increases damage slightly and has a 15% chance to wet the enemy per component)

## Debuff
- Burning (Deals 4 damage per second for 9 seconds. If it spreads by collision, the new Burning duration is 5 seconds)
- Freezing (Deals 2 damage per second, reduces movement speed to zero, and prevents spell casting. If it collides with anything, it deals 40 damage to the affected target. Effect lasts 3 seconds)
- Poisoning (Deals 2 damage per second for 15 seconds. Strong Poisoning deals 4 damage per second for 30 seconds)
- Shocking (Those affected move significantly slower, their spell cooldown is 50% longer, and they take 1 damage when their position actually changes. Effect lasts 6 seconds)
- Wetting (People afflicted with this status become slippery while moving. Moving at high speed while Wetting has a chance to make them fall and become Stunned for 1 second. Effect lasts 9 seconds)
- Stunned (Prevents movement and spell casting for its duration)

Combination:
- Burning + Wetting = Deals 25 damage and cures both debuffs
- Freezing + Burning = Deals 20 damage, cures both debuffs, and adds Wetting
- Poisoning + Burning = 75% chance to cure both debuffs, 25% chance to cure Burning and turn Poisoning into Strong Poisoning. Strong Poisoning deals 4 damage per second and lasts twice as long as normal poison
- Wetting + Freezing = Deals 70 damage, removes Wetting, extends Freezing, and prevents additional Wetting or Freezing from being applied during that Freezing period
- Wetting + Shocking = Deals 16 damage 5 times and reduces movement speed to zero. Once damage is dealt, both debuffs are removed. Additionally, if anyone collides with an affected target, that person will also be affected by the abnormal status effect

## Rule
- If a negative situation originates from within ourselves, it won't self-destructively, but other negative effects will persist, such as slowing down or taking more damage
- Magic cannot inflict damage on oneself
- If the status is applied repeatedly, it will reset the time
- An effect must have "Active" in front of it, for example, A E P E (the second E is considered to already contain A)
- Powerup must be placed after an Effect character, for example, A E P P (the second P is considered legal)
- If a player uses more than one Active component, it will have this effect: e.g., A E P P A E P P (the second E P P after the A will act separately from the first A E P P). A further example: the first A E P P deals area damage (Linear), while the second A E P P deals area damage (Around). It will deal damage around the Linear element it fires, but only at the end of the Linear range, not continuously
- The first non-empty component in a spell must always be Active
- Empty slots do not break a segment. For example, Active, Damage, empty, Pierce is still valid and Pierce still affects that segment
- If an Active appears after empty slots or after other components, it starts a new segment
- Powerups in a segment affect the whole segment, not only the previous Effect
- If a chained segment follows a projectile segment, the next segment happens at the hit position. If the projectile hits nothing, it happens at the end position. If Pierce or Bounce remains, the next segment waits until the last hit or endpoint
- Donut, Cone, and Around can pass through objects/units without limit, but they need Pierce to pass Barrier. Barrier uses one shared Pierce pool per cast
- All random status chances and combination rolls must be decided by Fusion StateAuthority

---
# Architecture

The project follows a modular, component-based architecture where gameplay systems are separated into independent modules. This design improves maintainability, simplifies debugging, and allows new features to be added without affecting unrelated systems.

## Player Architecture

```
Player
├── Input
├── Movement
├── Combat
├── Health
├── Mana
├── Spell
├── Inventory
└── Multiplayer
```

Each module has a dedicated responsibility.

- **Input** handles player controls.
- **Movement** manages character movement and rotation.
- **Combat** processes attacks and damage calculation.
- **Health & Mana** control player resources.
- **Spell** generates and executes custom spells.
- **Inventory** manages collected items and equipment.
- **Multiplayer** synchronizes gameplay across connected players.

---

## Spell Architecture

```
Spell
├── Element
├── Projectile
├── Effect
├── Buff
└── Visual Effect
```

Each spell is assembled from independent components.

- **Element** defines the spell's elemental identity.
- **Projectile** determines how the spell travels.
- **Effect** specifies additional behaviors after casting or collision.
- **Buff** modifies spell properties.
- **Visual Effect** controls particle effects and visual feedback.

This architecture allows new spell combinations to be created by adding new components instead of modifying existing systems, making the project easier to extend and maintain.

---

# Development Roadmap

## Completed

- Player movement
- Spell creation prototype
- Basic combat
- Health & Mana system
- Inventory prototype
- UI prototype

## In Progress

- Multiplayer synchronization
- Enemy AI
- Visual Effects

## Planned

- Quest System
- Boss Battle
- Ranking System
- Matchmaking

---

# Technical Challenges

## Designing a Flexible Spell System

Instead of creating every spell manually, the project uses reusable spell components that can be combined to generate many different skills.

Benefits:

- Easier maintenance
- Better scalability
- Reduced duplicated code
- Faster feature expansion

---

## Multiplayer Synchronization

Keeping player movement and combat synchronized while maintaining responsive gameplay is one of the primary technical challenges of the project.

The networking system is designed to separate gameplay logic from synchronization logic, making future maintenance easier.

---

# What I Learned

Through this project I gained practical experience in

- Multiplayer Game Development
- Gameplay Programming
- Game Architecture
- Object-Oriented Design
- Component-Based Design
- State Management
- UI Development
- Performance Optimization
- Debugging Complex Systems

---

# Future Improvements

- More Spell Components
- Better AI
- Character Customization
- Mobile Support

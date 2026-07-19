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
- Self (No projectile) (no Distance)
- Linear (Circle projectile rush forward) (Long Distance)
- Laser (Square projectile in a long line) (Upper middle Distance) (Pierce + 3)
- Donut (Circle projectile Drill a hole in the center) (middle Distance) (Pierce + 4)
- Cone (Squares projectile made into Cone) (lower middle) (Pierce + 5)
- Around (Circle projectile around) (Short Distance) (Pierce + 6)
- Touch (Circle projectile It happens at the moment of clicking, but within a limited range) (middle Distance)

Effect
- Damage (Increases damage to spells)
- Heal (Add healing to magic)
- Cure (Remove bad states based on the number of components, using the first-in, first-out (FIFO) method)
- Barrier (It creates a barrier against attacks a number of times depending on the component, but it can be ignored by pierce components and accessed if pierce components still remain, allowing normal penetration)
- Magnet (Increases the attraction of surrounding objects towards the magical center. The size depends on the number of components)
- Split (Separate the magic spell into 2 raised to the power of a component, Perhaps we should consider a new center of magic)
- Repeat (Use the spell repeatedly according to the number of components)
- Shift (If you hit a target, you can control the object's direction once by clicking and dragging it, but when used with Self, the push will be in the direction of the mouse cursor)
- Smog (Create a circular mist in the center of the magic circle)
- Ironbody (Reduces damage taken by 90%, but the user cannot move, and incoming projectiles will bounce off (bounce + 1) and cannot penetrate the user)
- Wolf (When used with a Self-type Active, it temporarily transforms you into a wolf, resulting in increased movement speed and higher damage dealt when charging, but temporarily disables your magic. However, when used with other Actives, it summons wolves equal to the number of components. Your attacks in wolf or pet form can be further enhanced by power-ups)

Powerup
- Accelerate (Speeding up the movement of magic per component)
- Burn (Increases damage slightly and inflicts burn on spells, with a 15% chance to affect them per component)
- Delay1 & Delay2 (It fires a slow spell that maintains its direction and position per component, and does not go on cooldown if the initial setting has not been activated)
- Pierce (Slightly increases the damage and penetration effectiveness of magic per component, such as spells used on objects, players, and defensive magic)
- Poison (Increases damage slightly and has a 15% chance to inflict poison on enemies per component)
- Powerup1 & Powerup2 & Powerup3 (Enhance the effectiveness of various spells, significantly depending on the component and level this component)
- Wet (Increases damage slightly and has a 15% chance to wet the enemy per component)
- Paralyze (Slightly increases damage and has a 15% chance to Paralyzing enemies per component)
- Freeze (Slightly increases damage and has a 15% chance to freezing enemies per component)
- Transfer (If mana runs out, blood will be used as a cost instead of mana)
- Explode (A small explosion occurred around the person hit by the magic attack)
- Leech (Take the damage dealt and increase your own health by 15% per component)
- Slaughter (There is a chance of instant kill based on the percentage of components)
- Reverse (Magic has an opposite goal)
- Bounce (Increases the magic bounce based on the number of components. If a penetration occurs, it will be canceled temporarily, but can be used again once the number of bounces is exhausted)

## Debuff
- Burning (Deals moderate damage to opponents afflicted with the magic status effect for 8 seconds, If collides with anything, that thing will also be affected by the status, but the duration will only be 4 seconds)
- Poisoning (Deals damage to opponents affected by the status effect for 15 seconds. Damage dealt is low)
- Wetting (People afflicted with this status effect take 20% more damage from others and move slightly slower)
- Paralyzing (Those affected by this status will move significantly slower, their spell will have a 50% longer cooldown, and they will take minor damage when moving)
- Freezing (Deals minor damage and reduces movement speed to zero for those affected, and if it collides with anything, it will deal moderate damage to the affected target And you cannot cast spells while affected by the status effect after remove by taken will move significantly slower)

Combination:
- Burning + Wetting = Deals moderate damage and Cure both Debuff
- Freezing + Burning = Deals moderate damage and Cure both Debuff and Add Wetting
- Wetting + Freezing = Increased damage dealt upon collision and extended the duration of the Freezing status. The Wetting status is also removed during Freezing time. From now on, the Freezing and Wetting statuses will no longer be applicable
- Wetting + Paralyzing = Inflicts moderate to high damage to affected targets 5 times and reduces their movement speed to zero. Once damage is dealt, the abnormal status effect is removed from both targets. Additionally, if anyone collides with an affected target, that person will also be affected by the abnormal status effect
- Poisoning + Burning = There's a 25/75 chance of whether or not both status effects will be removed. If not removed, the first poison will reappear and cannot be removed by this process again. The stronger poison will deal twice as much damage and last twice as long

## Rule
- If a negative situation originates from within ourselves, it won't self-destructively, but other negative effects will persist, such as slowing down or taking more damage
- Magic cannot inflict damage on oneself
- If the status is applied repeatedly, it will reset the time
- An effect must have "Active" in front of it, for example, A E P E (the second E is considered to already contain A)
- Powerup must be placed after an Effect character, for example, A E P P (the second P is considered legal)
- If a player uses more than one Active component, it will have this effect: e.g., A E P P A E P P (the second E P P after the A will act separately from the first A E P P). A further example: the first A E P P deals area damage (Linear), while the second A E P P deals area damage (Around). It will deal damage around the Linear element it fires, but only at the end of the Linear range, not continuously

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

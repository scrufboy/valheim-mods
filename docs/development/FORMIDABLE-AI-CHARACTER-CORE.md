# FormidableAI character core

Status: owner architecture. This document fixes boundaries before additional live-control modules.

## One person, one brain

Living World C.O.R.E. owns a persisted person. FormidableAI is that person's bounded decision system,
not a combat plugin running beside unrelated voice, status and social plugins. Every module reads the
same character profile and current world frame, proposes through the same arbitrator, and expresses
the resulting state through owned outputs.

```text
Persisted character profile (WHO)
  + world/party frame (WHERE, WITH WHOM)
  + capabilities/loadout (WHAT CAN I DO)
  + needs, bonds and duties (WHY)
                    |
                    v
            Formidable arbitrator
         mode -> intent -> owned domains
                    |
                    v
       bounded action skill (HOW, WHEN)
                    |
                    v
 native movement/combat + voice/status/emote expression
                    |
                    v
       feedback, memory and progression
```

## Character rollup

The rollup is deterministic for a saved person and records identity, origin biome, culture/people,
gender expression, faction/family/bonds, job/type, individual variance, starting stats and skills,
food-derived health/stamina, armor, weapons, ammunition, magic or special equipment, utilities and
world-progression inputs. It produces an inspectable explanation of the resulting loadout and training.

## Modes bound the problem

The system does not think about every possible behavior continuously. The current mode selects a small
eligible set: social/rest, work/hunt, travel, readiness, combat, recovery, retreat or emergency. Facts
and goals can change the mode; modules cannot ignore it.

## Action skills

An action skill is a small executable routine such as block an observed strike, dodge an unblockable
attack, close to spear reach, kite while preserving a firing lane, dash and recover, hold a choke,
protect a partner, herd enemies toward an area attack, or throw a spear from a valid elevated movement
state. Each skill declares:

- required character capability and equipment;
- sensed and predicted preconditions;
- tactical purpose and target;
- domains required;
- commitment point and expected feedback;
- abort/handback conditions;
- recovery and stamina cost;
- telemetry explaining why it was selected and what happened.

Timers may enforce human reaction, commitment and recovery. They may not blindly trigger the skill.

## Training and doctrine

Shared survival reasoning is universal. Weapon, class, biome, enemy-family and party-role doctrines
narrow and rank valid skills for this person. Randomness operates only after invalid choices are removed.
Progression changes skills and capability; equipment changes what can be executed; neither rerolls identity.

## Module rule

Voice, status, environment, social behavior, emotes and future class packages are modules of the same
person. They may observe, propose or express. They may not keep a competing truth, bypass arbitration,
or write native state outside an owned domain. Spawning and camp systems instantiate persisted profiles;
they never become behavioral authority.

## Current evidence boundary

The existing kernel, domain arbitration, class proposals, defense/perception policies and shadow trace
are foundation pieces. The 1.0.32.22 live run proves the observer wiring and telemetry, not a complete
brain. The next work must replace provisional class labels and placeholder facts with the character
profile and real action/opportunity state before live control is transferred.

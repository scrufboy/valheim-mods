# Living World C.O.R.E. character identity and additive roster contract

Status: owner-controlled, non-negotiable project invariant.

This contract prevents combat, class and biome work from silently rewriting the people
the project is building. It applies to source code, configs, migration logic, spawning,
profiles, Tooler controls, tests, documentation and installed defaults.

## 1. Identity, role, doctrine and loadout are different facts

Every persisted NPC has separate facts:

| Fact | Meaning | May change through ordinary progression? |
|---|---|---|
| Character identity | Who this persisted person is | No |
| Authored character/type | The template/person they were deliberately created as | No |
| Origin/home biome | Where that type belongs in Living World lore | No silent rewrite |
| Role/profession | Guard, Archer, Scout and future jobs | Only through an explicit owner-approved system; never inferred |
| Combat doctrine/training | The learned modules and competence this person may use | Yes, within their identity/profile |
| Loadout | Weapons, shield, armor, food, ammunition and tools | Yes |
| Skills/knowledge | Practiced ability and legitimately learned facts | Yes |

A shared fact does not collapse the others. Two people may use a bow without being the
same class or character. Two biome tiers may use similar tactics without one replacing
the other.

## 2. The roster is additive across biome progression

The project needs early-game companions in the Meadows and Black Forest and additional,
similar-but-distinct characters for later biomes such as the Plains.

- **Guard remains Guard.** The early-game Guard is never discarded, renamed Warden or
  upgraded into a different person.
- **Archer remains Archer.** The early-game Archer is never discarded, renamed Huntress
  or upgraded into a different person.
- **Scout remains Scout.** Scout is not a fallback label or migration source for another
  class.
- **Huntress is her own authored character.** The planned Plains spear Huntress has her
  own profile, history, starting package, training and doctrine. An Archer never becomes
  Huntress.
- **Warden is distinct from Guard.** Similar defender responsibilities may share modules,
  but Warden is not an alias or migration destination for Guard.
- Other future characters—Legion/heavy, magic user, cleric/healer and additional biome
  people—are additions. Their arrival never deletes an earlier type.

When a biome assignment or character detail is not yet decided, mark it `UNDECIDED`.
Never fill the gap by converting the closest existing character.

## 3. Progression preserves the person

A Guard may reach the Plains, improve skills, learn enemies, obtain Plains gear and
remain the same Guard. An Archer may master better bows and remain the same Archer.
Progression is not evolution into Warden or Huntress.

The only way Huntress exists is by deliberately creating/spawning the Huntress profile.
There is no Archer-to-Huntress migration, reroll, compatibility alias or hidden upgrade.

## 4. Behavior reuse is encouraged without identity reuse

FormidableAI should share strong modules:

- perception and awareness;
- initiative and rules of engagement;
- block/parry/evade arbitration;
- bow, spear, shield and heavy-weapon competence;
- formation, partner protection and fallback planning;
- biome and enemy knowledge.

An Archer and Huntress may both consume a ranged-lane module. Guard and Warden may both
consume shield-defense logic. Each profile supplies different training, weights,
equipment, biome knowledge and tactics. The implementation shares code, never names or
identity.

## 5. Forbidden implementation patterns

The following are invalid even as “backward compatibility”:

- `guard -> Warden`;
- `archer -> Huntress`;
- `scout -> Warden` or any nearest-class fallback;
- assigning class from roster slot, weapon, shield, sex/gender, appearance or prefab;
- writing a new class ID when a legacy profile lacks one;
- making `Classes=false` select a different class doctrine;
- changing the default pair from existing characters because new classes were added;
- treating omission from a design table as deletion or supersession;
- installing a semantic choice that the handoff itself labels unresolved.

## 6. Required design and test evidence

Any class/profile change must show a before/after roster matrix with:

- stable character/type ID;
- display name/type;
- origin biome;
- role/profession;
- doctrine modules;
- starting package/loadout source;
- persistence behavior;
- whether the row is new, unchanged or explicitly owner-approved for change.

Required regression claims:

1. Loading never changes an existing identity or writes a replacement class.
2. Equipment changes never change identity.
3. Role/doctrine lookup never changes identity.
4. Disabling a feature returns exact prior behavior.
5. Adding a biome tier leaves all earlier roster rows intact.
6. No candidate is installed while any roster or identity decision remains unresolved.

## 7. Current correction

The 1.0.33.0 A2 candidate's automatic Guard-to-Warden and Archer-to-Huntress mappings
violate this contract. That candidate is not a valid test build. The installed profile
must return to 1.0.32.29 before gameplay, and the A2 source must separate identity from
shared doctrine before resubmission.

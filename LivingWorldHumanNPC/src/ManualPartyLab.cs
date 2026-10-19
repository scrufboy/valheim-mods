using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LivingWorld.HumanNpc;
using UnityEngine;

namespace LivingWorld.ManualTest
{
    public sealed class ManualPartyLab : MonoBehaviour
    {
        const string WaveTag = "lwc_party_wave_v1";
        const string WaveLedger = "lwc_party_wave_ledger_v1_";
        const string ProtectionTag = "lwc_party_piece_protection_v1";
        readonly HashSet<ZDOID> protectedPieces = new HashSet<ZDOID>();
        readonly List<ZDOID> enemies = new List<ZDOID>();
        static ManualPartyLab current;
        static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly MethodInfo Target = typeof(MonsterAI).GetMethod("SetTarget", Hidden, null, new[] { typeof(Character) }, null);
        static readonly MethodInfo Alert = typeof(BaseAI).GetMethod("SetAlerted", Hidden, null, new[] { typeof(bool) }, null);
        ZNetScene scene;
        bool bound;
        string armed;
        Vector3 origin, facing;
        float startAt;
        bool countdown;
        internal bool HasEncounter { get { return countdown || enemies.Count > 0; } }

        internal static void Install(Harmony harmony)
        {
            if (Target == null || Alert == null) throw new MissingMethodException("Native wave target contracts changed");
            var method = typeof(WearNTear).GetMethod("ApplyDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(float), typeof(HitData) }, null);
            if (method == null) throw new MissingMethodException("WearNTear.ApplyDamage");
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(ManualPartyLab), "BeforePieceDamage"));
        }

        internal void Command(string verb)
        {
            ManualTestPlugin.GuardWorld();
            BindScene();
            enabled = true;
            if (verb == "status") { Status(); return; }
            if (verb == "stop") { Stop(); return; }
            if (verb == "go")
            {
                if (armed == null || countdown || enemies.Count != 0) throw new InvalidOperationException("Arm one wave first; stop/finish existing wave");
                RequireParty(); CaptureProtection();
                origin = Player.m_localPlayer.transform.position;
                facing = Player.m_localPlayer.transform.forward; facing.y = 0; facing.Normalize();
                startAt = Time.time + 10; countdown = true;
                Report("Wave starts in 10 seconds. Close F5. lwtest wave stop cancels; no automatic next wave."); return;
            }
            if (verb != "boarneck" && verb != "greydwarf") throw new ArgumentException("wave boarneck | greydwarf | go | status | stop");
            if (countdown || enemies.Count != 0) throw new InvalidOperationException("Existing wave retained; use wave stop or wait for completion");
            RequireParty(); armed = verb;
            Report("ARMED " + verb + ". Nothing spawned. Use lwtest wave go when YOU are ready.");
        }

        void BindScene()
        {
            if (scene == ZNetScene.instance && bound) return;
            bound = false;
            countdown = false; armed = null; protectedPieces.Clear(); enemies.Clear();
            scene = ZNetScene.instance; current = this;
            // World metadata, not the real Player's ephemeral network object.
            // Union interrupted commits; stale deleted IDs are pruned by Update.
            foreach (var key in ZoneSystem.instance.GetGlobalKeys().Where(k => k.StartsWith(WaveLedger, StringComparison.Ordinal)))
            {
                string hex = key.Substring(WaveLedger.Length);
                if (hex.Length > 256 || hex.Length % 2 != 0) throw new InvalidOperationException("Invalid wave ledger preserved");
                byte[] bytes = new byte[hex.Length / 2];
                for (int i = 0; i < bytes.Length; i++) bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                var package = new ZPackage(bytes); int count = package.ReadInt();
                if (count < 0 || count > 16) throw new InvalidOperationException("Invalid wave ledger count preserved");
                for (int i = 0; i < count; i++) { var id = package.ReadZDOID(); if (!enemies.Contains(id)) enemies.Add(id); }
            }
            if (enemies.Count > 16) throw new InvalidOperationException("Conflicting wave ledgers preserved; do not spawn another wave");
            bound = true;
        }

        void SaveLedger()
        {
            var package = new ZPackage(); package.Write(enemies.Count);
            foreach (var id in enemies) package.Write(id);
            string next = WaveLedger + BitConverter.ToString(package.GetArray()).Replace("-", "").ToLowerInvariant();
            var old = ZoneSystem.instance.GetGlobalKeys().Where(k => k.StartsWith(WaveLedger, StringComparison.Ordinal)).ToArray();
            if (enemies.Count > 0) ZoneSystem.instance.SetGlobalKey(next);
            foreach (var key in old) if (enemies.Count == 0 || key != next) ZoneSystem.instance.RemoveGlobalKey(key);
        }

        void RequireParty()
        {
            var members = Character.GetAllCharacters().Where(c => c != null && c.GetComponent<HumanCompanion>() != null && !c.IsDead()).ToArray();
            if (!members.Any(c => HumanCleric.IsCleric(c.gameObject)) || !members.Any(c => c.GetComponent<HumanPartyEquipment>().Ready))
                throw new InvalidOperationException("Ready living cleric and prepared scout required. Use lwtest party prepare, then party status");
            foreach (var member in members)
            {
                long party; Player leader;
                if (!member.GetComponent<HumanCompanion>().TryGetConfidenceParty(out party, out leader) || leader != Player.m_localPlayer ||
                    Vector3.Distance(member.transform.position, Player.m_localPlayer.transform.position) > 12f)
                    throw new InvalidOperationException("Bring both owned companions within 12m of you before starting");
            }
        }

        internal void CaptureProtection()
        {
            ManualTestPlugin.GuardWorld(); BindScene();
            protectedPieces.Clear();
            foreach (var collider in Physics.OverlapSphere(Player.m_localPlayer.transform.position, 40))
            {
                var piece = collider.GetComponentInParent<Piece>();
                var view = piece == null ? null : piece.GetComponent<ZNetView>();
                if (view != null && view.IsValid() && view.IsOwner())
                { protectedPieces.Add(view.GetZDO().m_uid); view.GetZDO().Set(ProtectionTag, "1"); }
            }
            Report("Lab protection: " + protectedPieces.Count + " existing pieces within 40m marked for damage protection while this plugin is installed in the lab. Players/NPCs remain vulnerable; no monster-suppression zone.");
        }

        static bool BeforePieceDamage(WearNTear __instance, ref bool __result)
        {
            // Saved markers survive reload, but never apply outside the guarded lab.
            try { ManualTestPlugin.GuardWorld(); } catch { return true; }
            var view = __instance.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner() || view.GetZDO().GetString(ProtectionTag, "") != "1") return true;
            __result = false; return false;
        }

        void Update()
        {
            if (!bound || scene == null || scene != ZNetScene.instance) { countdown = false; protectedPieces.Clear(); return; }
            if (!countdown && enemies.Count == 0) return;
            try
            {
                ManualTestPlugin.GuardWorld();
                if (countdown && Time.time >= startAt) { countdown = false; SpawnWave(); }
                if (enemies.Count != 0)
                {
                    int removed = enemies.RemoveAll(id => ZDOMan.instance.GetZDO(id) == null);
                    if (removed > 0) { SaveLedger(); if (enemies.Count == 0) { Report("Wave entities finished. Survivors/resources retained. Next wave waits for your command."); PartyStatus(); } }
                }
            }
            catch (Exception error) { countdown = false; armed = null; Report("Wave paused/refused: " + error.Message); enabled = false; }
        }

        void SpawnWave()
        {
            RequireParty();
            if (Vector3.Distance(origin, Player.m_localPlayer.transform.position) > 8) throw new InvalidOperationException("Player moved too far from armed approach; re-arm safely");
            string[] names = armed == "boarneck" ? new[] { "Boar", "Neck", "Boar", "Neck", "Boar", "Neck", "Boar", "Neck", "Boar", "Neck", "Boar", "Neck" } :
                new[] { "Greydwarf", "Greydwarf", "Greydwarf", "Greydwarf", "Greydwarf", "Greydwarf", "Greydwarf", "Greydwarf" };
            var prefabs = new List<GameObject>();
            // Resolve every prefab before searching so a missing catalog entry
            // is never misreported as bad terrain.
            for (int i = 0; i < names.Length; i++)
            {
                var prefab = scene.GetPrefab(names[i]);
                var ai = prefab == null ? null : prefab.GetComponent<MonsterAI>();
                if (ai == null || prefab.GetComponent<Character>() == null) throw new InvalidOperationException("Native wave prefab missing: " + names[i]);
                prefabs.Add(prefab);
            }
            List<Vector3> positions; Vector3 spawnFacing;
            if (!TryFindWaveSite(names, prefabs, out positions, out spawnFacing))
                throw new InvalidOperationException("No dry, clear, reachable wave site found within 20-45m after searching around the player");
            string wave = armed; armed = null;
            for (int i = 0; i < names.Length; i++)
            {
                var actor = Instantiate(prefabs[i], positions[i], Quaternion.LookRotation(-spawnFacing));
                var view = actor.GetComponent<ZNetView>();
                if (view == null || !view.IsValid() || !view.IsOwner()) throw new InvalidOperationException("Spawn ownership refused; stop the partial wave");
                view.GetZDO().Set(WaveTag, "1"); enemies.Add(view.GetZDO().m_uid); SaveLedger();
                var ai = actor.GetComponent<MonsterAI>();
                Target.Invoke(ai, new object[] { Player.m_localPlayer }); Alert.Invoke(ai, new object[] { true });
            }
            Report("STARTED controlled " + wave + " wave: " + names.Length + " native enemies at an automatically selected dry, clear, reachable site. Not a native raid event. No stats/ammo/healing reset.");
            PartyStatus();
        }

        bool TryFindWaveSite(string[] names, List<GameObject> prefabs, out List<Vector3> positions, out Vector3 spawnFacing)
        {
            positions = null; spawnFacing = facing;
            if (Pathfinding.instance == null) return false;
            int columns = armed == "boarneck" ? 6 : 4;
            // Prefer the direction the tester faced, but search the full circle
            // at several radii. One obstructed point must never refuse a raid.
            for (float radius = 20f; radius <= 45f; radius += 5f)
            {
                for (int step = 0; step < 24; step++)
                {
                    int signed = step == 0 ? 0 : ((step + 1) / 2) * (step % 2 == 1 ? 1 : -1);
                    Vector3 direction = Quaternion.AngleAxis(signed * 15f, Vector3.up) * facing;
                    direction.y = 0f; direction.Normalize();
                    Vector3 side = Vector3.Cross(Vector3.up, direction);
                    var candidate = new List<Vector3>(); bool valid = true;
                    Vector3 approach;
                    try { approach = ManualTestPlugin.GroundPoint(origin + direction * 6f); }
                    catch { continue; }
                    for (int i = 0; i < names.Length; i++)
                    {
                        int row = i / columns, column = i % columns;
                        try
                        {
                            Vector3 at = ManualTestPlugin.GroundPoint(origin + direction * (radius + row * 4f) +
                                side * ((column - (columns - 1) * .5f) * 3f));
                            ManualTestPlugin.RequireClear(at, .8f);
                            var path = new List<Vector3>();
                            var ai = prefabs[i].GetComponent<MonsterAI>();
                            if (!Pathfinding.instance.GetPath(at, approach, path, ai.m_pathAgentType, true, true, false) ||
                                path.Count == 0 || Vector3.Distance(path[path.Count - 1], approach) > 3f)
                            { valid = false; break; }
                            candidate.Add(at);
                        }
                        catch { valid = false; break; }
                    }
                    if (!valid || candidate.Count != names.Length) continue;
                    positions = candidate; spawnFacing = direction;
                    Report("Wave site selected automatically: radius=" + radius + "m, search angle=" + (signed * 15) + " degrees.");
                    return true;
                }
            }
            return false;
        }

        void Stop()
        {
            countdown = false; armed = null;
            foreach (var id in enemies.ToArray())
            {
                var zdo = ZDOMan.instance.GetZDO(id);
                if (zdo == null) { enemies.Remove(id); continue; }
                if (zdo.GetString(WaveTag, "") != "1") throw new InvalidOperationException("Wave identity mismatch; object preserved");
                var actor = scene.FindInstance(id);
                if (actor == null) throw new InvalidOperationException("Wave enemy is unloaded; return to the lab before stopping");
                var view = actor.GetComponent<ZNetView>();
                if (!view.IsOwner()) throw new InvalidOperationException("Wave ownership unavailable; object preserved");
                scene.Destroy(actor);
            }
            enemies.RemoveAll(id => ZDOMan.instance.GetZDO(id) == null); SaveLedger();
            enabled = true;
            Report("Wave stop requested for recorded enemies only. Scout, cleric, player, buildings and resources preserved; remaining=" + enemies.Count);
        }
        void Status() { Report("Wave: armed=" + (armed ?? "none") + "; countdown=" + countdown + "; recorded enemies=" + enemies.Count + "; protected pieces=" + protectedPieces.Count); PartyStatus(); }
        void PartyStatus()
        {
            foreach (var character in Character.GetAllCharacters())
            {
                if (character == null || character.GetComponent<HumanCompanion>() == null) continue;
                Report("PARTY " + character.GetComponent<Humanoid>().m_name + " [" + character.GetZDOID() + "]; health=" + character.GetHealth() + "; " +
                    character.GetComponent<HumanPartyEquipment>().Describe() + "; " + character.GetComponent<HumanCleric>().Describe());
                Report(character.GetComponent<HumanResources>().Describe());
            }
        }
        void Report(string value) { ManualTestPlugin.Instance.Report(value); }
        void OnDestroy() { protectedPieces.Clear(); if (current == this) current = null; }
    }
}

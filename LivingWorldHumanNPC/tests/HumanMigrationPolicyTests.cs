using System;
using System.Collections.Generic;
using System.IO;
using LivingWorld.HumanNpc;

internal static class HumanMigrationPolicyTests
{
    static int checks;
    static void Check(bool value, string name) { if (!value) throw new Exception("FAIL " + name); checks++; }
    static HumanMigrationSnapshot Snapshot(string visual, string composition, string origin, string identity, string tag)
    { return new HumanMigrationSnapshot(visual, composition, origin, identity, tag); }
    static HumanMigrationSnapshot Legacy()
    { return Snapshot(HumanMigrationPolicy.VisualRevision, "", "", HumanMigrationPolicy.Identity, HumanMigrationPolicy.NpcTag); }
    static HumanMigrationSnapshot Current(string origin)
    { return Snapshot(HumanMigrationPolicy.VisualRevision, HumanMigrationPolicy.CompositionRevision, origin, HumanMigrationPolicy.Identity, HumanMigrationPolicy.NpcTag); }
    static HumanMigrationSnapshot Read(Dictionary<string, string> fields)
    {
        return Snapshot(fields[HumanMigrationPolicy.VisualKey], fields[HumanMigrationPolicy.CompositionKey], fields[HumanMigrationPolicy.OriginKey], fields[HumanMigrationPolicy.IdentityKey], fields[HumanMigrationPolicy.LabTagKey]);
    }
    static void Apply(Dictionary<string, string> fields, KeyValuePair<string, string>[] writes)
    { foreach (var pair in writes) fields[pair.Key] = pair.Value; }
    static Dictionary<string, string> Fields(HumanMigrationSnapshot s)
    {
        return new Dictionary<string, string> {
            { HumanMigrationPolicy.VisualKey, s.Visual }, { HumanMigrationPolicy.CompositionKey, s.Composition },
            { HumanMigrationPolicy.OriginKey, s.Origin }, { HumanMigrationPolicy.IdentityKey, s.Identity }, { HumanMigrationPolicy.LabTagKey, s.LabTag },
            { "native-health-sentinel", "147.5" }, { "native-position-sentinel", "5,-1,7" }, { "native-equipment-sentinel", "leather;sword;shield" },
            { "lab-built-sentinel", "true" }, { "prefab-stable-hash-sentinel", "unchanged" }, { "zdo-identity-sentinel", "uid:42" }
        };
    }
    static Dictionary<string, string> RoundTrip(Dictionary<string, string> source)
    {
        using (var data = new MemoryStream())
        {
            var writer = new BinaryWriter(data); writer.Write(source.Count);
            foreach (var pair in source) { writer.Write(pair.Key); writer.Write(pair.Value); }
            writer.Flush(); data.Position = 0;
            var reader = new BinaryReader(data); var result = new Dictionary<string, string>(); int count = reader.ReadInt32();
            for (int i = 0; i < count; i++) result.Add(reader.ReadString(), reader.ReadString());
            return result;
        }
    }
    static void Throws(Action action, string name)
    { try { action(); } catch (InvalidOperationException) { checks++; return; } throw new Exception("FAIL expected refusal: " + name); }
    public static int Main()
    {
        Check(HumanMigrationPolicy.Inspect(Legacy()) == HumanMigrationState.LegacyMigrationRequired, "known legacy requires opt-in");
        Check(HumanMigrationPolicy.Inspect(Current(HumanMigrationPolicy.FreshOrigin)) == HumanMigrationState.Ready, "current fresh accepted");
        Check(HumanMigrationPolicy.Inspect(Current(HumanMigrationPolicy.LegacyOrigin)) == HumanMigrationState.Ready, "current migrated accepted");
        var empty = Snapshot("", "", "", "", "");
        Check(HumanMigrationPolicy.Inspect(empty) == HumanMigrationState.Unstamped, "empty is not fresh by inference");
        Check(HumanMigrationPolicy.Inspect(null) == HumanMigrationState.Refused, "null snapshot");
        var bad = new[] {
            Snapshot(null,"","",HumanMigrationPolicy.Identity,"npc"), Snapshot("native-human-v2","","",HumanMigrationPolicy.Identity,"npc"),
            Snapshot("native-human-v1","future","",HumanMigrationPolicy.Identity,"npc"), Snapshot("native-human-v1","","","other","npc"),
            Snapshot("native-human-v1","","",HumanMigrationPolicy.Identity,"piece"), Snapshot("native-human-v1","","",HumanMigrationPolicy.Identity,""),
            Snapshot("native-human-v1","","","","npc"), Snapshot("NATIVE-HUMAN-V1","","",HumanMigrationPolicy.Identity,"npc"),
            Snapshot("native-human-v1",HumanMigrationPolicy.CompositionRevision,"",HumanMigrationPolicy.Identity,"npc"),
            Snapshot("native-human-v1",HumanMigrationPolicy.CompositionRevision,"unknown",HumanMigrationPolicy.Identity,"npc"),
            Snapshot("",HumanMigrationPolicy.CompositionRevision,HumanMigrationPolicy.LegacyOrigin,HumanMigrationPolicy.Identity,"npc"),
            Snapshot("native-human-v1","",HumanMigrationPolicy.FreshOrigin,HumanMigrationPolicy.Identity,"npc"),
            Snapshot("native-human-v1 ","","",HumanMigrationPolicy.Identity,"npc"), Snapshot("native-human-v1"," ","",HumanMigrationPolicy.Identity,"npc"),
            Snapshot("native-human-v1","",null,HumanMigrationPolicy.Identity,"npc")
        };
        foreach (var value in bad) {
            Check(HumanMigrationPolicy.Inspect(value) == HumanMigrationState.Refused, "unknown/corrupt frozen");
            Check(!HumanMigrationPolicy.MayMigrate(value, true, true, true), "unknown/corrupt not migrated");
            Check(!HumanMigrationPolicy.MayInitializeFresh(value, true, true, true), "unknown/corrupt not reset as fresh");
        }
        for (int flags = 0; flags < 8; flags++) {
            bool world = (flags & 1) != 0, owner = (flags & 2) != 0, consent = (flags & 4) != 0;
            Check(HumanMigrationPolicy.MayMigrate(Legacy(), world, owner, consent) == (flags == 7), "three migration guards");
            Check(HumanMigrationPolicy.MayInitializeFresh(empty, consent, world, owner) == (flags == 7), "three fresh guards");
        }
        Throws(() => HumanMigrationPolicy.PlanMigration(Legacy(), true, true, false), "no consent");
        Throws(() => HumanMigrationPolicy.PlanMigration(empty, true, true, true), "unclassified legacy");
        Throws(() => HumanMigrationPolicy.PlanMigration(Current(HumanMigrationPolicy.LegacyOrigin), true, true, true), "repeat migration");
        Throws(() => HumanMigrationPolicy.PlanFresh(Legacy(), true, true, true), "legacy cannot become fresh");
        Throws(() => HumanMigrationPolicy.PlanFresh(empty, false, true, true), "not verified new spawn");
        var oldFields = Fields(Legacy()); var fields = RoundTrip(oldFields);
        var plan = HumanMigrationPolicy.PlanMigration(Read(fields), true, true, true);
        Check(plan.Length == 2 && plan[1].Key == HumanMigrationPolicy.CompositionKey, "exact two writes; composition commit last");
        fields[plan[0].Key] = plan[0].Value;
        Check(HumanMigrationPolicy.Inspect(Read(fields)) == HumanMigrationState.LegacyMigrationRequired, "partial migration remains frozen and retryable");
        Apply(fields, HumanMigrationPolicy.PlanMigration(Read(fields), true, true, true));
        fields = RoundTrip(fields);
        Check(HumanMigrationPolicy.Inspect(Read(fields)) == HumanMigrationState.Ready, "metadata round-trip accepts committed migration");
        foreach (var pair in oldFields) if (pair.Key != HumanMigrationPolicy.CompositionKey && pair.Key != HumanMigrationPolicy.OriginKey)
            Check(fields[pair.Key] == pair.Value, "migration preserves unrelated field " + pair.Key);
        Check(!HumanMigrationPolicy.MayInitializeFresh(Read(fields), true, true, true), "migrated cannot be reinitialized");
        var freshFields = Fields(empty); Apply(freshFields, HumanMigrationPolicy.PlanFresh(empty, true, true, true));
        Check(HumanMigrationPolicy.Inspect(Read(freshFields)) == HumanMigrationState.CurrentAppearancePending, "fresh does not preempt visual initialization");
        freshFields[HumanMigrationPolicy.VisualKey] = HumanMigrationPolicy.VisualRevision;
        Check(HumanMigrationPolicy.Inspect(Read(RoundTrip(freshFields))) == HumanMigrationState.Ready, "fresh round-trip ready after visual initialization");
        Check(HumanMigrationPolicy.MayInitializeFresh(Snapshot("","","",HumanMigrationPolicy.Identity,"npc"), true, true, true), "trusted spawn may have existing exact tags");
        Console.WriteLine("HumanMigrationPolicyTests PASS: " + checks + " pure metadata checks; native Unity load/migration NOT RUN.");
        return 0;
    }
}

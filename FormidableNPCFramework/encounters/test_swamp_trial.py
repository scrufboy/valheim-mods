import copy
import json
import tempfile
import unittest
from pathlib import Path

from build_swamp_trial import BlockedBuild, EVENT, compile_trial, main


class SwampTrialTests(unittest.TestCase):
    def setUp(self):
        self.inventory = json.loads(Path(__file__).with_name("swamp_inventory.json").read_text())

    def complete(self):
        result = copy.deepcopy(self.inventory)
        for role in ("shaman", "blowgun"):
            # Synthetic fixtures only; never shipped as a real inventory.
            result["actors"][role] = {"prefab": "Fixture_" + role, "registered": True,
                                      "evidence": "synthetic unit test"}
        result["native_shaman_control"].update(ready=True,
            single_decision_per_encounter=True, configured_beast_chance_percent=25,
            evidence="synthetic native executor acceptance receipt")
        result["raid_faction_passthrough"] = {
            "verified": True, "evidence": "synthetic verified scoped passthrough fixture"}
        return result

    def test_current_roster_explicitly_blocks(self):
        with self.assertRaisesRegex(BlockedBuild, "shaman|blowgun"):
            compile_trial(self.inventory)

    def test_no_partial_file_on_block(self):
        with tempfile.TemporaryDirectory() as folder:
            output = Path(folder) / "candidate.cfg"
            with self.assertRaises(SystemExit) as failure:
                main(["--inventory", str(Path(__file__).with_name("swamp_inventory.json")),
                      "--output", str(output)])
            self.assertEqual(failure.exception.code, 2)
            self.assertFalse(output.exists())

    def test_manual_only_and_exact_actor_ids(self):
        result = compile_trial(self.complete())
        self.assertIn("Name = " + EVENT, result)
        self.assertIn("Random = false", result)
        self.assertIn("PrefabName = RRRN_Neckman", result)
        self.assertIn("PrefabName = LWC_MarshMarauder_Prototype", result)
        self.assertNotIn("PrefabName = Goblin", result)
        self.assertNotIn("Faction =", result)
        self.assertNotIn("Bonemass", result.splitlines()[1:])

    def test_full_trial_always_needs_beast_identity(self):
        roster = self.complete()
        del roster["actors"]["beast"]
        roster["native_shaman_control"]["configured_beast_chance_percent"] = 0
        with self.assertRaisesRegex(BlockedBuild, "beast"):
            compile_trial(roster)

    def test_no_raid_owned_beast_writer(self):
        result = compile_trial(self.complete())
        self.assertNotIn("PrefabName = LWC_UndeadColossus_Test", result)
        self.assertNotIn("Name = Formidable beast", result)
        self.assertEqual(result.count("PrefabName ="), 4)

    def test_nonzero_chance_requires_verified_beast(self):
        roster = self.complete()
        roster["actors"]["beast"]["registered"] = False
        with self.assertRaisesRegex(BlockedBuild, "beast"):
            compile_trial(roster)

    def test_chance_bounds(self):
        for bad in (-1, 101, float("nan"), float("inf"), True, "25"):
            with self.subTest(bad=bad), self.assertRaises(ValueError):
                roster = self.complete()
                roster["native_shaman_control"]["configured_beast_chance_percent"] = bad
                compile_trial(roster)
        roster = self.complete()
        roster["native_shaman_control"]["configured_beast_chance_percent"] = 100
        compile_trial(roster)

    def test_native_executor_gate_even_with_complete_roster(self):
        for field, value in (("ready", False), ("evidence", ""),
                             ("single_decision_per_encounter", False),
                             ("event", "other_event"), ("beast_prefab", "other_beast")):
            roster = self.complete()
            roster["native_shaman_control"][field] = value
            with self.subTest(field=field), self.assertRaisesRegex(BlockedBuild, "executor"):
                compile_trial(roster)

    def test_installed_forced_faction_blocks_even_if_actors_and_executor_ready(self):
        roster = self.complete()
        roster["raid_faction_passthrough"] = self.inventory["raid_faction_passthrough"]
        with self.assertRaisesRegex(BlockedBuild, "faction passthrough"):
            compile_trial(roster)

    def test_identity_and_evidence_required(self):
        for value in ("Goblin\nEnabled = true", None, "../bad"):
            roster = self.complete()
            roster["actors"]["shaman"]["prefab"] = value
            with self.assertRaises(BlockedBuild):
                compile_trial(roster)
        roster = self.complete()
        roster["actors"]["shaman"]["evidence"] = ""
        with self.assertRaises(BlockedBuild):
            compile_trial(roster)

    def test_no_duplicate_role_substitution(self):
        roster = self.complete()
        roster["actors"]["shaman"]["prefab"] = "RRRN_Neckman"
        with self.assertRaises(BlockedBuild):
            compile_trial(roster)


if __name__ == "__main__":
    unittest.main()

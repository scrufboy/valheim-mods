import json
from pathlib import Path
import tempfile
import unittest
from import_souls_roster import read_roster, write_inventory

class ImportTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.root / "source"
        self.source.mkdir()
    def save(self, name="Neckman", **extra):
        value = {"sNewPrefabName": name, "sOriginalPrefabName": "RRR_NPC",
                 "Category_Character": {"sName": "Swamp Neckman"},
                 "Category_Humanoid": {"aRandomWeapon": ["Bow", "Bow", "Spear"], "aDefaultItems": ["@1", "SageRobeRed"]},
                 "futureField": {"doNotLose": True}}
        value.update(extra)
        path = self.source / ("rrrnpcs.custom." + name + ".json")
        path.write_text(json.dumps(value), encoding="utf-8")
        return path
    def test_preserves_weights_unknown_fields_and_identity(self):
        self.save()
        row = read_roster(self.source)["actors"][0]
        self.assertEqual(row["equipment"]["aRandomWeapon"], ["Bow", "Bow", "Spear"])
        self.assertTrue(row["sourceDefinition"]["futureField"]["doNotLose"])
        self.assertEqual(row["identity"], "Neckman")
        self.assertEqual(row["legacySpecialTokens"], ["@1"])
        self.assertEqual(row["legacyRuntimeReferences"], ["RRR_NPC"])
        self.assertFalse(row["status"]["formidableAIIntegrated"])
    def test_imports_every_definition_without_roster_filter(self):
        for name in ("Neckman", "Servant", "WoodsSoul", "Pillager", "NinoBosque", "OrcBrute"):
            self.save(name)
        self.assertEqual(len(read_roster(self.source)["actors"]), 6)
    def test_rejects_duplicate_identity(self):
        self.save()
        other = self.save("Other")
        value = json.loads(other.read_text())
        value["sNewPrefabName"] = "neckman"
        other.write_text(json.dumps(value))
        with self.assertRaises(ValueError):
            read_roster(self.source)
    def test_rejects_duplicate_json_keys(self):
        self.save().write_text('{"sNewPrefabName":"A","sNewPrefabName":"B"}')
        with self.assertRaises(ValueError):
            read_roster(self.source)
    def test_rejects_nonfinite_and_missing_identity(self):
        self.save(Category_Character={"fHealth": float("nan")})
        with self.assertRaises(ValueError):
            read_roster(self.source)
    def test_output_never_overwrites_source_or_prior_output(self):
        self.save()
        with self.assertRaises(ValueError):
            write_inventory(self.source, self.source / "bad")
        output = self.root / "candidate"
        result = write_inventory(self.source, output)
        self.assertFalse(result["installable"])
        with self.assertRaises(ValueError):
            write_inventory(self.source, output)
    def test_rejects_profile_destination(self):
        self.save()
        with self.assertRaises(ValueError):
            write_inventory(self.source, self.root / "r2modmanPlus-local" / "output")

if __name__ == "__main__":
    unittest.main()

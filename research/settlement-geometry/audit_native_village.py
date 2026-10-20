"""Read-only, selected native village geometry audit. Never starts Unity.

Requires an existing UnityPy installation, passed with --python-libs.
Only stdout is written. No assets, prefab graphs, game code or world saves are
exported. Bounds are serialized-state envelopes, NOT navigation guarantees.
"""
import argparse
from collections import Counter
import hashlib
import itertools
import json
import math
import re
import sys
from pathlib import Path


def identity():
    return [[float(i == j) for j in range(4)] for i in range(4)]


def multiply(a, b):
    return [[sum(a[i][k] * b[k][j] for k in range(4))
             for j in range(4)] for i in range(4)]


def xyz(d):
    return [d[k] for k in "xyz"]


def trs(d):
    x, y, z, w = [d["m_LocalRotation"][k] for k in "xyzw"]
    n = math.sqrt(x*x + y*y + z*z + w*w)
    if not n:
        raise ValueError("Invalid zero quaternion")
    x, y, z, w = x/n, y/n, z/n, w/n
    r = [[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w), 0],
         [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w), 0],
         [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y), 0],
         [0, 0, 0, 1]]
    scale = xyz(d["m_LocalScale"])
    pos = xyz(d["m_LocalPosition"])
    for i in range(3):
        for j in range(3):
            r[i][j] *= scale[j]
        r[i][3] = pos[i]
    return r


def point(m, p):
    return [sum(m[i][j] * p[j] for j in range(3)) + m[i][3]
            for i in range(3)]


def box(m, center, size):
    corners = [point(m, [center[i] + signs[i]*size[i]/2 for i in range(3)])
               for signs in itertools.product((-1, 1), repeat=3)]
    return ([min(p[i] for p in corners) for i in range(3)],
            [max(p[i] for p in corners) for i in range(3)])


def envelope(bounds):
    if not bounds:
        return None
    lo = [min(b[0][i] for b in bounds) for i in range(3)]
    hi = [max(b[1][i] for b in bounds) for i in range(3)]
    return {"min_xyz": [round(v, 4) for v in lo],
            "max_xyz": [round(v, 4) for v in hi],
            "size_xyz": [round(hi[i]-lo[i], 4) for i in range(3)]}


def asset_id(d):
    return "".join(format(d[k], "08x") for k in ("v3", "v2", "v1", "v0"))


class Audit:
    def __init__(self, data, env=None, loaded=()):
        import UnityPy
        from UnityPy.classes.PPtr import PPtr
        self.PPtr = PPtr
        self.data = Path(data)
        self.bundles = self.data / "StreamingAssets/SoftRef/Bundles"
        self.env = env if env is not None else UnityPy.Environment()
        self.loaded = set(loaded)
        self.assets = {}
        self.dependencies = {}
        self.meshes = {}
        self.scripts = {}
        self.unresolved = set()
        defaults = self.data / "Resources/unity default resources"
        if defaults.exists():
            self.env.load_file(str(defaults))
        for name in ("manifest_extended", "manifest"):
            text = (self.bundles.parent / name).read_text(encoding="utf-8")
            for a, b, p in re.findall(
                r"- asset ID: (\w+)\n  bundle: (\w+)\n  path in bundle: (.+)", text
            ):
                self.assets[a] = (b, p)
            for b, lines in re.findall(
                r"- bundle: (\w+)\n  dependencies:[^\n]*\n((?:  - [^\n]+\n)*)", text
            ):
                self.dependencies[b] = re.findall(r"  - (\w+)", lines)

    def load(self, name):
        if name in self.loaded:
            return
        self.loaded.add(name)
        for dep in self.dependencies.get(name, []):
            self.load(dep)
        self.env.load_file(str(self.bundles / name))

    def prefab(self, path):
        matches = [(a, b, p) for a, (b, p) in self.assets.items() if p == path]
        if len(matches) != 1:
            raise ValueError("Ambiguous/missing manifest entry: " + path)
        a, b, p = matches[0]
        self.load(b)
        return self.env.container[p].deref(), a, b

    def ref(self, d, owner):
        return self.PPtr(m_FileID=d["m_FileID"], m_PathID=d["m_PathID"],
                         assetsfile=owner.assets_file).deref()

    def components(self, obj):
        return [self.ref(c["component"], obj)
                for c in obj.read_typetree()["m_Component"]]

    def kind(self, obj, fields):
        if obj.type.name != "MonoBehaviour":
            return obj.type.name
        p = fields["m_Script"]
        key = (obj.assets_file.name, p["m_FileID"], p["m_PathID"])
        if key not in self.scripts:
            try:
                self.scripts[key] = self.ref(p, obj).read_typetree()["m_ClassName"]
            except (FileNotFoundError, KeyError) as exc:
                self.unresolved.add("Script: " + str(exc))
                self.scripts[key] = "UnresolvedMonoBehaviour"
        return self.scripts[key]

    def graph(self, root):
        result = []
        visited = set()

        def walk(obj, parent_matrix, parent_path, active, is_root=False):
            key = (obj.assets_file.name, obj.path_id)
            if key in visited:
                raise ValueError("Repeated/cyclic GameObject")
            visited.add(key)
            d = obj.read_typetree()
            path = parent_path + "/" + d["m_Name"] if parent_path else d["m_Name"]
            comps = [(c, c.read_typetree()) for c in self.components(obj)]
            tr, td = next((c, cd) for c, cd in comps if c.type.name == "Transform")
            # Catalog coordinates are relative to the root, not its authoring position.
            matrix = identity() if is_root else multiply(parent_matrix, trs(td))
            # Root activation is a template-storage detail. Descendant active flags count.
            active = True if is_root else bool(active and d["m_IsActive"])
            result.append({"path": path, "name": d["m_Name"], "matrix": matrix,
                           "active": active, "components": [
                               (self.kind(c, cd), c, cd) for c, cd in comps]})
            for p in td["m_Children"]:
                child = self.ref(p, tr)
                walk(self.ref(child.read_typetree()["m_GameObject"], child),
                     matrix, path, active)
        walk(root, identity(), "", True, True)
        return result

    def mesh_box(self, p, owner, matrix):
        if not p["m_PathID"]:
            return None
        key = (owner.assets_file.name, p["m_FileID"], p["m_PathID"])
        if key not in self.meshes:
            try:
                self.meshes[key] = self.ref(p, owner).read_typetree()["m_LocalAABB"]
            except (FileNotFoundError, KeyError) as exc:
                self.unresolved.add("Mesh: " + str(exc))
                self.meshes[key] = None
        aabb = self.meshes[key]
        if not aabb:
            return None
        return box(matrix, xyz(aabb["m_Center"]),
                   [2*v for v in xyz(aabb["m_Extent"])])

    def summarize(self, root):
        graph = self.graph(root)
        collision, visuals, floors = [], [], []
        features, rooms = [], []
        collider_count = 0
        for n in graph:
            matrix = n["matrix"]
            pos = [round(v, 4) for v in point(matrix, [0, 0, 0])]
            rendered = any(k == "MeshRenderer" and d.get("m_Enabled", 1)
                           for k, _, d in n["components"])
            for kind, c, d in n["components"]:
                if kind == "Room":
                    rooms.append({k: d[k] for k in (
                        "m_size", "m_theme", "m_enabled", "m_faceCenter",
                        "m_perimeter", "m_weight", "m_entrance", "m_endCap")})
                bound = None
                if n["active"] and d.get("m_Enabled", 1):
                    if not d.get("m_IsTrigger", 0):
                        if kind == "BoxCollider":
                            bound = box(matrix, xyz(d["m_Center"]), xyz(d["m_Size"]))
                        elif kind == "MeshCollider":
                            bound = self.mesh_box(d["m_Mesh"], c, matrix)
                        elif kind == "SphereCollider":
                            radius = d["m_Radius"] * max(
                                math.sqrt(sum(matrix[i][j]**2 for i in range(3)))
                                for j in range(3))
                            center = point(matrix, xyz(d["m_Center"]))
                            bound = box(identity(), center, [2*radius]*3)
                        elif kind.endswith("Collider"):
                            self.unresolved.add("Unsupported collider: " + kind)
                        if bound:
                            collision.append(bound)
                            collider_count += 1
                            if "/wood_floor" in n["path"]:
                                floors.append(bound)
                    if kind == "MeshFilter" and rendered:
                        visual = self.mesh_box(d["m_Mesh"], c, matrix)
                        if visual:
                            visuals.append(visual)
                if kind in ("Door", "Bed", "CreatureSpawner", "SpawnArea",
                            "RoomConnection", "TerrainModifier", "RandomSpawn"):
                    feature = {"kind": kind, "path": n["path"], "xyz": pos,
                               "active": n["active"], "fields": {}}
                    for k in (
                        "m_type", "m_entrance", "m_allowDoor", "m_checkGuardStone",
                        "m_canNotBeClosed", "m_chanceToSpawn", "m_spawnChance",
                        "m_level", "m_levelOffset", "m_radius", "m_levelRadius",
                        "m_smoothRadius", "m_square", "m_paintRadius",
                        "m_respawnTimeMinuts", "m_triggerDistance",
                    ):
                        if k in d:
                            feature["fields"][k] = d[k]
                    if kind == "CreatureSpawner":
                        p = d.get("m_creaturePrefab")
                        if p and p["m_PathID"]:
                            feature["prefab"] = self.ref(p, c).read_typetree()["m_Name"]
                    if kind == "RoomConnection":
                        feature["forward"] = [round(matrix[i][2], 4) for i in range(3)]
                    features.append(feature)
        return {"root": root.read_typetree()["m_Name"], "room": rooms,
                "node_count": len(graph), "collider_count": collider_count,
                "serialized_collision_envelope": envelope(collision),
                "serialized_render_envelope": envelope(visuals),
                "wood_floor_collision_envelope": envelope(floors),
                "wood_floor_collider_top_y_counts": dict(Counter(
                    round(b[1][1], 4) for b in floors)),
                "feature_counts": dict(Counter(f["kind"] for f in features)),
                "features": features}

    def primitive_boxes(self, path):
        root, aid, bn = self.prefab(path)
        boxes = []
        for n in self.graph(root):
            for kind, _, d in n["components"]:
                if kind == "BoxCollider" and n["active"] and d.get("m_Enabled", 1):
                    if not d.get("m_IsTrigger", 0):
                        boxes.append({
                            "path": n["path"],
                            "bounds": envelope([box(n["matrix"], xyz(d["m_Center"]),
                                                    xyz(d["m_Size"]))])})
        return {"asset_id": aid, "bundle": bn, "boxes": boxes}

    def village(self):
        self.unresolved.clear()
        dg, aid, bn = self.prefab("Assets/world/dungeon/DG_MeadowsVillage.prefab")
        generator = next(c.read_typetree() for c in self.components(dg)
                         if "m_algorithm" in c.read_typetree())
        rl, _, _ = self.prefab("Assets/Systems/RoomLists/_RoomList.prefab")
        registry = next(c.read_typetree()["m_rooms"] for c in self.components(rl)
                        if "m_rooms" in c.read_typetree())
        output = []
        for entry in registry:
            if not (entry["m_theme"] & generator["m_themes"]):
                continue
            key = asset_id(entry["m_prefab"]["m_assetID"])
            if key not in self.assets:
                self.unresolved.add("Room asset ID: " + key)
                continue
            bn, path = self.assets[key]
            root, _, _ = self.prefab(path)
            summary = self.summarize(root)
            summary.update(asset_id=key, bundle=bn, registry_enabled=entry["m_enabled"])
            output.append(summary)
        wrapper, wrapper_id, wrapper_bundle = self.prefab(
            "Assets/world/Locations/Meadows/WoodVillage1.prefab")
        wrapper_summary = []
        for n in self.graph(wrapper):
            for kind, _, d in n["components"]:
                if kind in ("Location", "DungeonGenerator"):
                    wrapper_summary.append({
                        "path": n["path"], "kind": kind,
                        "xyz": point(n["matrix"], [0, 0, 0]),
                        "fields": {k: v for k, v in d.items() if k not in (
                            "m_GameObject", "m_Script", "m_doorTypes",
                            "m_generator", "m_interiorPrefab", "m_interiorTransform")}
                    })
        primitives = {}
        for name in ("wood_door", "wood_gate", "wood_floor", "wood_floor_1x1"):
            primitives[name] = self.primitive_boxes(
                "Assets/GameElements/Pieces/" + name + ".prefab")
        return {"schema": "lwc.native-village-audit.v1",
                "units": "Unity meters; root-local x,y,z; serialized-state only",
                "assembly_sha256": hashlib.sha256(
                    (self.data / "Managed/assembly_valheim.dll").read_bytes()).hexdigest(),
                "wrapper": {"asset_id": wrapper_id, "bundle": wrapper_bundle,
                            "components": wrapper_summary},
                "generator": {k: v for k, v in generator.items()
                              if k not in ("m_GameObject", "m_Script", "m_doorTypes")},
                "modules": output, "primitive_boxes": primitives,
                "unresolved": sorted(self.unresolved)}


def self_test():
    assert envelope([box(identity(), [1, 2, 3], [2, 4, 6])]) == {
        "min_xyz": [0, 0, 0], "max_xyz": [2, 4, 6], "size_xyz": [2, 4, 6]}
    d = {"m_LocalRotation": dict(x=0, y=math.sqrt(.5), z=0, w=math.sqrt(.5)),
         "m_LocalPosition": dict(x=10, y=1, z=-5),
         "m_LocalScale": dict(x=2, y=3, z=4)}
    b = envelope([box(trs(d), [0, 0, 0], [2, 2, 2])])
    assert b["size_xyz"] == [8, 6, 4] and b["min_xyz"] == [6, -2, -7]
    parent = identity()
    parent[0][3] = 5
    assert point(multiply(parent, trs(d)), [0, 0, 0]) == [15, 1, -5]
    assert envelope([]) is None
    assert asset_id(dict(v3=1, v2=2, v1=3, v0=4)) == (
        "00000001000000020000000300000004")
    print("PASS: identity bounds; rotated nonuniform scale; parent translation;"
          " empty bounds; SoftRef ID order")


if __name__ == "__main__":
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--data", type=Path)
    p.add_argument("--python-libs", type=Path)
    p.add_argument("--self-test", action="store_true")
    args = p.parse_args()
    if args.self_test:
        self_test()
    else:
        if args.data is None or args.python_libs is None:
            p.error("--data and --python-libs are required")
        sys.path.insert(0, str(args.python_libs))
        result = Audit(args.data).village()
        print(json.dumps(result, indent=2))
        if result["unresolved"]:
            sys.exit(2)

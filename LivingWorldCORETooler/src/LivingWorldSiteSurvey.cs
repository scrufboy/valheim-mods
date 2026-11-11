using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;

namespace Scrufboy.LivingWorld.Tooler
{
    // Isolated from NPC, raid, combat, and settlement behavior. This module observes live terrain only.
    internal static class SiteSurveyPanel
    {
        const int MaxSamples = 4096;
        const float CampusLong = 224f;
        const float CampusShort = 128f;
        static readonly string buildVersion = LivingWorldCoreToolerPlugin.PluginVersion;
        static readonly List<SurveyCorner> corners = new List<SurveyCorner>();
        static readonly List<SurveyCell> cells = new List<SurveyCell>();
        static readonly List<GameObject> preview = new List<GameObject>();
        static SurveyResult result;
        static long worldUid;
        static int worldSeed;
        static string worldName = "", profileName = "", startedUtc = "";
        static float grid = 4f;
        static bool leftHeld, rightHeld;
        static int lastControllerFrame = -1;

        internal static void DisplayGUI()
        {
            UpdateController();
            GUILayout.Space(8);
            GUILayout.Label("Site Survey", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
            GUILayout.Label("Walk the four corners. The Tooler records your exact horizontal position and resolves live altered ground—never seed-inferred terrain.", GUI.skin.box);
            GUILayout.Label("Controller: A = next stage / mark corner, X = calculate or preview, Y = export, B = clear preview, LB/RB = grid size.", GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Begin Survey", GUILayout.Height(44))) Begin();
            if (GUILayout.Button("Mark Corner " + Math.Min(4, corners.Count + 1), GUILayout.Height(44))) MarkCorner();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Calculate", GUILayout.Height(44))) Calculate();
            if (GUILayout.Button("Preview Campus", GUILayout.Height(44))) PreviewCampus();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Export Plan", GUILayout.Height(44))) Export();
            if (GUILayout.Button("Clear Preview", GUILayout.Height(44))) ClearPreview();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Live-ground grid: " + grid.ToString("0", CultureInfo.InvariantCulture) + " m", GUILayout.Width(180));
            if (GUILayout.Button("2 m (bounded)", GUILayout.Height(34))) SetGrid(2f);
            if (GUILayout.Button("4 m default", GUILayout.Height(34))) SetGrid(4f);
            if (GUILayout.Button("8 m fast", GUILayout.Height(34))) SetGrid(8f);
            GUILayout.EndHorizontal();
            GUILayout.Label(StatusText(), GUI.skin.box);
            GUILayout.Space(8);
            GUILayout.Label("Terrain application", new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold });
            GUI.enabled = false;
            GUILayout.Button("Apply Terrain — LOCKED (requires backup receipt + second confirmation)", GUILayout.Height(44));
            GUI.enabled = true;
            GUILayout.Label("Current application gate: " + TerrainApplicationBlockedReason(), GUI.skin.box);
            GUILayout.Label("2.1.0 is survey/preview/export only. No terrain modifier is created. A future application build must refuse combat, clients, open servers, or connected peers; chunk and journal every cell; support cancellation; and verify a world-backup receipt before confirmation.", GUI.skin.box);
        }

        static void UpdateController()
        {
            if (lastControllerFrame == Time.frameCount) return;
            lastControllerFrame = Time.frameCount;
            if (Input.GetKeyDown(KeyCode.JoystickButton0)) PrimaryControllerAction();
            if (Input.GetKeyDown(KeyCode.JoystickButton2)) { if (result == null) Calculate(); else PreviewCampus(); }
            if (Input.GetKeyDown(KeyCode.JoystickButton3)) Export();
            if (Input.GetKeyDown(KeyCode.JoystickButton1)) ClearPreview();
            bool left = Input.GetKey(KeyCode.JoystickButton4), right = Input.GetKey(KeyCode.JoystickButton5);
            if (left && !leftHeld) SetGrid(grid <= 2f ? 2f : (grid <= 4f ? 2f : 4f));
            if (right && !rightHeld) SetGrid(grid >= 8f ? 8f : (grid >= 4f ? 8f : 4f));
            leftHeld = left; rightHeld = right;
        }

        static void PrimaryControllerAction()
        {
            if (String.IsNullOrEmpty(startedUtc)) Begin();
            else if (corners.Count < 4) MarkCorner();
            else if (result == null) Calculate();
            else if (preview.Count == 0) PreviewCampus();
            else Export();
        }

        static void Begin()
        {
            if (!WorldReady()) return;
            ClearPreview(); corners.Clear(); cells.Clear(); result = null;
            worldUid = ZNet.instance.GetWorldUID();
            worldSeed = WorldGenerator.instance == null ? 0 : WorldGenerator.instance.GetSeed();
            worldName = ZNet.instance.GetWorldName();
            var profile = Game.instance == null ? null : Game.instance.GetPlayerProfile();
            profileName = profile == null ? "unknown" : profile.GetFilename();
            startedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            Report("Site Survey begun. World identity recorded; live altered terrain is authoritative. Walk to Corner 1 and press Mark Corner (controller A).");
        }

        static void MarkCorner()
        {
            if (!WorldReady() || String.IsNullOrEmpty(startedUtc)) { Report("Press Begin Survey first."); return; }
            if (!SameWorld()) { Report("Survey refused: the world changed after Begin Survey."); return; }
            if (corners.Count >= 4) { Report("All four corners are recorded. Press Calculate."); return; }
            Vector3 horizontal = Player.m_localPlayer.transform.position;
            float ground; RaycastHit hit;
            if (!TryGround(horizontal, out ground, out hit)) { Report("Corner refused: live ground could not be resolved here."); return; }
            float water = ZoneSystem.instance.m_waterLevel;
            var c = new SurveyCorner();
            c.Index = corners.Count + 1; c.Position = new Vector3(horizontal.x, ground, horizontal.z);
            c.Ground = ground; c.Water = water; c.Depth = Mathf.Max(0f, water - ground);
            c.Biome = Heightmap.FindBiome(c.Position).ToString();
            c.Heading = Player.m_localPlayer.transform.eulerAngles.y;
            c.Utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            corners.Add(c); result = null; cells.Clear(); ClearPreview();
            Report("Corner " + c.Index + " recorded at X " + F(c.Position.x) + ", Z " + F(c.Position.z) + ", live ground " + F(ground) + ".");
        }

        static void Calculate()
        {
            if (!SameWorld() || corners.Count != 4) { Report("Calculate requires four corners from the current world."); return; }
            string error;
            result = SurveyGeometry.Calculate(corners, out error);
            if (result == null) { Report("Survey invalid: " + error); return; }
            if (grid == 2f && !FineGridSafe(result)) { grid = 4f; Report("2 m sampling exceeded the bounded safety limit; using 4 m."); }
            if (!SampleLiveGround(result)) { result = null; return; }
            result.Cells = cells;
            result.FitsCampus = result.Width >= CampusShort && result.Length >= CampusLong;
            CalculateGrade(result);
            Report("Survey calculated: " + F(result.Length) + " × " + F(result.Width) + " m, " + cells.Count + " live samples, elevation range " + F(result.MaxHeight - result.MinHeight) + " m, campus " + (result.FitsCampus ? "FITS" : "DOES NOT FIT") + ".");
        }

        static bool SampleLiveGround(SurveyResult r)
        {
            cells.Clear();
            int nx = Mathf.FloorToInt(r.Length / grid) + 1, nz = Mathf.FloorToInt(r.Width / grid) + 1;
            if (nx * nz > MaxSamples) { Report("Sampling refused: " + (nx * nz) + " cells exceeds the " + MaxSamples + "-cell bound. Increase grid size."); return false; }
            float water = ZoneSystem.instance.m_waterLevel;
            for (int ix = 0; ix < nx; ix++) for (int iz = 0; iz < nz; iz++)
            {
                float u = nx == 1 ? 0f : -r.Length * .5f + r.Length * ix / (nx - 1);
                float v = nz == 1 ? 0f : -r.Width * .5f + r.Width * iz / (nz - 1);
                Vector3 p = r.Center + r.AxisLong * u + r.AxisShort * v;
                float h; RaycastHit hit;
                if (!TryGround(p, out h, out hit)) continue;
                var cell = new SurveyCell { X = p.x, Z = p.z, Ground = h, Water = water, Depth = Mathf.Max(0f, water - h), Biome = Heightmap.FindBiome(new Vector3(p.x, h, p.z)).ToString() };
                cell.Blocker = BlockingAt(new Vector3(p.x, h, p.z)); cells.Add(cell);
            }
            if (cells.Count < 4) { Report("Sampling refused: insufficient live ground results."); return false; }
            return true;
        }

        static string BlockingAt(Vector3 p)
        {
            Collider[] hits = Physics.OverlapBox(p + Vector3.up, new Vector3(1.25f, 1.5f, 1.25f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i]; if (c == null || c.GetComponentInParent<Player>() != null || c.GetComponentInParent<LivingWorldSurveyPreviewMarker>() != null) continue;
                if (c.gameObject.layer == LayerMask.NameToLayer("terrain")) continue;
                Piece piece = c.GetComponentInParent<Piece>();
                if (piece != null) return "piece:" + Safe(piece.name);
                if (c.gameObject.isStatic) return "static:" + Safe(c.name);
            }
            return "";
        }

        static void CalculateGrade(SurveyResult r)
        {
            float min = Single.MaxValue, max = Single.MinValue, sum = 0f;
            for (int i = 0; i < r.Cells.Count; i++) { float h = r.Cells[i].Ground; min = Mathf.Min(min, h); max = Mathf.Max(max, h); sum += h; }
            r.MinHeight = min; r.MaxHeight = max; r.TargetHeight = sum / r.Cells.Count;
            r.MaxRaise = 0f; r.MaxLower = 0f;
            for (int i = 0; i < r.Cells.Count; i++) { float d = r.TargetHeight - r.Cells[i].Ground; if (d > 0f) r.MaxRaise = Mathf.Max(r.MaxRaise, d); else r.MaxLower = Mathf.Max(r.MaxLower, -d); }
        }

        static void PreviewCampus()
        {
            if (result == null) { Report("Calculate the survey before previewing the campus."); return; }
            ClearPreview();
            for (int i = 0; i < corners.Count; i++) Marker("Corner " + (i + 1), corners[i].Position, Color.yellow, new Vector3(1.2f, 3f, 1.2f));
            Marker("Campus center", result.Center, Color.white, new Vector3(1.5f, 5f, 1.5f));
            Vector3 settlement = result.Center - result.AxisLong * 48f;
            Vector3 arena = result.Center + result.AxisLong * 64f;
            Marker("Settlement / maypole center", settlement, Color.green, new Vector3(2f, 5f, 2f));
            Ring("64 m settlement boundary", settlement, 64f, Color.green);
            Line("Separation corridor", result.Center + result.AxisLong * 8f, result.AxisShort, result.Width, new Color(1f, .6f, 0f));
            Marker("Arena / combat ward center", arena, Color.red, new Vector3(2f, 5f, 2f));
            Rectangle("Arena bounds", arena, result.AxisLong, result.AxisShort, 80f, 80f, Color.red);
            Marker("Raid approach north", arena + result.AxisShort * 58f, new Color(1f, .25f, .25f), new Vector3(1f, 4f, 1f));
            Marker("Raid approach south", arena - result.AxisShort * 58f, new Color(1f, .25f, .25f), new Vector3(1f, 4f, 1f));
            Marker("Raid approach east", arena + result.AxisLong * 58f, new Color(1f, .25f, .25f), new Vector3(1f, 4f, 1f));
            Marker("Raid approach west", arena - result.AxisLong * 58f, new Color(1f, .25f, .25f), new Vector3(1f, 4f, 1f));
            Report("Campus preview created with " + preview.Count + " temporary Living World-owned markers. Clear Preview removes only these markers.");
        }

        static GameObject Marker(string label, Vector3 p, Color color, Vector3 scale)
        {
            float h; RaycastHit hit; if (TryGround(p, out h, out hit)) p.y = h;
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = "LW_SITE_PREVIEW::" + label; g.transform.position = p + Vector3.up * scale.y * .5f; g.transform.localScale = scale;
            Collider c = g.GetComponent<Collider>(); if (c != null) UnityEngine.Object.Destroy(c);
            g.AddComponent<LivingWorldSurveyPreviewMarker>();
            Renderer renderer = g.GetComponent<Renderer>(); if (renderer != null) { renderer.material = new Material(Shader.Find("Standard")); renderer.material.color = color; }
            preview.Add(g); return g;
        }

        static void Line(string label, Vector3 center, Vector3 axis, float length, Color color)
        { GameObject g = Marker(label, center, color, new Vector3(1f, 1f, 1f)); g.transform.rotation = Quaternion.LookRotation(axis); g.transform.localScale = new Vector3(.35f, .15f, length); }

        static void Ring(string label, Vector3 center, float radius, Color color)
        { for (int i = 0; i < 24; i++) { float a = i * Mathf.PI * 2f / 24f; Marker(label, center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius), color, new Vector3(.45f, .4f, .45f)); } }

        static void Rectangle(string label, Vector3 center, Vector3 a, Vector3 b, float length, float width, Color color)
        { Line(label, center + b * width * .5f, a, length, color); Line(label, center - b * width * .5f, a, length, color); Line(label, center + a * length * .5f, b, width, color); Line(label, center - a * length * .5f, b, width, color); }

        static void ClearPreview()
        {
            for (int i = 0; i < preview.Count; i++) if (preview[i] != null) UnityEngine.Object.Destroy(preview[i]);
            preview.Clear(); Report("Living World Site Survey preview cleared. Unrelated world objects were untouched.");
        }

        static void Export()
        {
            if (result == null || cells.Count == 0) { Report("Calculate a four-corner survey before export."); return; }
            string root = Path.Combine(Paths.ConfigPath, "LivingWorld", "Tooler", "SiteSurveys"); Directory.CreateDirectory(root);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string stem = "site-survey-" + Safe(worldName) + "-" + stamp;
            string jsonPath = Path.Combine(root, stem + ".json"), textPath = Path.Combine(root, stem + ".txt");
            File.WriteAllText(jsonPath, Json(), Encoding.UTF8); File.WriteAllText(textPath, HumanReport(), Encoding.UTF8);
            Report("Site Survey exported: " + textPath + " and " + jsonPath + ". Summary telemetry only: cells=" + cells.Count + ", fits=" + result.FitsCampus + ".");
        }

        static string HumanReport()
        {
            int wet = 0, blocked = 0; for (int i = 0; i < cells.Count; i++) { if (cells[i].Depth > 0f) wet++; if (!String.IsNullOrEmpty(cells[i].Blocker)) blocked++; }
            var s = new StringBuilder(); s.AppendLine("Living World C.O.R.E. Site Survey");
            s.AppendLine("World: " + worldName + " (UID " + worldUid + ", seed identity " + worldSeed + ") | profile: " + profileName + " | Tooler " + buildVersion);
            s.AppendLine("Started UTC: " + startedUtc + " | exported UTC: " + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            s.AppendLine("Live terrain authority: yes; seed used for identity only.");
            for (int i = 0; i < corners.Count; i++) s.AppendLine("Corner " + (i + 1) + ": " + V(corners[i].Position) + ", water depth " + F(corners[i].Depth) + " m, " + corners[i].Biome + ", heading " + F(corners[i].Heading));
            s.AppendLine("Best-fit center: " + V(result.Center) + " | rotation " + F(result.Rotation) + "°");
            s.AppendLine("Dimensions: " + F(result.Length) + " × " + F(result.Width) + " m | area " + F(result.Area) + " m² | campus: " + (result.FitsCampus ? "FITS" : "DOES NOT FIT"));
            s.AppendLine("Samples: " + cells.Count + " at " + F(grid) + " m | wet " + wet + " | blocked " + blocked + " | elevation " + F(result.MinHeight) + "–" + F(result.MaxHeight));
            s.AppendLine("Preview cut/fill grade: target " + F(result.TargetHeight) + " m | max raise " + F(result.MaxRaise) + " m | max lower " + F(result.MaxLower) + " m");
            s.AppendLine("Terrain application: NOT APPLIED / LOCKED."); return s.ToString();
        }

        static string Json()
        {
            var s = new StringBuilder(); s.Append("{\n  \"schema\":\"living-world-site-survey-v1\",");
            s.Append("\n  \"identity\":{\"worldUid\":").Append(worldUid).Append(",\"worldSeed\":").Append(worldSeed).Append(",\"worldName\":\"").Append(J(worldName)).Append("\",\"profile\":\"").Append(J(profileName)).Append("\",\"toolerVersion\":\"").Append(J(buildVersion)).Append("\"},");
            s.Append("\n  \"liveAlteredTerrainAuthoritative\":true,\n  \"seedTerrainInferred\":false,");
            s.Append("\n  \"center\":").Append(JV(result.Center)).Append(",\"rotationDegrees\":").Append(F(result.Rotation)).Append(",\"length\":").Append(F(result.Length)).Append(",\"width\":").Append(F(result.Width)).Append(",\"area\":").Append(F(result.Area)).Append(",\"campusFits\":").Append(result.FitsCampus ? "true" : "false").Append(",");
            s.Append("\n  \"gridMeters\":").Append(F(grid)).Append(",\"targetGrade\":").Append(F(result.TargetHeight)).Append(",\"maxRaise\":").Append(F(result.MaxRaise)).Append(",\"maxLower\":").Append(F(result.MaxLower)).Append(",");
            s.Append("\n  \"corners\":["); for (int i = 0; i < corners.Count; i++) { if (i > 0) s.Append(','); SurveyCorner c = corners[i]; s.Append("{\"index\":").Append(c.Index).Append(",\"position\":").Append(JV(c.Position)).Append(",\"ground\":").Append(F(c.Ground)).Append(",\"water\":").Append(F(c.Water)).Append(",\"depth\":").Append(F(c.Depth)).Append(",\"biome\":\"").Append(J(c.Biome)).Append("\",\"heading\":").Append(F(c.Heading)).Append(",\"utc\":\"").Append(J(c.Utc)).Append("\"}"); } s.Append("],");
            s.Append("\n  \"cells\":["); for (int i = 0; i < cells.Count; i++) { if (i > 0) s.Append(','); SurveyCell c = cells[i]; s.Append("{\"x\":").Append(F(c.X)).Append(",\"z\":").Append(F(c.Z)).Append(",\"ground\":").Append(F(c.Ground)).Append(",\"water\":").Append(F(c.Water)).Append(",\"depth\":").Append(F(c.Depth)).Append(",\"biome\":\"").Append(J(c.Biome)).Append("\",\"blocker\":\"").Append(J(c.Blocker)).Append("\"}"); } s.Append("]\n}\n"); return s.ToString();
        }

        static bool TryGround(Vector3 at, out float ground, out RaycastHit hit)
        {
            ground = 0f; hit = new RaycastHit(); if (ZoneSystem.instance == null) return false;
            Vector3 probe = new Vector3(at.x, at.y + 200f, at.z); int terrain = LayerMask.GetMask("terrain");
            bool ray = Physics.Raycast(probe, Vector3.down, out hit, 500f, terrain, QueryTriggerInteraction.Ignore);
            float valheim; bool native = ZoneSystem.instance.GetGroundHeight(at, out valheim);
            if (!native && !ray) return false; ground = native ? valheim : hit.point.y; return !Single.IsNaN(ground) && !Single.IsInfinity(ground);
        }

        static bool WorldReady()
        { if (ZNet.instance == null || ZoneSystem.instance == null || Player.m_localPlayer == null) { Report("Site Survey requires a loaded world and local player."); return false; } return true; }
        static bool SameWorld() { return WorldReady() && !String.IsNullOrEmpty(startedUtc) && ZNet.instance.GetWorldUID() == worldUid; }
        static string TerrainApplicationBlockedReason()
        {
            if (ZNet.instance == null || Player.m_localPlayer == null) return "no loaded local world";
            if (!ZNet.instance.IsServer()) return "connected client";
            if (ZNet.IsOpenServer()) return "open server";
            if (ZNet.instance.GetPeers().Count != 0) return "peers connected";
            foreach (Character candidate in Character.GetAllCharacters())
                if (candidate != null && candidate != Player.m_localPlayer && !candidate.IsDead() &&
                    Vector3.Distance(candidate.transform.position, Player.m_localPlayer.transform.position) < 40f &&
                    BaseAI.IsEnemy(Player.m_localPlayer, candidate)) return "hostile combatant within 40 m";
            return "LOCKED: no verified world-backup receipt and application code is intentionally absent";
        }
        static bool FineGridSafe(SurveyResult r) { return r.Area <= 65536f && (Mathf.FloorToInt(r.Length / 2f) + 1) * (Mathf.FloorToInt(r.Width / 2f) + 1) <= MaxSamples; }
        static void SetGrid(float value) { grid = value; if (result != null && grid == 2f && !FineGridSafe(result)) grid = 4f; Report("Site Survey grid set to " + F(grid) + " m."); }
        static string StatusText() { if (String.IsNullOrEmpty(startedUtc)) return "No survey active."; if (result == null) return "World " + worldName + " | corners " + corners.Count + "/4 | grid " + F(grid) + " m"; return "Center " + V(result.Center) + " | " + F(result.Length) + " × " + F(result.Width) + " m | " + cells.Count + " samples | campus " + (result.FitsCampus ? "FITS" : "DOES NOT FIT"); }
        static void Report(string text) { LivingWorldPanel.SetStatus(text); }
        static string F(float value) { return value.ToString("0.###", CultureInfo.InvariantCulture); }
        static string V(Vector3 v) { return F(v.x) + ", " + F(v.y) + ", " + F(v.z); }
        static string JV(Vector3 v) { return "{\"x\":" + F(v.x) + ",\"y\":" + F(v.y) + ",\"z\":" + F(v.z) + "}"; }
        static string J(string v) { return (v ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n"); }
        static string Safe(string v) { var s = new StringBuilder(); foreach (char c in v ?? "") if (Char.IsLetterOrDigit(c) || c == '-' || c == '_') s.Append(c); return s.Length == 0 ? "unknown" : s.ToString(); }

        sealed class SurveyCorner { public int Index; public Vector3 Position; public float Ground, Water, Depth, Heading; public string Biome, Utc; }
        sealed class SurveyCell { public float X, Z, Ground, Water, Depth; public string Biome, Blocker; }
        sealed class SurveyResult
        {
            public Vector3 Center, AxisLong, AxisShort; public float Rotation, Length, Width, Area, MinHeight, MaxHeight, TargetHeight, MaxRaise, MaxLower; public bool FitsCampus; public List<SurveyCell> Cells;
        }

        static class SurveyGeometry
        {
            public static SurveyResult Calculate(List<SurveyCorner> input, out string error)
            {
                error = ""; Vector2 centroid = Vector2.zero; for (int i = 0; i < 4; i++) centroid += new Vector2(input[i].Position.x, input[i].Position.z); centroid *= .25f;
                var ordered = new List<SurveyCorner>(input); ordered.Sort(delegate(SurveyCorner a, SurveyCorner b) { return Mathf.Atan2(a.Position.z - centroid.y, a.Position.x - centroid.x).CompareTo(Mathf.Atan2(b.Position.z - centroid.y, b.Position.x - centroid.x)); });
                float signed = 0f; for (int i = 0; i < 4; i++) { Vector3 a = ordered[i].Position, b = ordered[(i + 1) % 4].Position; signed += a.x * b.z - b.x * a.z; }
                if (Mathf.Abs(signed) < 4f) { error = "corners are degenerate or area is too small"; return null; }
                float sign = Mathf.Sign(signed); for (int i = 0; i < 4; i++) { Vector3 a = ordered[i].Position, b = ordered[(i + 1) % 4].Position, c = ordered[(i + 2) % 4].Position; float cross = (b.x-a.x)*(c.z-b.z)-(b.z-a.z)*(c.x-b.x); if (Mathf.Abs(cross) < .01f || Mathf.Sign(cross) != sign) { error = "corners form a concave or self-intersecting quadrilateral"; return null; } }
                float xx=0f, zz=0f, xz=0f; for(int i=0;i<4;i++){float x=input[i].Position.x-centroid.x,z=input[i].Position.z-centroid.y;xx+=x*x;zz+=z*z;xz+=x*z;}
                float theta=.5f*Mathf.Atan2(2f*xz,xx-zz); Vector3 axis=new Vector3(Mathf.Cos(theta),0f,Mathf.Sin(theta)).normalized, side=new Vector3(-axis.z,0f,axis.x);
                float minA=Single.MaxValue,maxA=Single.MinValue,minB=Single.MaxValue,maxB=Single.MinValue,meanY=0f;
                for(int i=0;i<4;i++){Vector3 p=input[i].Position;float da=p.x*axis.x+p.z*axis.z,db=p.x*side.x+p.z*side.z;minA=Mathf.Min(minA,da);maxA=Mathf.Max(maxA,da);minB=Mathf.Min(minB,db);maxB=Mathf.Max(maxB,db);meanY+=p.y*.25f;}
                float la=maxA-minA,lb=maxB-minB;if(lb>la){float t=la;la=lb;lb=t;Vector3 tv=axis;axis=side;side=tv;float tm=minA;minA=minB;minB=tm;tm=maxA;maxA=maxB;maxB=tm;}
                Vector3 center=axis*((minA+maxA)*.5f)+side*((minB+maxB)*.5f);center.y=meanY;
                return new SurveyResult{Center=center,AxisLong=axis,AxisShort=side,Rotation=Mathf.Atan2(axis.x,axis.z)*Mathf.Rad2Deg,Length=la,Width=lb,Area=Mathf.Abs(signed)*.5f,Cells=new List<SurveyCell>()};
            }
        }
    }

    public sealed class LivingWorldSurveyPreviewMarker : MonoBehaviour { }
}

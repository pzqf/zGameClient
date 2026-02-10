using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameClient.Editor
{
    public class MapExporter : EditorWindow
    {
        private static string mapName = "Demo";
        private static int mapId = 1;
        private static int mapWidth = 1000;
        private static int mapHeight = 1000;

        [MenuItem("Tools/Map Exporter")]
        public static void ShowWindow()
        {
            GetWindow<MapExporter>("Map Exporter");
        }

        private void OnGUI()
        {
            GUILayout.Label("Map Export Settings", EditorStyles.boldLabel);

            mapName = EditorGUILayout.TextField("Map Name", mapName);
            mapId = EditorGUILayout.IntField("Map ID", mapId);
            mapWidth = EditorGUILayout.IntField("Map Width", mapWidth);
            mapHeight = EditorGUILayout.IntField("Map Height", mapHeight);

            if (GUILayout.Button("Export Map Config"))
            {
                ExportMapConfig();
            }

            if (GUILayout.Button("Export Spawn Points"))
            {
                ExportSpawnPoints();
            }

            if (GUILayout.Button("Select Objects to Export"))
            {
                SelectExportableObjects();
            }
        }

        private static void ExportMapConfig()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"map_id\": {mapId},");
            sb.AppendLine($"  \"name\": \"{mapName}\",");
            sb.AppendLine($"  \"map_type\": 1,");
            sb.AppendLine($"  \"width\": {mapWidth},");
            sb.AppendLine($"  \"height\": {mapHeight},");
            sb.AppendLine($"  \"region_size\": 32,");
            sb.AppendLine($"  \"tile_width\": 1,");
            sb.AppendLine($"  \"tile_height\": 1,");
            sb.AppendLine($"  \"is_instance\": false,");
            sb.AppendLine($"  \"max_players\": 100,");
            sb.AppendLine($"  \"description\": \"{mapName} map\",");
            sb.AppendLine($"  \"min_level\": 1,");
            sb.AppendLine($"  \"max_level\": 100,");
            sb.AppendLine($"  \"respawn_rate\": 1.0");
            sb.AppendLine("}");

            string path = EditorUtility.SaveFilePanel("Save Map Config", "", $"{mapName.ToLower()}_map.json", "json");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, sb.ToString());
                Debug.Log($"Map config exported to: {path}");
            }
        }

        private static void ExportSpawnPoints()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[");

            bool first = true;
            foreach (var obj in Selection.gameObjects)
            {
                if (!first)
                {
                    sb.AppendLine(",");
                }
                first = false;

                var pos = obj.transform.position;
                var name = obj.name.ToLower();
                int spawnType = name.Contains("monster") ? 1 : 2;
                int objectId = ExtractIdFromName(obj.name);

                sb.AppendLine("  {");
                sb.AppendLine($"    \"spawn_id\": {objectId},");
                sb.AppendLine($"    \"map_id\": {mapId},");
                sb.AppendLine($"    \"monster_id\": {objectId},");
                sb.AppendLine($"    \"spawn_type\": {spawnType},");
                sb.AppendLine($"    \"pos_x\": {pos.x:F2},");
                sb.AppendLine($"    \"pos_y\": {pos.y:F2},");
                sb.AppendLine($"    \"pos_z\": {pos.z:F2},");
                sb.AppendLine($"    \"max_count\": 1,");
                sb.AppendLine($"    \"spawn_interval\": 60,");
                sb.AppendLine($"    \"radius\": 5.0,");
                sb.AppendLine($"    \"patrol_range\": 10.0");
                sb.Append("  }");
            }

            sb.AppendLine();
            sb.AppendLine("]");

            string path = EditorUtility.SaveFilePanel("Save Spawn Points", "", $"{mapName.ToLower()}_spawns.json", "json");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, sb.ToString());
                Debug.Log($"Spawn points exported to: {path}");
            }
        }

        private static void SelectExportableObjects()
        {
            var exportableTags = new[] { "Monster", "NPC", "Enemy" };
            var objects = GameObject.FindObjectsOfType<GameObject>();
            
            var exportable = new System.Collections.Generic.List<GameObject>();
            foreach (var obj in objects)
            {
                foreach (var tag in exportableTags)
                {
                    if (obj.CompareTag(tag) || obj.name.ToLower().Contains(tag.ToLower()))
                    {
                        exportable.Add(obj);
                        break;
                    }
                }
            }

            Selection.objects = exportable.ToArray();
            Debug.Log($"Selected {exportable.Count} exportable objects");
        }

        private static int ExtractIdFromName(string name)
        {
            var parts = name.Split('_');
            if (parts.Length > 0 && int.TryParse(parts[parts.Length - 1], out int id))
            {
                return id;
            }
            return 1000 + name.GetHashCode() % 1000;
        }
    }
}

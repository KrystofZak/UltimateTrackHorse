using System.IO;
using UnityEngine;

namespace GameLogic.Ghost
{
    public class GhostPersistence
    {
        private static string GetPath(string mapId)
        {
            string safeMapId = mapId.Replace("/", "_").Replace("\\", "_");
            return Path.Combine(Application.persistentDataPath, $"ghost_{safeMapId}.json");
        }

        public static void SaveBestLap(GhostLapData data)
        {
            if (data == null)
                return;

            string json = JsonUtility.ToJson(data);
            File.WriteAllText(GetPath(data.mapId), json);
        }

        public static GhostLapData LoadBestLap(string mapId)
        {
            string path = GetPath(mapId);

            if (!File.Exists(path))
                return null;

            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<GhostLapData>(json);
        }

        public static bool HasBestLap(string mapId)
        {
            return File.Exists(GetPath(mapId));
        }
    }
}
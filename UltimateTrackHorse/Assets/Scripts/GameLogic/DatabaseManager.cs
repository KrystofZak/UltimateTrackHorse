using GameLogic.Network;
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class DatabaseManager : MonoBehaviour
{
    [Header("Database Settings")]
    [SerializeField] private string databaseURL = ConfigFile.FirebaseUrl;

    #region Public Methods

    public void SendGameResult(string seed, int laps, float bestLapTime)
    {
        if (!DiscordManager.IsLinked)
        {
            Debug.Log("[DatabaseManager] Hráè není propojen. Výsledek se neodesílá.");
            return;
        }

        if (laps == 0 || bestLapTime <= 0f) return;

        StartCoroutine(ProcessAndUploadResult(seed, laps, bestLapTime, DiscordManager.DiscordID, DiscordManager.AuthToken));
    }

    #endregion

    #region Internal Logic

    private IEnumerator ProcessAndUploadResult(string seed, int newLaps, float newBestLap, string discordId, string token)
    {
        string url = $"{databaseURL}/leaderboards/{seed}/{discordId}.json";

        using (UnityWebRequest getRequest = UnityWebRequest.Get(url))
        {
            yield return getRequest.SendWebRequest();

            if (getRequest.result == UnityWebRequest.Result.Success)
            {
                string json = getRequest.downloadHandler.text;
                if (json != "null")
                {
                    GameResult oldData = JsonUtility.FromJson<GameResult>(json);

                    bool isOldBetter = (oldData.laps > newLaps) || (oldData.laps == newLaps && oldData.bestLap <= newBestLap);

                    if (isOldBetter)
                    {
                        Debug.Log($"[DatabaseManager] Dosavadní rekord ({oldData.laps} kol, èas {oldData.bestLap}) je lepší. Neodesílám.");
                        yield break;
                    }
                }
            }
        }

        string safeTime = newBestLap.ToString(CultureInfo.InvariantCulture);
        string jsonBody = $"{{\"discordId\":\"{discordId}\", \"laps\":{newLaps}, \"bestLap\":{safeTime}, \"token\":\"{token}\"}}";

        using (UnityWebRequest putRequest = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            putRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            putRequest.downloadHandler = new DownloadHandlerBuffer();
            putRequest.SetRequestHeader("Content-Type", "application/json");

            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[DatabaseManager] Chyba pøi odesílání: {putRequest.error}");
            }
            else
            {
                Debug.Log($"[DatabaseManager] Nový osobní rekord uložen! Seed: {seed}, Kola: {newLaps}, Èas: {newBestLap}");
            }
        }
    }

    #endregion

    [System.Serializable]
    private class GameResult
    {
        public int laps;
        public float bestLap;
    }
}
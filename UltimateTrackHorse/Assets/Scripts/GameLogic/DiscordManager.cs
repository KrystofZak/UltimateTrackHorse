using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GameLogic.Network
{
    public class DiscordManager : MonoBehaviour
    {
        [Header("Database Settings")]
        [SerializeField] private string databaseURL = ConfigFile.FirebaseUrl;

        public event Action<string> OnCodeGenerated;
        public event Action OnAuthorizationSuccess;
        public event Action OnAuthorizationTimeout;

        public static string DiscordID { get; private set; }
        public static string AuthToken { get; private set; }
        public static bool IsLinked => !string.IsNullOrEmpty(DiscordID) && !string.IsNullOrEmpty(AuthToken);

        public string CurrentLinkPIN { get; private set; }
        private bool isPolling = false;

        #region Unity Lifecycle

        private void Awake()
        {
            
            LoadAuthData();
            Debug.Log($"DiscordManager Awake - Loaded DiscordID: {DiscordID}, AuthToken: {(string.IsNullOrEmpty(AuthToken) ? "null" : "exists")}");
            /*Authorize();
            Debug.Log($"DiscordManager Awake - IsLinked: {IsLinked}");
            */
        }

        #endregion

        #region Public Methods

        public void Authorize()
        {
            Debug.Log("Starting Discord authorization process...");
            if (IsLinked)
            {
                OnAuthorizationSuccess?.Invoke();
                return;
            }

            CurrentLinkPIN = GenerateRandomPIN(4);

            Debug.Log($"VYGENEROVÁN PIN: {CurrentLinkPIN} - podívej se do Firebase!");

            OnCodeGenerated?.Invoke(CurrentLinkPIN);

            StartCoroutine(UploadPINToFirebase(CurrentLinkPIN));

            if (!isPolling)
            {
                StartCoroutine(PollFirebaseForToken());
            }
        }

        public void CancelAuthorization()
        {
            if (isPolling)
            {
                isPolling = false;
                StopAllCoroutines();
                StartCoroutine(DeletePendingLink());
                CurrentLinkPIN = null;
            }
        }

        public void Disconnect()
        {
            DiscordID = null;
            AuthToken = null;
            PlayerPrefs.DeleteKey("DiscordID");
            PlayerPrefs.DeleteKey("AuthToken");
            PlayerPrefs.Save();
        }

        #endregion

        #region Internal Logic

        private IEnumerator UploadPINToFirebase(string pin)
        {
            string url = $"{databaseURL}/pending_links/{pin}.json";

            string jsonBody = "{\"status\":\"waiting_for_bot\"}";

            using (UnityWebRequest putRequest = new UnityWebRequest(url, "PUT"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                putRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                putRequest.downloadHandler = new DownloadHandlerBuffer();
                putRequest.SetRequestHeader("Content-Type", "application/json");

                yield return putRequest.SendWebRequest();

                if (putRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Firebase ERROR] Nepodaøilo se nahrát PIN {pin}: {putRequest.error}");
                }
                else
                {
                    Debug.Log($"[Firebase] Uzel pro PIN {pin} byl úspìšnì vytvoøen. Èekám na Discord bota...");
                }
            }
        }

        private void LoadAuthData()
        {
            if (PlayerPrefs.HasKey("DiscordID") && PlayerPrefs.HasKey("AuthToken"))
            {
                DiscordID = PlayerPrefs.GetString("DiscordID");
                AuthToken = PlayerPrefs.GetString("AuthToken");
            }
        }

        private string GenerateRandomPIN(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string result = "";
            for (int i = 0; i < length; i++)
            {
                result += chars[UnityEngine.Random.Range(0, chars.Length)];
            }
            return result;
        }

        private IEnumerator PollFirebaseForToken()
        {
            isPolling = true;
            string url = $"{databaseURL}/pending_links/{CurrentLinkPIN}.json";
            int attempts = 0;

            while (attempts < 100 && !IsLinked && isPolling)
            {
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    yield return request.SendWebRequest();

                    // Pokud se spojení podaøilo
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string json = request.downloadHandler.text;
                        Debug.Log($"[Firebase] Odpovìï pro PIN {CurrentLinkPIN}: {json}");

                        if (json != "null" && json.Contains("discordId") && json.Contains("token"))
                        {
                            AuthData data = JsonUtility.FromJson<AuthData>(json);
                            if (data != null && !string.IsNullOrEmpty(data.discordId) && !string.IsNullOrEmpty(data.token))
                            {
                                SaveAuthData(data.discordId, data.token);
                                StartCoroutine(DeletePendingLink());
                                isPolling = false;
                                OnAuthorizationSuccess?.Invoke();
                                yield break;
                            }
                        }
                    }
                    else // PØIDÁNO: Pokud nastala CHYBA (napø. 401 Unauthorized nebo špatná URL)
                    {
                        Debug.LogError($"[Firebase ERROR] PIN {CurrentLinkPIN}: {request.error} | URL: {url}");
                    }
                }

                attempts++;
                yield return new WaitForSeconds(3f);
            }
            isPolling = false;
        }

        private void SaveAuthData(string id, string token)
        {
            DiscordID = id;
            AuthToken = token;

            PlayerPrefs.SetString("DiscordID", DiscordID);
            PlayerPrefs.SetString("AuthToken", AuthToken);
            PlayerPrefs.Save();
        }

        private IEnumerator DeletePendingLink()
        {
            if (string.IsNullOrEmpty(CurrentLinkPIN)) yield break;

            string url = $"{databaseURL}/pending_links/{CurrentLinkPIN}.json";
            using (UnityWebRequest request = UnityWebRequest.Delete(url))
            {
                yield return request.SendWebRequest();
            }
        }

        #endregion

        [Serializable]
        private class AuthData
        {
            public string discordId;
            public string token;
        }
    }
}
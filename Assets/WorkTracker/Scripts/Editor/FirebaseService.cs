using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Networking;

namespace WorkTracker
{
    public class FirebaseService
    {
        private static string ApiKey => WorkTrackerSettings.Instance.FirebaseApiKey;
        private static string ProjectId => WorkTrackerSettings.Instance.FirebaseProjectId;

        private const string AuthUrl = "https://identitytoolkit.googleapis.com/v1/accounts";
        private const string FirestoreUrl = "https://firestore.googleapis.com/v1/projects";

        private static string _idToken;
        private static string _localId;
        private static string _refreshToken;

        public static bool IsLoggedIn => !string.IsNullOrEmpty(_idToken);
        public static string CurrentUserId => _localId;

        // Auth Response Wrapper
        [Serializable]
        private class AuthResponse
        {
            public string idToken;
            public string email;
            public string refreshToken;
            public string expiresIn;
            public string localId;
            public string error; // Custom field for easy error checking
        }
        
        // Firestore Document Wrapper
        [Serializable]
        private class FirestoreDocument
        {
            public string name;
            public FirestoreFields fields;
        }

        [Serializable]
        private class FirestoreFields
        {
            // We'll use a dynamic dictionary approach or specific wrappers for user data
            // For simplicity in Unity JsonUtility, we might need specific classes or use a JSON parser that supports dicts.
            // But let's try to keep it simple: We store the HUGE JSON used by UserData as a string blob in Firestore?
            // Or we map fields. Mapping fields cleanly with JsonUtility is pain.
            // Strategy: Store the UserData JSON string inside a single field called "jsonBlob" to avoid mapping hell,
            // OR use a proper serializer if we want the web to query fields easily.
            // The user wants the Web Page to display stats. So the web needs to query fields?
            // Actually, if we just upload the JSON blob, the web can parse it.
            // But it's better to verify user existence via email.
            
            // Let's go with:
            // users/{uid} has fields:
            // - email (string)
            // - name (string)
            // - role (string)
            // - lastUpdated (timestamp)
            // - data (string - the encrypted or plain json of the full history)
        }

        [Serializable]
        private class AuthRequest
        {
            public string email;
            public string password;
            public bool returnSecureToken;
        }

        public static IEnumerator SignInEnumerator(string email, string password, Action<bool, string> callback)
        {
            var task = SignIn(email, password, callback);
            while (!task.IsCompleted)
                yield return null;

            // Optional: check for task.Exception
        }

        public static async Task<string> SignIn(string email, string password, Action<bool, string> callback)
        {
            AuthRequest authRequest = new()
            {
                email = email,
                password = password,
                returnSecureToken = true
            };
            string url = $"{AuthUrl}:signInWithPassword?key={ApiKey}";
            UnityWebRequest request = createAPIpostRequestObject(authRequest, url);
            request = await SendWebRequestAsync(request);

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    object response = request.downloadHandler.text;
                    if (response is string jsonResponse)
                    {
                        AuthResponse res = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                        _idToken = res.idToken;
                        _localId = res.localId;
                        _refreshToken = res.refreshToken;
                        callback?.Invoke(true, "Success");
                        //return response;
                    }

                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error parsing JSON: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    Console.WriteLine($"InnerException: {ex.InnerException}");
                    throw;
                }
            }
            throw new Exception(request.responseCode.ToString());
        }

        public static async Task<UnityWebRequest> SendWebRequestAsync(UnityWebRequest request)
        {
            //AddCsrfTokenHeader(request);
            UnityWebRequestAsyncOperation asyncOp = request.SendWebRequest();
            while (!asyncOp.isDone)
            {
                await Task.Yield();
            }
            return request;
        }
        private static UnityWebRequest createAPIpostRequestObject(object payload, string url)
        {
            var data = JsonConvert.SerializeObject(payload);
            UnityWebRequest request = new(url, "POST");
            byte[] payloadBytes = Encoding.UTF8.GetBytes(data);
            request.uploadHandler = new UploadHandlerRaw(payloadBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }
        //public static IEnumerator SignIn(string email, string password, Action<bool, string> callback)
        //{
        //    if (string.IsNullOrEmpty(ApiKey))
        //    {
        //        callback?.Invoke(false, "API Key is missing in settings.");
        //        yield break;
        //    }

        //    string url = $"{AuthUrl}:signInWithPassword?key={ApiKey}";
        //    string json = JsonUtility.ToJson(new AuthRequest { email = email, password = password, returnSecureToken = true });

        //    Debug.Log($"[WorkTracker] Signing In... Payload: {json}");

        //    // Using verbose Manual Request to ensure valid JSON Content-Type (Fix for UnityWebRequest.Post quirks)
        //// ... existing code ...

        //    using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        //    {
        //        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        //        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        //        www.downloadHandler = new DownloadHandlerBuffer();
        //        www.SetRequestHeader("Content-Type", "application/json");

        //        // DATA LOSS WARNING: Bypassing SSL for debugging Code 0 issues. 
        //        // In production, this should be removed or handled with a proper trusted root.
        //        www.certificateHandler = new BypassCertificateHandler();

        //        yield return www.SendWebRequest();

        //        if (www.result == UnityWebRequest.Result.Success)
        //        {
        //            Debug.Log($"[WorkTracker] Auth Success: {www.downloadHandler.text}");
        //            AuthResponse res = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
        //            _idToken = res.idToken;
        //            _localId = res.localId;
        //            _refreshToken = res.refreshToken;
        //            callback?.Invoke(true, "Success");

        //        }
        //        else
        //        {
        //            Debug.LogError($"[WorkTracker] Auth Failed: {www.error} Code: {www.responseCode} Response: {www.downloadHandler.text}");
        //            callback?.Invoke(false, www.error + ": " + www.downloadHandler.text);
        //        }
        //    }
        //}

        public static IEnumerator SignUp(string email, string password, Action<bool, string> callback)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                callback?.Invoke(false, "API Key is missing in settings.");
                yield break;
            }

            string url = $"{AuthUrl}:signUp?key={ApiKey}";
            string json = JsonUtility.ToJson(new AuthRequest { email = email, password = password, returnSecureToken = true });

            Debug.Log($"[WorkTracker] Signing Up... Payload: {json}");

            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                // SSL Bypass
                www.certificateHandler = new BypassCertificateHandler();

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[WorkTracker] Auth Failed: {www.error} Code: {www.responseCode} Response: {www.downloadHandler.text}");
                    callback?.Invoke(false, www.error + ": " + www.downloadHandler.text);
                }
                else
                {
                    Debug.Log($"[WorkTracker] Auth Success: {www.downloadHandler.text}");
                    AuthResponse res = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                    _idToken = res.idToken;
                    _localId = res.localId;
                    _refreshToken = res.refreshToken;
                    callback?.Invoke(true, "Account created.");
                }
            }
        }

        // Add this helper to check general connectivity
        public static IEnumerator CheckInternet(Action<bool> callback)
        {
             using (UnityWebRequest www = UnityWebRequest.Get("https://www.google.com"))
             {
                 yield return www.SendWebRequest();
                 if (www.result == UnityWebRequest.Result.Success)
                 {
                     Debug.Log("[WorkTracker] Internet Reachable.");
                     callback?.Invoke(true);
                 }
                 else
                 {
                     Debug.LogError($"[WorkTracker] Internet Unreachable: {www.error} Code: {www.responseCode}");
                     callback?.Invoke(false);
                 }
             }
        }

        private class BypassCertificateHandler : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                // Always accept
                return true;
            }
        }


        public static IEnumerator SaveUserData(UserData data, Action<bool> callback = null)
        {
            if (!IsLoggedIn)
            {
                callback?.Invoke(false);
                yield break;
            }

            // Structure to send to Firestore
            // Path: projects/{projectId}/databases/(default)/documents/users/{uid}
            string url = $"{FirestoreUrl}/{ProjectId}/databases/(default)/documents/users/{_localId}?key={ApiKey}"; // Using Auth token usually, but REST needs Bearer
            
            // We need to PATCH to update
            // But standard UnityWebRequest.Put is PUT.
            // We can use a helper for Patch.

            // Payload:
            // We will store the ENTIRE UserData as a JSON string for simplicity of sync,
            // PLUS extracted fields for querying.
            
            string fullJson = JsonUtility.ToJson(data);

            // Firestore JSON format is verbose: { "fields": { "key": { "stringValue": "val" } } }
            // We'll construct it manually for these few fields.
            string firestoreJson = "{ \"fields\": { " +
                                   $"\"name\": {{ \"stringValue\": \"{data.UserName}\" }}, " +
                                   $"\"email\": {{ \"stringValue\": \"{data.Email}\" }}, " +
                                   $"\"role\": {{ \"stringValue\": \"{data.Role}\" }}, " +
                                   $"\"dataBlob\": {{ \"stringValue\": \"{EscapeJson(fullJson)}\" }} " +
                                   "} }";

            using (UnityWebRequest www = UnityWebRequest.Put(url, firestoreJson))
            {
                www.method = "PATCH"; // Use PATCH to merge/update
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", "Bearer " + _idToken);

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[WorkTracker] Cloud Save Failed: {www.error} {www.downloadHandler.text}");
                    callback?.Invoke(false);
                }
                else
                {
                    if (WorkTrackerSettings.Instance.ShowDebugLogs) Debug.Log("[WorkTracker] Cloud Save Success");
                    callback?.Invoke(true);
                }
            }
        }

        public static IEnumerator LoadUserData(Action<UserData> onSuccess, Action onFailure)
        {
            if (!IsLoggedIn)
            {
                onFailure?.Invoke();
                yield break;
            }

            string url = $"{FirestoreUrl}/{ProjectId}/databases/(default)/documents/users/{_localId}";

            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("Authorization", "Bearer " + _idToken);
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[WorkTracker] Cloud Load Failed: {www.error}");
                    onFailure?.Invoke();
                }
                else
                {
                    // Parse Firestore response
                    // It returns { "name": "...", "fields": { ... } }
                    // We need to extract fields -> dataBlob -> stringValue
                    try
                    {
                        string json = www.downloadHandler.text;
                        // Quick hack parsing: Find "dataBlob" then "stringValue"
                        // Robust way would be full classes, but let's try a simple extract for now to save 500 lines of DTOs.
                        string innerJson = ExtractStringValue(json, "dataBlob");
                        if (!string.IsNullOrEmpty(innerJson))
                        {
                            // innerJson is the ESCAPED json string we sent. We need to unescape it?
                            // Actually ExtractStringValue should handle it unless we double encoded.
                            UserData user = JsonUtility.FromJson<UserData>(innerJson);
                            onSuccess?.Invoke(user);
                        }
                        else
                        {
                            // Maybe doc exists but no blob?
                             onFailure?.Invoke();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[WorkTracker] Parse Error: {e.Message}");
                        onFailure?.Invoke();
                    }
                }
            }
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\""); 
        }

        private static string ExtractStringValue(string firestoreJson, string fieldName)
        {
            // Look for "fieldName": { "stringValue": "VALUE" }
            string key = $"\"{fieldName}\"";
            int keyIdx = firestoreJson.IndexOf(key);
            if (keyIdx == -1) return null;

            int stringValueIdx = firestoreJson.IndexOf("\"stringValue\"", keyIdx);
            if (stringValueIdx == -1) return null;

            int colonIdx = firestoreJson.IndexOf(":", stringValueIdx);
            int startQuote = firestoreJson.IndexOf("\"", colonIdx + 1);
            
            // Now we need to read until the matching end quote, handling escapes.
            // This is fragile but acceptable for this specific strict format if we are careful.
            // Actually, JsonUtility.ToJson might have escaped quotes as \".
            // So we just read until we see an unescaped quote? 
            
            // Better approach: Use a mini parser.
            StringBuilder sb = new StringBuilder();
            bool escaped = false;
            for (int i = startQuote + 1; i < firestoreJson.Length; i++)
            {
                char c = firestoreJson[i];
                if (escaped)
                {
                    sb.Append(c);
                    escaped = false;
                }
                else
                {
                    if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == '"')
                    {
                        return sb.ToString();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            return null;
        }
    }
}

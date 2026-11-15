using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TensorHawks.Data;

namespace TensorHawks.Core
{
    public class WebApiClient : MonoBehaviour
    {
        [Header("API Settings")]
        [SerializeField] private string apiUrl = "https://tensor-hawks-api.onrender.com/run-simulation";

        public void SendSimulationData(SimulationRequest requestData, Action<SimulationResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(PostSimulationCoroutine(requestData, onSuccess, onError));
        }

        private IEnumerator PostSimulationCoroutine(SimulationRequest requestData, Action<SimulationResponse> onSuccess, Action<string> onError)
        {
            string jsonPayload = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            using (UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST"))
            {
                webRequest.timeout = 60; // Wait up to 60 seconds for FastAPI

                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    onError?.Invoke($"API Error: {webRequest.error}");
                }
                else
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    SimulationResponse response = JsonUtility.FromJson<SimulationResponse>(jsonResponse);
                    onSuccess?.Invoke(response);
                }
            }
        }
    }
}
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class InventoryServer : MonoBehaviour
{
    private const string Url = "https://wadahub.manerai.com/api/inventory/status";
    private const string AuthToken = "Bearer kPERnYcWAY46xaSy8CEzanosAgsWM84Nx7SKM4QBSqPq6c7StWfGxzhxPfDh8MaP";

    public IEnumerator SendItemEvent(int itemId, string action)
    {
        string jsonData = JsonUtility.ToJson(new ItemEvent(itemId, action));

        using (UnityWebRequest request = new UnityWebRequest(Url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", AuthToken);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Ошибка отправки запроса: {request.error}");
            }
            else
            {
                Debug.Log($"[InventoryServer] Успешный ответ: {request.downloadHandler.text}");
            }
        }
    }

    [System.Serializable]
    private class ItemEvent
    {
        public int itemId;
        public string action;

        public ItemEvent(int itemId, string action)
        {
            this.itemId = itemId;
            this.action = action;
        }
    }
}

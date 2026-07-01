using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class FirestoreManager : MonoBehaviour
{
    private const string ProjectId = "magician-league"; // เปลี่ยนตรงนี้

    private string BaseUrl =>
        $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents";

    public static FirestoreManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ==================
    // WRITE
    // ==================

    public void SetDocument(
        string collection,
        string docId,
        string jsonBody,
        Action<bool> onComplete = null
    )
    {
        StartCoroutine(SetDocumentCoroutine(collection, docId, jsonBody, onComplete));
    }

    private IEnumerator SetDocumentCoroutine(
        string collection,
        string docId,
        string jsonBody,
        Action<bool> onComplete
    )
    {
        string url = $"{BaseUrl}/{collection}/{docId}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using UnityWebRequest req = new UnityWebRequest(url, "PATCH");

        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success;

        if (!ok)
            Debug.LogError($"Firestore Write Error: {req.error}");

        onComplete?.Invoke(ok);
    }

    // ==================
    // READ
    // ==================

    public void GetDocument(string collection, string docId, Action<string> onComplete)
    {
        StartCoroutine(GetDocumentCoroutine(collection, docId, onComplete));
    }

    private IEnumerator GetDocumentCoroutine(
        string collection,
        string docId,
        Action<string> onComplete
    )
    {
        string url = $"{BaseUrl}/{collection}/{docId}";

        using UnityWebRequest req = UnityWebRequest.Get(url);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            onComplete?.Invoke(req.downloadHandler.text);
        else
        {
            Debug.LogError($"Firestore Read Error: {req.error}");
            onComplete?.Invoke(null);
        }
    }
}

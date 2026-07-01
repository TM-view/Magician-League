using System.Collections.Generic;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    // ==================
    // บันทึกข้อมูล
    // ==================
    public void SavePlayerData(string playerId, string username, int score)
    {
        var fields = new Dictionary<string, object>
        {
            { "username", username },
            { "score", score },
        };

        string json = FirestoreHelper.ToFirestoreJson(fields);

        FirestoreManager.Instance.SetDocument(
            collection: "players",
            docId: playerId,
            jsonBody: json,
            onComplete: (ok) =>
            {
                if (ok)
                    Debug.Log("บันทึกสำเร็จ");
                else
                    Debug.LogError("บันทึกไม่สำเร็จ");
            }
        );
    }

    // ==================
    // โหลดข้อมูล
    // ==================
    public void LoadPlayerData(string playerId)
    {
        FirestoreManager.Instance.GetDocument(
            collection: "players",
            docId: playerId,
            onComplete: (json) =>
            {
                if (json == null)
                {
                    Debug.LogError("โหลดข้อมูลไม่สำเร็จ");
                    return;
                }

                string username = FirestoreHelper.GetStringField(json, "username");

                string scoreStr = FirestoreHelper.GetStringField(json, "score");

                Debug.Log($"Username: {username}, Score: {scoreStr}");
            }
        );
    }
}

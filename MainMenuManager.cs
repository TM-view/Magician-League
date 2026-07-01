using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField]
    private string gameSceneName = "Main"; // ชื่อ Scene เกม

    [SerializeField]
    private TMP_InputField playerNameInput;

    public void OnClickStartMultiplayer()
    {
        PlayerData.Name = playerNameInput.text;
        SceneManager.LoadScene(gameSceneName);
    }
}

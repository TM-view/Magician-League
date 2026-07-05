using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private const int MaxPlayerNameLength = 7;

    [SerializeField]
    private string gameSceneName = "Main";

    [SerializeField]
    private TMP_InputField playerNameInput;

    private void Awake()
    {
        if (playerNameInput != null)
        {
            playerNameInput.characterLimit = MaxPlayerNameLength;
        }
    }

    public void OnClickStartMultiplayer()
    {
        PlayerData.Name = GetValidPlayerName(playerNameInput != null ? playerNameInput.text : "");
        SceneManager.LoadScene(gameSceneName);
    }

    private string GetValidPlayerName(string playerName)
    {
        string validName = string.IsNullOrWhiteSpace(playerName)
            ? GetGuestName()
            : playerName.Trim();

        return validName.Length <= MaxPlayerNameLength
            ? validName
            : validName.Substring(0, MaxPlayerNameLength);
    }

    private string GetGuestName()
    {
        return "Guest" + Random.Range(0, 100).ToString("00");
    }
}

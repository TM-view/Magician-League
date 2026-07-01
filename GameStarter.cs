using UnityEngine;

public class GameStarter : MonoBehaviour
{
    [SerializeField]
    private BasicSpawner basicSpawner;

    private void Start()
    {
        basicSpawner.StartGame();
    }
}

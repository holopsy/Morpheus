using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [Header("First Game Scene")]
    [SerializeField] string firstGameSceneName = "VerticalSlice"; // <-- change to your gameplay scene name

    void Awake()
    {
        // Provide the name to SceneLoader
        SceneLoader.FirstGameSceneName = firstGameSceneName;
        Time.timeScale = 1f; // safety reset
    }

    // Hook these from Button OnClick
    public void OnStartGame() => SceneLoader.LoadFirstGameScene();
    public void OnExit()      => SceneLoader.QuitGame();
}
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Bones Economy")]
    public int bones = 10;   // starting bones
    public int bonesPerKill = 5; // reward per enemy kill
    public TextMeshProUGUI bonesText;

    [Header("UI Panels")]
    public GameObject optionsMenuPanel;
    public GameObject gameOverCanvas;

    private bool isGamePaused = false;

    void Start()
    {
        DisplayBones();
        if (optionsMenuPanel) optionsMenuPanel.SetActive(false);
        if (gameOverCanvas) gameOverCanvas.SetActive(false);
    }

    // --- Bones Economy ---
    public bool SpendBones(int cost)
    {
        if (bones >= cost)
        {
            bones -= cost;
            DisplayBones();
            return true;
        }
        return false;
    }

    public void CollectBones()
    {
        bones += bonesPerKill;
        DisplayBones();
    }

    public int GetBones() => bones;

    private void DisplayBones()
    {
        if (bonesText) bonesText.text = bones.ToString();
    }

    // --- Options Menu ---
    public void OpenOptionsMenu()
    {
        if (!optionsMenuPanel) return;

        optionsMenuPanel.SetActive(true);
        PauseGame();
    }

    public void ResumeGame()
    {
        if (!optionsMenuPanel) return;

        optionsMenuPanel.SetActive(false);
        UnpauseGame();
    }

    public void RestartGame()
    {
        UnpauseGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitToOpeningScene()
    {
        UnpauseGame();
        SceneManager.LoadScene("OpeningScene");
    }

    // --- Game Over ---
    public void GameOver()
    {
        if (!gameOverCanvas) return;

        gameOverCanvas.SetActive(true);
        PauseGame();
    }

    // --- Pause / Unpause ---
    private void PauseGame()
    {
        isGamePaused = true;
        Time.timeScale = 0f;
    }

    private void UnpauseGame()
    {
        isGamePaused = false;
        Time.timeScale = 1f;
    }

    // --- Tower Death Check ---
    public void TowerDead(bool isDead)
    {
        if (isDead)
        {
            GameOver();
            Debug.Log("Game Over! The tower has been destroyed.");
        }
    }
}
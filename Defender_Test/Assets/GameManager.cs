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

    //trying to fix an issue where the opening scene opens the game scene then immediately closes 

   /* private void Awake()
    {
        if (FindObjectsOfType<GameManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }*/
    void Start()
    {
        //displaying the number of bones the player has, and making sure the options and game over screens are hidden 
        DisplayBones();
        if (optionsMenuPanel) optionsMenuPanel.SetActive(false);
        if (gameOverCanvas) gameOverCanvas.SetActive(false);
    }

    // allows the player to spend bones buying defenders 
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

//collects bones when an enemy is killed and displays the changes to the screen 
    public void CollectBones()
    {
        bones += bonesPerKill;
        DisplayBones();
    }

    public int GetBones() => bones;

//displaying the number of bones a player has on the UI 
    private void DisplayBones()
    {
        if (bonesText) bonesText.text = bones.ToString();
    }

   //Opening the options menu, set to a button click 
    public void OpenOptionsMenu()
    {
        if (!optionsMenuPanel) return;

        optionsMenuPanel.SetActive(true);
        PauseGame();
    }

//allowing the player to resume to the game that they were playing and hiding the options menu again 
    public void ResumeGame()
    {
        if (!optionsMenuPanel) return;

        optionsMenuPanel.SetActive(false);
        UnpauseGame();
    }

//reloading the scene to allow the player to restart the game with a new map generation 
    public void RestartGame()
    {
        UnpauseGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    //allows the player to exit back to the opening scene 

    public void ExitToOpeningScene()
    {
        UnpauseGame();
        SceneManager.LoadScene("OpeningScene");
    }

    // opens the game over screen when the tower is dead 
    public void GameOver()
    {
        if (!gameOverCanvas) return;

        gameOverCanvas.SetActive(true);
        PauseGame();
    }

    // pauses the game so nothing happens when the player is in the optuons menu 
    private void PauseGame()
    {
        isGamePaused = true;
        Time.timeScale = 0f;
    }

//unpauses the game if the player wants to resume 
    private void UnpauseGame()
    {
        isGamePaused = false;
        Time.timeScale = 1f;
    }

    // Checks if the tower is dead so it knows when to trigger the Game over display 
    public void TowerDead(bool isDead)
    {
        if (isDead)
        {
            GameOver();
            Debug.Log("Game Over! The tower has been destroyed.");
        }
    }
}
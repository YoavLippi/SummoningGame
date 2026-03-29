using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{

    [Header("Interaction Audio")]
    public AudioSource buttonPressed;
    public AudioSource gameMusic;
    public AudioSource roundMusic;
    public bool isGameMusicPlaying = false;

    [Header("Music Settings")]
    public float musicVolume = 0.5f;
    public Slider musicSlider;

    [Header("Timer Settings")]
    public float timer = 10f;
    public TextMeshProUGUI timerText;

    [Header("Menu UI")]
    public GameObject endRoundPanel;

    public static object Instance { get; internal set; }
    bool roundStarted = false;

    public void Start()
    {
        isGameMusicPlaying = gameMusic.isPlaying;

        if (musicSlider != null)
            musicSlider.value = musicVolume;
    }

    // Menu UI elements (General logic we may need)
    public void StartGame()
    {
        buttonPressed.Play();
        SceneManager.LoadScene("GameScene");
    }

    public void ReturnToMainMenu ()
    {
        buttonPressed.Play();
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame ()
    {
        buttonPressed.Play();
        Application.Quit();
    }

    public void RestartGame ()
    {
        buttonPressed.Play();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void StartRound()
    {
        timer = 10f;
        Time.timeScale = 1f;
        roundStarted = true;
        timerText.text = "10";
    }
    public void EndRound ()
    {
        if (!roundStarted)
            return; // additional check to prevent multiple calls to EndRound

        Time.timeScale = 0f;
        endRoundPanel.SetActive(true);
    }

    // Music control logic (General logic we may need)

    public void ToggleMusic ()
    {
        if (isGameMusicPlaying)
        {
            gameMusic.Pause();
            isGameMusicPlaying = false;
        }
        else
        {
            gameMusic.Play();
            isGameMusicPlaying = true;
        }
    }

    public void SetMusicVolume (float volume)
    {
        musicVolume = volume;
        gameMusic.volume = musicVolume;
    }

    // Timer control logic 

    public void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(timer).ToString();
        }
        else
        {
            // Timer has reached zero
            EndRound();

            Debug.Log("Timer has reached zero!");
        }
    }
}


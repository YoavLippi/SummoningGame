using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Linq;

public class GameManager : MonoBehaviour
{

    [Header("Music Settings")]
    public float musicVolume = 0.5f;
    public Slider musicSlider;

    [Header("Timer Settings")]
    public float timer = 10f;
    public TextMeshProUGUI timerText;

    [Header("Menu UI")]
    public GameObject endRoundPanel;

    [Header("Events")]
    public UnityEvent RoundEnd;

    public static GameManager Instance;

    public GameObject endScreen;
    public GameObject pauseScreen;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = GetComponent<GameManager>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    bool roundStarted = false;

    public void Start()
    {
        // isGameMusicPlaying = gameMusic.isPlaying;

        if (musicSlider != null)
            musicSlider.value = musicVolume;
    }

    // Menu UI elements (General logic we may need)
    public void StartGame()
    {
        // buttonPressed.Play();
        SceneManager.LoadScene(1);
    }

    public void ReturnToMainMenu()
    {
        // buttonPressed.Play();
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        // buttonPressed.Play();
        Application.Quit();
    }

    public void RestartGame()
    {
        // buttonPressed.Play();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void StartRound()
    {
        timer = 10f;
        Time.timeScale = 1f;
        roundStarted = true;
        timerText.text = "10";
    }
    public void EndRound()
    {
        if (!roundStarted)
            return; // additional check to prevent multiple calls to EndRound

        Time.timeScale = 0f;
        endRoundPanel.SetActive(true);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        AudioManager.Instance?.SetVolume(volume);
    }

    // Timer control logic 
    // Adjusting to be invokable globally

    /*public void Update()
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
    }*/

    public static IEnumerator DoTiming(int time)
    {
        yield return new WaitForSeconds(time);
        Instance?.RoundEnd.Invoke();
    }
}

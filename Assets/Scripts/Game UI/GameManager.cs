using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    public AudioSource buttonPressed;
    public AudioSource gameMusic;
    public bool isGameMusicPlaying = false;
    public float musicVolume = 0.5f;

    public void Start()
    {
        isGameMusicPlaying = gameMusic.isPlaying;
    }

    // Menu UI elements
    public void StartGame ()
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

    // Music control

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
}

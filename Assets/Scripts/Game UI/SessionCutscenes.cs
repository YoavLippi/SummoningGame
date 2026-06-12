using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class SessionCutscenes : NetworkBehaviour
{
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    [SerializeField] private GameObject puzzle1CompletePanel;
    [SerializeField] private GameObject bothCutscenePanel;

    public InteractionHandler interactionHandler;

    private bool _isFirstScene = true;

    private void Start()
    {
        StartCoroutine(PlayClip(introPanel, onComplete: () => introPanel.SetActive(false)));
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_isFirstScene)
        {
            _isFirstScene = false;
            return;
        }

        interactionHandler.Initialize();
        StartCoroutine(PlayClip(introPanel, onComplete: () => introPanel.SetActive(false)));
    }

    [ClientRpc]
    public void TriggerWinClientRpc() => StartCoroutine(PlayClip(winPanel, onComplete: LoadMainMenu));

    [ClientRpc]
    public void TriggerLoseClientRpc() => StartCoroutine(PlayClip(losePanel, onComplete: LoadMainMenu));

    public void TriggerPuzzle1Complete()
    {
        StartCoroutine(PlayClip(puzzle1CompletePanel, onComplete: () => puzzle1CompletePanel.SetActive(false)));
    }

    public void TriggerBothCutscene()
    {
        StartCoroutine(PlayClip(bothCutscenePanel, onComplete: LoadMainMenu));
    }

    private IEnumerator PlayClip(GameObject panel, System.Action onComplete)
    {
        panel.SetActive(true);

        MusicManager.Instance.SetPaused(true);

        VideoPlayer video = panel.GetComponent<VideoPlayer>();

        video.Prepare();
        yield return new WaitUntil(() => video.isPrepared);

        video.Play();
        yield return null;
        yield return new WaitUntil(() => !video.isPlaying);

        MusicManager.Instance.SetPaused(false);

        onComplete?.Invoke();
    }

    private void LoadMainMenu()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using Unity.Netcode;

public class SessionCutscenes : NetworkBehaviour // changed to network
{
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    private void Start()
    {
        StartCoroutine(PlayClip(introPanel, onComplete: () => introPanel.SetActive(false)));
    }

    [ClientRpc]
    public void TriggerWinClientRpc() => StartCoroutine(PlayClip(winPanel, onComplete: LoadMainMenu)); // changed to client rpc for both

    [ClientRpc]
    public void TriggerLoseClientRpc() => StartCoroutine(PlayClip(losePanel, onComplete: LoadMainMenu));


    private IEnumerator PlayClip(GameObject panel, System.Action onComplete)
    {
        panel.SetActive(true);

                MusicManager.Instance.SetPaused(true); // stop music

        VideoPlayer video = panel.GetComponent<VideoPlayer>();

        video.Prepare();
        yield return new WaitUntil(() => video.isPrepared);

        video.Play();
        yield return null; // one frame for isPlaying to become true
        yield return new WaitUntil(() => !video.isPlaying);

        MusicManager.Instance.SetPaused(false); // resume music

        onComplete?.Invoke();
    }

    private void LoadMainMenu()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("Main Menu", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Video;
using static UnityEngine.Timeline.DirectorControlPlayable;

public class SessionCutscenes : NetworkBehaviour // changed to network
{
	[SerializeField] private GameObject introPanel;
	[SerializeField] private GameObject winPanel;
	[SerializeField] private GameObject losePanel;

	[SerializeField] private GameObject puzzle1CompletePanel;
	[SerializeField] private GameObject bothCutscenePanel;

    //reference to the input actions to disable them during cutscenes
    private WizardController wizControl;
    private InteractionHandler interactHandler;


    public UnityEvent bothCutsceneCompleteEvent;

	private void Start()
	{
        //find the local player's controllers to disable input during cutscenes
        FindLocalPlayerControllers();
        StartCoroutine(PlayClip(introPanel, onComplete: () => introPanel.SetActive(false)));
	}

    //function  to find local player contraller to disable/enable input during cutscenes
    private void FindLocalPlayerControllers()
    {
        // Wait a frame for player to be spawned/moved, then find
        if (NetworkManager.Singleton?.LocalClient?.PlayerObject != null)
        {
            var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            wizControl = playerObj.GetComponent <WizardController > ();
            interactHandler = playerObj.GetComponent <InteractionHandler > ();
        }
        else
        {
            Debug.LogWarning("[SessionCutscenes] Could not find local player object!");
        }
    }

    [ClientRpc]
	public void TriggerWinClientRpc() => StartCoroutine(PlayClip(winPanel, onComplete: LoadMainMenu)); // changed to client rpc for both

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
        //find the controllers in case scene changed and references are stale
        FindLocalPlayerControllers();

        //disable the player input before cutscene starts
        SetPlayerInputEnabled(false);

        panel.SetActive(true);

        MusicManager.Instance.SetPaused(true); // stop music

		VideoPlayer video = panel.GetComponent<VideoPlayer>();

		video.Prepare();
		yield return new WaitUntil(() => video.isPrepared);

		video.Play();
		yield return null; // one frame for isPlaying to become true
		yield return new WaitUntil(() => !video.isPlaying);

		MusicManager.Instance.SetPaused(false); // resume music

        //then enable player input after cutscene ends
        SetPlayerInputEnabled(true);

        onComplete?.Invoke();
	}

    private void SetPlayerInputEnabled(bool enabled)
    {
        if (wizControl != null)
            wizControl.isDetectingInput = enabled;

        if (interactHandler != null)
            interactHandler.isDetectingInput = enabled;

        Debug.Log($"[SessionCutscenes] Player input set to: {enabled}");
    }

    private void LoadMainMenu()
	{
		NetworkManager.Singleton.SceneManager.LoadScene("Main Menu", UnityEngine.SceneManagement.LoadSceneMode.Single);
	}
}
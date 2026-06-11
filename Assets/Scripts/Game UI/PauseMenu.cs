using System.Collections;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : NetworkBehaviour
{
    //this script is gonna reference the wizardcontroller script to enable /disable the cursor lock state. idk how that makes sense
    //but here we go lol. 
    //want the script to only pause for the local player (these auto comments are SO ANNOYING)

    [Header("UI OBJS")] //the ui objs that will display the pause panel and stuff. someone gotta make this
    [SerializeField] GameObject pausePanel;
    //the panel that will show up when the game is paused. should have a resume button and a quit button. maybe some other stuff idk.
    [SerializeField] GameObject resumeBtn;
    [SerializeField] GameObject exitBtn;

    //[Header("VOICE CHAT CTRLS")] //space for all th voice chat related stuffs to go

    //[Header("NETWORK MANGER CTRLS")] //space for the network manager things to be plugged into

    [Header("INPUT ACTIONS")] //referencing the wizard cntrller and the action map
    [SerializeField] PlayerInput playerInput;
    InputAction pauseAction;
    bool isPaused = false;

    [SerializeField] WizardController wizControl;
    [SerializeField] InteractionHandler interactHandler;
    private bool canToggleVoice;

    void Awake()
    {
        //turning off the panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        //wizControl = GetComponent<WizardController>();
    }

    public override void OnNetworkSpawn()
    {
        StartCoroutine(DelayFindWizControl(1));
    }

    private IEnumerator DelayFindWizControl(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        wizControl = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<WizardController>();
        interactHandler = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<InteractionHandler>();
    }

    void Start()
    {
       if (playerInput != null)
       {
           pauseAction = playerInput.actions["Pause"];
        }
       else
        {
            Debug.LogError("You forgor to assign the pause input!!!");
        }
    }


    void Update()
    {
        //need to check if the player has pressed 'ESC' to pause the game
        if (pauseAction != null && pauseAction.WasPerformedThisFrame())
        {
            TogglePause();
        }
    }

    void TogglePause()
    {
        isPaused = !isPaused; 
        //switch the pausign when pressed then
        if (isPaused)
        {
            //pause the game for local
            PauseGame();
            wizControl.isDetectingInput = false;
            interactHandler.isDetectingInput = false;
        }
        else
        {
            //unpause for local
            ResumeGame();
            wizControl.isDetectingInput = true;
            interactHandler.isDetectingInput = true;
        }
    }

    void PauseGame()
    {
        //open the panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        //unlock the cursor
        if (wizControl != null)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void ResumeGame()
    {
        //remove pause panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        //lock the cursor again
        if (wizControl != null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    //button functions cuz im too lazy
    public void ResumeBtnClicked()
    {
        if (isPaused)
        {
            TogglePause();
        }
    }
    
    public void ExitBtnClicked()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
    }

    //voice chat and networking stuff can go underneath.
    public async void JoinVoiceSession()
    {
        string inputName = NetworkManager.Singleton.LocalClient.ClientId.ToString();
        canToggleVoice = false;
        await VivoxVoiceManager.Instance.InitializeAsync(inputName);
        var loginOptions = new LoginOptions()
        {
            DisplayName = inputName,
            ParticipantUpdateFrequency = ParticipantPropertyUpdateFrequency.FivePerSecond
        };
        await VivoxService.Instance.LoginAsync(loginOptions);
        canToggleVoice = true;
        //CanJoinVoice = false;
    }

    public async void LeaveVoiceSession()
    {
        await VivoxService.Instance.LogoutAsync();
    }
}

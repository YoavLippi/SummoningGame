using System;
using Blocks.Common;
using Unity.Netcode;
using Unity.Properties;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[UxmlElement]
public partial class ReadyButton : Button
{
    const string k_LeaveSessionButtonText = "Ready";
    ISession m_Session;
    SessionObserver m_SessionObserver;
    
    DataBinding m_DataBinding;
    
    [CreateProperty, UxmlAttribute]
    public string SessionType
    {
        get => m_SessionType;
        set
        {
            if (m_SessionType == value)
            {
                return;
            }

            m_SessionType = value;
            if (panel != null)
            {
                UpdateBindings();
            }
        }
    }

    string m_SessionType;
    
    public ReadyButton()
    {
        text = k_LeaveSessionButtonText;
        AddToClassList(BlocksTheme.Button);
        
        m_DataBinding = new DataBinding()
        {
            //dataSourcePath = new PropertyPath(nameof(PlayerCountViewModel.DisplayText)),
            bindingMode = BindingMode.ToTarget
        };
        
        clicked += StartGame;
    }

    void StartGame()
    {
        Debug.Log("test");
        if (m_Session != null) Debug.Log($"Session name: {m_Session.Name}");
        /*if (m_Session?.PlayerCount == m_Session?.MaxPlayers)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Game Scene", LoadSceneMode.Single);   
        }*/
    }
    
    void UpdateBindings()
    {
        CleanupBindings();
        
        if (string.IsNullOrEmpty(m_SessionType))
            return;

        m_SessionObserver = new SessionObserver(m_SessionType);
        m_SessionObserver.SessionAdded += OnSessionAdded;

        if (m_SessionObserver.Session != null)
        {
            OnSessionAdded(m_SessionObserver.Session);
        }
    }

    void CleanupBindings()
    {
        if (m_DataBinding.dataSource is IDisposable disposable)
        {
            disposable.Dispose();
        }

        m_DataBinding.dataSource = null;
    }
    
    void OnSessionAdded(ISession newSession)
    {
        Debug.Log("Added new session");
        m_Session = newSession;
        m_Session.Changed += OnSessionChanged;
        m_Session.RemovedFromSession += OnSessionRemoved;
        m_Session.Deleted += OnSessionRemoved;
        OnSessionChanged();
    }

    void OnSessionRemoved()
    {
        //DisplayText = k_DefaultDisplayText;
        CleanUpSession();
    }

    void CleanUpSession()
    {
        m_Session.RemovedFromSession -= OnSessionRemoved;
        m_Session.Deleted -= OnSessionRemoved;
        m_Session.Changed -= OnSessionChanged;
        m_Session = null;
    }

    void OnSessionChanged()
    {
        //DisplayText = $"{m_Session.Players.Count} / {m_Session.MaxPlayers} Players";
    }
}

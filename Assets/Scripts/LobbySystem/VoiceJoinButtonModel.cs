using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Properties;
using Unity.Services.Multiplayer;
using Unity.Services.Vivox;
using UnityEngine.UIElements;

    class VoiceJoinButtonModel : IDisposable, IDataSourceViewHashProvider, INotifyBindablePropertyChanged
    {
        SessionObserver m_SessionObserver;
        ISession m_Session;
        long m_UpdateVersion;

        string playerName;

        [CreateProperty]
        public string PlayerName
        {
            get => playerName;
            private set
            {
                if (playerName == value)
                {
                    return;
                }

                playerName = value;
                Notify();
                ++m_UpdateVersion;
            }
        }

        [CreateProperty]
        public bool CanToggleVoice
        {
            get => m_CanLeaveSession;
            private set
            {
                if (m_CanLeaveSession == value)
                {
                    return;
                }

                m_CanLeaveSession = value;
                Notify();
                ++m_UpdateVersion;
            }
        }
        
        [CreateProperty]
        public string DisplayText
        {
            get => m_DisplayText;
            private set
            {
                if (m_DisplayText == value)
                {
                    return;
                }

                m_DisplayText = value;
                Notify();
                ++m_UpdateVersion;
            }
        }

        private string m_DisplayText = "Join Voice";

        bool m_CanLeaveSession;

        public VoiceJoinButtonModel(string sessionType)
        {
            m_SessionObserver = new SessionObserver(sessionType);
            m_SessionObserver.SessionAdded += OnSessionAdded;
            if (m_SessionObserver.Session != null)
            {
                OnSessionAdded(m_SessionObserver.Session);
            }
        }

        public async void JoinVoiceSession(string inputName)
        {
            CanToggleVoice = false;
            await VivoxVoiceManager.Instance.InitializeAsync(inputName);
            var loginOptions = new LoginOptions()
            {
                DisplayName = inputName,
                ParticipantUpdateFrequency = ParticipantPropertyUpdateFrequency.FivePerSecond
            };
            await VivoxService.Instance.LoginAsync(loginOptions);
            DisplayText = "Leave Voice";
            CanToggleVoice = true;
            //CanJoinVoice = false;
        }

        public async void LeaveVoiceSession()
        {
            await VivoxService.Instance.LogoutAsync();
            DisplayText = "Join Voice";
            //CanJoinVoice = true;
        }

        void OnSessionAdded(ISession newSession)
        {
            m_Session = newSession;
            m_Session.RemovedFromSession += OnSessionRemoved;
            m_Session.Deleted += OnSessionRemoved;
            CanToggleVoice = true;
        }

        void OnSessionRemoved()
        {
            CanToggleVoice = false;
            CleanupSession();
        }

        void CleanupSession()
        {
            m_Session.RemovedFromSession -= OnSessionRemoved;
            m_Session.Deleted -= OnSessionRemoved;
            m_Session = null;
        }

        public void Dispose()
        {
            if (m_SessionObserver != null)
            {
                m_SessionObserver.Dispose();
                m_SessionObserver = null;
            }

            if (m_Session != null)
            {
                CleanupSession();
            }
        }

        /// <summary>
        /// This method is used by UIToolkit to determine if any data bound to the UI has changed.
        /// Instead of hashing the data, an m_CanLeaveSession boolean is toggled when changes occur.
        /// </summary>
        public long GetViewHashCode() => m_UpdateVersion;

        /// <summary>
        /// Suggested implementation of INotifyBindablePropertyChanged from UIToolkit.
        /// </summary>
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        void Notify([CallerMemberName] string property = null)
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
    }

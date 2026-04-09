using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Properties;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UIElements;

    public class StartButtonModel : INotifyBindablePropertyChanged, IDataSourceViewHashProvider, IDisposable
    {
        const string k_DefaultDisplayText = "Start";

        SessionObserver m_SessionObserver;
        ISession m_Session;
        long m_UpdateVersion;

        private int MaxPlayers;
        private int CurrentPlayers;
        public bool b_isEnabled;
        
        [CreateProperty]
        public bool IsEnabled
        {
            get => b_isEnabled;
            private set
            {
                if (b_isEnabled == value) return; 
                
                b_isEnabled = value;
                ++m_UpdateVersion;
                Notify();
            }
        }

        /// <summary>
        /// This property is bound to <see cref="PlayerCountLabel.text"/> so that the label displays the number of
        /// players in the session.
        /// It is a property using [CreateProperty] attribute to allow for data binding in UIToolkit
        /// <summary>
        [CreateProperty]
        public string DisplayText
        {
            get => m_DisplayText;
            set
            {
                if (m_DisplayText == value)
                {
                    return;
                }

                m_DisplayText = value;
                ++m_UpdateVersion;
                Notify();
            }
        }
        string m_DisplayText = k_DefaultDisplayText;

        public StartButtonModel(string sessionType)
        {
            m_SessionObserver = new SessionObserver(sessionType);
            m_SessionObserver.SessionAdded += OnSessionAdded;

            if (m_SessionObserver.Session != null)
            {
                OnSessionAdded(m_SessionObserver.Session);
            }
        }

        void OnSessionAdded(ISession newSession)
        {
            m_Session = newSession;
            m_Session.Changed += OnSessionChanged;
            m_Session.RemovedFromSession += OnSessionRemoved;
            m_Session.Deleted += OnSessionRemoved;
            IsEnabled = false;
        }

        void OnSessionRemoved()
        {
            DisplayText = k_DefaultDisplayText;
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
            CurrentPlayers = m_Session.PlayerCount;
            MaxPlayers = m_Session.MaxPlayers;
            if (NetworkManager.Singleton.IsServer)
            {
                IsEnabled = (CurrentPlayers == MaxPlayers);
            }
            else
            {
                IsEnabled = false;
                DisplayText = "Not Host!";
            }
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
                CleanUpSession();
            }
        }

        /// <summary>
        /// This method is used by UIToolkit to determine if any data bound to the UI has changed.
        /// Instead of hashing the data, an m_UpdateVersion counter is incremented when changes occur.
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

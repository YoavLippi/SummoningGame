using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Netcode;
using Unity.Properties;
using Unity.Services.Multiplayer;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UIElements;

    class VoiceJoinButtonModel : IDisposable, IDataSourceViewHashProvider, INotifyBindablePropertyChanged
    {
        SessionObserver m_SessionObserver;
        ISession m_Session;
        long m_UpdateVersion;

        string playerName;
        int m_PermissionAskedCount;

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
        
        //Bunch of permission asking stuff
        #if (UNITY_ANDROID && !UNITY_EDITOR) || __ANDROID__
    bool IsAndroid12AndUp()
    {
        // android12VersionCode is hardcoded because it might not be available in all versions of Android SDK
        const int android12VersionCode = 31;
        AndroidJavaClass buildVersionClass = new AndroidJavaClass("android.os.Build$VERSION");
        int buildSdkVersion = buildVersionClass.GetStatic<int>("SDK_INT");

        return buildSdkVersion >= android12VersionCode;
    }

    string GetBluetoothConnectPermissionCode()
    {
        if (IsAndroid12AndUp())
        {
            // UnityEngine.Android.Permission does not contain the BLUETOOTH_CONNECT permission, fetch it from Android
            AndroidJavaClass manifestPermissionClass = new AndroidJavaClass("android.Manifest$permission");
            string permissionCode = manifestPermissionClass.GetStatic<string>("BLUETOOTH_CONNECT");

            return permissionCode;
        }

        return "";
    }
#endif

    bool IsMicPermissionGranted()
    {
        bool isGranted = Permission.HasUserAuthorizedPermission(Permission.Microphone);
#if (UNITY_ANDROID && !UNITY_EDITOR) || __ANDROID__
        if (IsAndroid12AndUp())
        {
            // On Android 12 and up, we also need to ask for the BLUETOOTH_CONNECT permission for all features to work
            isGranted &= Permission.HasUserAuthorizedPermission(GetBluetoothConnectPermissionCode());
        }
#endif
        return isGranted;
    }

    void AskForPermissions()
    {
        string permissionCode = Permission.Microphone;

#if (UNITY_ANDROID && !UNITY_EDITOR) || __ANDROID__
        if (m_PermissionAskedCount == 1 && IsAndroid12AndUp())
        {
            permissionCode = GetBluetoothConnectPermissionCode();
        }
#endif
        m_PermissionAskedCount++;
        Permission.RequestUserPermission(permissionCode);
    }

    bool IsPermissionsDenied()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR) || __ANDROID__
        // On Android 12 and up, we also need to ask for the BLUETOOTH_CONNECT permission
        if (IsAndroid12AndUp())
        {
            return m_PermissionAskedCount == 2;
        }
#endif
        return m_PermissionAskedCount == 1;
    }

    public void LoginToVivoxService(string inName)
    {
        Debug.Log("Attempting vivox login");
        if (IsMicPermissionGranted())
        {
            // The user authorized use of the microphone.
            LoginToVivox(inName);
        }
        else
        {
            // We do not have the needed permissions.
            // Ask for permissions or proceed without the functionality enabled if they were denied by the user
            if (IsPermissionsDenied())
            {
                m_PermissionAskedCount = 0;
                LoginToVivox(inName);
            }
            else
            {
                AskForPermissions();
            }
        }
    }

    async void LoginToVivox(string inName)
    {
        CanToggleVoice = false;
        DisplayText = "Joining...";
        await VivoxVoiceManager.Instance.InitializeAsync(inName);
        var loginOptions = new LoginOptions()
        {
            DisplayName = inName,
            ParticipantUpdateFrequency = ParticipantPropertyUpdateFrequency.FivePerSecond
        };
        await VivoxService.Instance.LoginAsync(loginOptions);
        await VivoxService.Instance.JoinGroupChannelAsync(m_Session.Id, ChatCapability.AudioOnly);
        
        DisplayText = "Leave Voice";
        CanToggleVoice = true;
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

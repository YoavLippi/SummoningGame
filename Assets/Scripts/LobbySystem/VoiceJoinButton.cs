using System;
using Blocks.Common;
using Unity.Properties;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UIElements;

    [UxmlElement]
    public partial class VoiceJoinButton : Button
    {
        private string playerName;

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

        DataBinding m_DataBinding, m_nameBinding, m_textBinding;
        VoiceJoinButtonModel m_ViewModel;

        public VoiceJoinButton()
        {
            
            m_DataBinding = new DataBinding()
            {
                dataSourcePath = new PropertyPath(nameof(VoiceJoinButtonModel.CanToggleVoice)), 
                bindingMode = BindingMode.ToTarget
            };
            SetBinding(new BindingId(nameof(enabledSelf)), m_DataBinding);
            
            m_nameBinding = new DataBinding()
            {
                dataSourcePath = new PropertyPath(nameof(VoiceJoinButtonModel.PlayerName)),
                bindingMode = BindingMode.ToTarget
            };
            SetBinding(new BindingId(nameof(playerName)), m_nameBinding);

            m_textBinding = new DataBinding()
            {
                dataSourcePath = new PropertyPath(nameof(VoiceJoinButtonModel.DisplayText)),
                bindingMode = BindingMode.ToTarget
            };
            SetBinding(new BindingId(nameof(text)), m_textBinding);
            
            clicked += ToggleJoin;

            RegisterCallback<AttachToPanelEvent>(_ => UpdateBindings());
            RegisterCallback<DetachFromPanelEvent>(_ => CleanupBindings());
        }

        void ToggleJoin()
        {
            if (VivoxService.Instance.IsLoggedIn)
            {
                m_ViewModel.LeaveVoiceSession();
            }
            else
            {
                m_ViewModel.LoginToVivoxService(playerName);
                Debug.Log($"Player name is {playerName}");
            }
        }

        void UpdateBindings()
        {
            CleanupBindings();
            m_ViewModel = new VoiceJoinButtonModel(m_SessionType);
            m_nameBinding.dataSource = m_ViewModel;
            m_DataBinding.dataSource = m_ViewModel;
            m_textBinding.dataSource = m_ViewModel;
        }

        void CleanupBindings()
        {
            if (m_nameBinding.dataSource is IDisposable disposable)
            {
                disposable.Dispose();
            }

            m_ViewModel = null;
            m_ViewModel = null;
            m_nameBinding.dataSource = null;
            m_DataBinding.dataSource = null;
            m_textBinding.dataSource = null;
        }
    }
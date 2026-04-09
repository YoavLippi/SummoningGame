using System;
using Blocks.Common;
using Unity.Netcode;
using Unity.Properties;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
    [UxmlElement]
    public partial class StartButton : Button
    {
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

        DataBinding displayTextBinding, enabledBinding;
        StartButtonModel m_Model;

        /*public LeaveSessionButton()
        {
            text = k_LeaveSessionButtonText;

            AddToClassList(BlocksTheme.Button);
            m_DataBinding = new DataBinding() { dataSourcePath = new PropertyPath(nameof(LeaveSessionViewModel.CanLeaveSession)), bindingMode = BindingMode.ToTarget };
            SetBinding(new BindingId(nameof(enabledSelf)), m_DataBinding);
            clicked += LeaveSession;

            RegisterCallback<AttachToPanelEvent>(_ => UpdateBindings());
            RegisterCallback<DetachFromPanelEvent>(_ => CleanupBindings());
        }*/
        public StartButton()
        {
            AddToClassList(BlocksTheme.Label);
            BindData();

            clicked += StartGame;
            RegisterCallback<AttachToPanelEvent>(_ => UpdateBindings());
            RegisterCallback<DetachFromPanelEvent>(_ => CleanupBindings());
        }

        private void BindData()
        {
            displayTextBinding = new DataBinding()
            {
                dataSourcePath = new PropertyPath(nameof(StartButtonModel.DisplayText)),
                bindingMode = BindingMode.ToTarget
            };
            SetBinding(new BindingId(nameof(text)), displayTextBinding);
            
            enabledBinding = new DataBinding()
            {
                dataSourcePath = new PropertyPath(nameof(StartButtonModel.IsEnabled)),
                bindingMode = BindingMode.ToTarget
            };
            SetBinding(new BindingId(nameof(enabledSelf)), enabledBinding);
        }
        
        //Should allow any client to start the game if they click the button
        //MVC-wise this is bad logic, but we'll have to do it like this because of time constraints
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void StartGame()
        {
            if (SessionType != null) Debug.Log($"Session name: {SessionType}");
            
            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.Log("Not server, ignoring scene load");
                return;
            }
            
            if (enabledSelf)
            {
                NetworkManager.Singleton.SceneManager.LoadScene("Game Scene", LoadSceneMode.Single);
            }
        }

        void UpdateBindings()
        {
            CleanupBindings();
            m_Model = new StartButtonModel(m_SessionType);
            displayTextBinding.dataSource = m_Model;
            enabledBinding.dataSource = m_Model;
        }

        void CleanupBindings()
        {
            if (displayTextBinding.dataSource is IDisposable disposable)
            {
                disposable.Dispose();
            }

            displayTextBinding.dataSource = null;
            enabledBinding.dataSource = null;
        }
    }
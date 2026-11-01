using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

// TODO: Refactor this whole shit.
namespace Arman.DevelopmentConsole
{
    public class DevelopmentConsolePanel : MonoBehaviour
    {
        [FormerlySerializedAs("groupPrefab")]
        public DevelopmentGroup GroupPrefab;

        [FormerlySerializedAs("commandInputPrompt")]
        public CommandInputPrompt CommandInputPrompt;

        [FormerlySerializedAs("toolsPanel")]
        public Canvas ToolsPanel;

        [FormerlySerializedAs("devButton")]
        public Button DevButton;

        [FormerlySerializedAs("devButtonText")]
        public Text DevButtonText;

        [FormerlySerializedAs("groupsContainer")]
        public GameObject GroupsContainer;

        [FormerlySerializedAs("onErrorDetected")]
        public UnityEvent OnErrorDetected;

        [FormerlySerializedAs("onToolsPanelOpened")]
        public UnityEvent OnToolsPanelOpened;

        [FormerlySerializedAs("onToolsPanelClosed")]
        public UnityEvent OnToolsPanelClosed;

        List<ShortCutInfo> _shortCutInfos = new List<ShortCutInfo>();

        private void Awake()
        {
            ToolsPanel.gameObject.SetActive(false);
            Init();
        }

        public void Init()
        {
            //ServiceLocator.Find<ConfigurationManager>().Configure(this);

            var definitionTypes = ReflectionUtilities.FindTypesOf(
                typeof(DevelopmentOptionsDefinition),
                false
            );

            foreach (var type in definitionTypes)
            {
                InitCommands(type);
                InitShortCuts(type);
            }

            Application.logMessageReceived += CheckForErros;
        }

        void CheckForErros(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
                OnErrorDetected.Invoke();
        }

        // TODO: Refactor this shit.
        public void InitShortCuts(Type type)
        {
            var methods = GetMethodsWithAttribute(type, typeof(ShortCutAttribute));
            foreach (var method in methods)
            {
                if (method.IsStatic == false)
                {
                    Debug.LogErrorFormat(
                        "Method {0} in {1} is not static.",
                        method,
                        method.DeclaringType
                    );
                    continue;
                }

                var attributes = method.GetCustomAttributes(typeof(ShortCutAttribute), false);
                var attribute = (attributes[0] as ShortCutAttribute);

                var shortCutInfo = new ShortCutInfo(
                    attribute.KeyCodes,
                    () => method.Invoke(null, null)
                );

                _shortCutInfos.Add(shortCutInfo);
            }
        }

        private void Update()
        {
            foreach (var shortCut in _shortCutInfos)
            {
                if (ArePressed(shortCut.KeyCodes))
                    shortCut.Action();
            }
        }

        private bool ArePressed(KeyCode[] keyCodes)
        {
            bool atleastOneIsDown = false;

            foreach (var key in keyCodes)
            {
                if (Input.GetKeyDown(key))
                    atleastOneIsDown = true;

                if (Input.GetKeyDown(key) == false && Input.GetKey(key) == false)
                    return false;
            }

            return atleastOneIsDown;
        }

        // TODO: Refactor this shit.
        public void InitCommands(Type type)
        {
            var methods = GetMethodsWithAttribute(type, typeof(DevOptionAttribute));

            var groups = new Dictionary<string, List<string>>();
            var commands = new Dictionary<string, CommandInfo>();

            foreach (var method in methods)
            {
                if (method.IsStatic == false)
                {
                    Debug.LogErrorFormat(
                        "Method {0} in {1} is not static.",
                        method,
                        method.DeclaringType
                    );
                    continue;
                }
                var attributes = method.GetCustomAttributes(typeof(DevOptionAttribute), false);
                var attribute = (attributes[0] as DevOptionAttribute);

                if (groups.ContainsKey(attribute.Group) == false)
                    groups[attribute.Group] = new List<string>();

                groups[attribute.Group].Add(attribute.CommandName);

                var commandInfo = new CommandInfo(attribute.CommandName, method);

                var shortcuts = method.GetCustomAttributes(typeof(ShortCutAttribute), false);
                if (shortcuts.Length > 0)
                {
                    commandInfo.SetShortcut((shortcuts[0] as ShortCutAttribute).KeyCodes);
                }

                commands.Add(attribute.CommandName, commandInfo);
            }

            foreach (var group in groups)
            {
                var groupObject = Instantiate(GroupPrefab, this.GroupsContainer.transform, false);

                groupObject.Init(group.Key);

                foreach (var commandName in group.Value)
                    groupObject.AddCommand(commands[commandName]);
            }
        }

        public static IEnumerable<MethodInfo> GetMethodsWithAttribute(
            Type classType,
            Type attributeType
        )
        {
            return classType
                .GetMethods()
                .Where(methodInfo =>
                    methodInfo.GetCustomAttributes(attributeType, true).Length > 0
                );
        }

        public static IEnumerable<MemberInfo> GetMembersWithAttribute(
            Type classType,
            Type attributeType
        )
        {
            return classType
                .GetMembers()
                .Where(memberInfo =>
                    memberInfo.GetCustomAttributes(attributeType, true).Length > 0
                );
        }

        [ShortCut(KeyCode.LeftShift, KeyCode.D)]
        public void Toggle()
        {
            ToolsPanel.gameObject.SetActive(!ToolsPanel.gameObject.activeSelf);
            if (ToolsPanel.gameObject.activeSelf)
                OnToolsPanelOpened.Invoke();
            else
                OnToolsPanelClosed.Invoke();
        }

        // TODO: Refactor this shit
        [DevOptionAttribute("Other", "Toggle Dev Visibility")]
        public void ToggleHideDevToolsButton()
        {
            var color = DevButton.image.color;
            if (color.a <= 0.1)
            {
                color.a = 1;
                DevButtonText.enabled = true;
            }
            else
            {
                color.a = 0;
                DevButtonText.enabled = false;
            }

            DevButton.image.color = color;
        }

        public void OpenCommandInputPromtFor(CommandInfo commandInfo)
        {
            CommandInputPrompt.Init(commandInfo);
        }
    }
}

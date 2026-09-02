using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// TODO: Refactor this whole shit.
namespace Arman.DevelopmentConsole
{

    public class DevelopmentConsolePanel : MonoBehaviour
    {
        public DevelopmentGroup groupPrefab;
        public CommandInputPrompt commandInputPrompt;
        public Canvas toolsPanel;
        public Button devButton;
        public Text devButtonText;
        public GameObject groupsContainer;

        public UnityEvent onErrorDetected;
        public UnityEvent onToolsPanelOpened;
        public UnityEvent onToolsPanelClosed;

        List<ShortCutInfo> shortCutInfos = new List<ShortCutInfo>();

        private void Awake()
        {
            toolsPanel.gameObject.SetActive(false);
            //reporter.gameObject.SetActive(false);
            Init();
        }
        public void Init()
        {
            //ServiceLocator.Find<ConfigurationManager>().Configure(this);

            var definitionTypes = ReflectionUtilities.FindTypesOf(typeof(DevelopmentOptionsDefinition), false);

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
                onErrorDetected.Invoke();

        }
        // TODO: Refactor this shit.
        public void InitShortCuts(Type type)
        {
            var methods = GetMethodsWithAttribute(type, typeof(ShortCutAttribute));
            foreach (var method in methods)
            {
                if (method.IsStatic == false)
                {
                    Debug.LogErrorFormat("Method {0} in {1} is not static.", method, method.DeclaringType);
                    continue;
                }

                var attributes = method.GetCustomAttributes(typeof(ShortCutAttribute), false);
                var attribute = (attributes[0] as ShortCutAttribute);

                var shortCutInfo = new ShortCutInfo(attribute.keyCodes, () => method.Invoke(null, null));

                shortCutInfos.Add(shortCutInfo);
            }
        }

        private void Update()
        {
            foreach (var shortCut in shortCutInfos)
            {
                if (ArePressed(shortCut.keyCodes))
                    shortCut.action();
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
                if(method.IsStatic == false)
                {
                    Debug.LogErrorFormat("Method {0} in {1} is not static.", method, method.DeclaringType);
                    continue;
                }
                var attributes = method.GetCustomAttributes(typeof(DevOptionAttribute), false);
                var attribute = (attributes[0] as DevOptionAttribute);

                if (groups.ContainsKey(attribute.group) == false)
                    groups[attribute.group] = new List<string>();

                groups[attribute.group].Add(attribute.commandName);

                var commandInfo = new CommandInfo(attribute.commandName, method);

                var shortcuts = method.GetCustomAttributes(typeof(ShortCutAttribute), false);
                if(shortcuts.Length > 0)
                {
                    commandInfo.SetShortcut((shortcuts[0] as ShortCutAttribute).keyCodes);
                }

                commands.Add(attribute.commandName, commandInfo);
            }

            foreach (var group in groups)
            {
                var groupObject = Instantiate(groupPrefab, this.groupsContainer.transform, false);

                groupObject.Init(group.Key);

                foreach (var commandName in group.Value)
                    groupObject.AddCommand(commands[commandName]);

            }
        }


        public static IEnumerable<MethodInfo> GetMethodsWithAttribute(Type classType, Type attributeType)
        {
            return classType.GetMethods().Where(methodInfo => methodInfo.GetCustomAttributes(attributeType, true).Length > 0);
        }

        public static IEnumerable<MemberInfo> GetMembersWithAttribute(Type classType, Type attributeType)
        {
            return classType.GetMembers().Where(memberInfo => memberInfo.GetCustomAttributes(attributeType, true).Length > 0);
        }


        [ShortCut(KeyCode.LeftShift, KeyCode.D)]
        public void Toggle()
        {
            toolsPanel.gameObject.SetActive(!toolsPanel.gameObject.activeSelf);
            if (toolsPanel.gameObject.activeSelf)
                onToolsPanelOpened.Invoke();
            else
                onToolsPanelClosed.Invoke();
        }

        // TODO: Refactor this shit
        [DevOptionAttribute("Other", "Toggle Dev Visibility")]
        public void ToggleHideDevToolsButton()
        {
            var color = devButton.image.color;
            if (color.a <= 0.1)
            {
                color.a = 1;
                devButtonText.enabled = true;
            }
            else
            {
                color.a = 0;
                devButtonText.enabled = false;
            }

            devButton.image.color = color;

        }


        public void OpenCommandInputPromtFor(CommandInfo commandInfo)
        {
            commandInputPrompt.Init(commandInfo);
        }
    }
}
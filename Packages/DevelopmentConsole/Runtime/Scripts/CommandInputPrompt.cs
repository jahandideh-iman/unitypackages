using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Arman.DevelopmentConsole
{
    public class CommandInputPrompt : MonoBehaviour
    {
        [FormerlySerializedAs("inputFieldsContainer")]
        public RectTransform InputFieldsContainer;

        [FormerlySerializedAs("inputFieldPrefab")]
        public InputField InputFieldPrefab;

        List<InputField> _inputFields = new List<InputField>();

        CommandInfo _commandInfo;

        public void Init(CommandInfo commandInfo)
        {
            this._commandInfo = commandInfo;
            this.gameObject.SetActive(true);

            foreach (var param in commandInfo.MethodInfo.GetParameters())
            {
                var inputField = Instantiate(InputFieldPrefab, InputFieldsContainer, false);
                inputField.placeholder.As<Text>().text = param.Name;

                var type = param.ParameterType;
                if (type == typeof(int))
                    inputField.contentType = InputField.ContentType.IntegerNumber;
                else if (type == typeof(float))
                    inputField.contentType = InputField.ContentType.DecimalNumber;
                else if (type == typeof(string))
                    inputField.contentType = InputField.ContentType.Standard;
                else
                    Debug.LogErrorFormat("Parameter of type {0} is not supported", type);

                _inputFields.Add(inputField);
            }
        }

        public void Cancel()
        {
            Close();
        }

        public void Execute()
        {
            var parameters = _commandInfo.MethodInfo.GetParameters();
            var inputs = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; ++i)
            {
                var type = parameters[i].ParameterType;
                var stringValue = _inputFields[i].text;
                object value = null;
                if (type == typeof(int))
                    value = int.Parse(stringValue);
                else if (type == typeof(float))
                    value = float.Parse(stringValue);
                else if (type == typeof(string))
                    value = stringValue;
                else
                    Debug.LogErrorFormat("Parameter of type {0} is not supported", type);

                inputs[i] = value;
            }

            _commandInfo.Invoke(inputs);

            Close();
        }

        void Close()
        {
            foreach (var field in _inputFields)
                Destroy(field.gameObject);

            _inputFields.Clear();
            this.gameObject.SetActive(false);
        }
    }
}

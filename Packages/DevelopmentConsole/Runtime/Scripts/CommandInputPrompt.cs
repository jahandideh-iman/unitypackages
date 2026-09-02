using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arman.DevelopmentConsole
{
    public class CommandInputPrompt : MonoBehaviour
    {
        public RectTransform inputFieldsContainer;
        public InputField inputFieldPrefab;

        List<InputField> inputFields = new List<InputField>();

        CommandInfo commandInfo;

        public void Init(CommandInfo commandInfo)
        {
            this.commandInfo = commandInfo;
            this.gameObject.SetActive(true);

            foreach(var param in commandInfo.methodInfo.GetParameters())
            {
                var inputField = Instantiate(inputFieldPrefab, inputFieldsContainer, false);
                inputField.placeholder.As<Text>().text = param.Name;

                var type = param.ParameterType;
                if (type == typeof(int))
                    inputField.contentType = InputField.ContentType.IntegerNumber;
                else if(type == typeof(float))
                    inputField.contentType = InputField.ContentType.DecimalNumber;
                else if(type == typeof(string))
                    inputField.contentType = InputField.ContentType.Standard;
                else
                    Debug.LogErrorFormat("Parameter of type {0} is not supproted", type);

                inputFields.Add(inputField);
            }
        }

        public void Cancel()
        {
            Close();
        }

        public void Execute()
        {
            var paramters = commandInfo.methodInfo.GetParameters();
            var inputs = new object[paramters.Length];

            for(int i = 0; i< paramters.Length; ++i )
            {
                var type = paramters[i].ParameterType;
                var stringValue = inputFields[i].text;
                object value = null;
                if (type == typeof(int))
                    value = int.Parse(stringValue);
                else if (type == typeof(float))
                    value = float.Parse(stringValue);
                else if (type == typeof(string))
                    value = stringValue;
                else
                    Debug.LogErrorFormat("Parameter of type {0} is not supproted", type);

                inputs[i] = value;
            }


            commandInfo.Invoke(inputs);

            Close();
        }

        void Close()
        {

            foreach (var field in inputFields)
                Destroy(field.gameObject);

            inputFields.Clear();
            this.gameObject.SetActive(false);
        }
    }
}
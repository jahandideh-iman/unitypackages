using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arman.PersistentDataManagement.Samples
{
    public class ExamplePersistentDataSerializer : IPersistentDataSerializer
    {
        PersistentDataExample persistentDataExample;

        public ExamplePersistentDataSerializer(PersistentDataExample persistentDataExample)
        {
            this.persistentDataExample = persistentDataExample;
        }

        public string Key()
        {
            return "ExampleSerializer";
        }

        public void DeserializeFrom(IReadablePersistentDataEncoder persistentDataEncoder)
        {
            persistentDataExample.intValue = persistentDataEncoder.ReadInt("int");
            persistentDataExample.floatValue = persistentDataEncoder.ReadFloat("float");
            persistentDataExample.stringValue = persistentDataEncoder.ReadString("string");
        }

        public void SerializeTo(IWritablePersistentDataEncoder persistentDataEncoder)
        {
            persistentDataEncoder.WriteInt("int", persistentDataExample.intValue);
            persistentDataEncoder.WriteFloat("float", persistentDataExample.floatValue);
            persistentDataEncoder.WriteString("string", persistentDataExample.stringValue);
        }
    }

    public class PersistentDataExample : MonoBehaviour
    {
        IPersistentDataManager persistentDataManager;

        public Text intText;
        public Text floatText;
        public Text stringText;

        [HideInInspector]
        public int intValue;

        [HideInInspector]
        public float floatValue;

        [HideInInspector]
        public string stringValue;

        private void Awake()
        {
            persistentDataManager = PersistentDataManagerFactory.CreateDefault();

            persistentDataManager.Register(new ExamplePersistentDataSerializer(this));
            UpdateGUI();
        }

        public void SetInt(string valueStr)
        {
            intValue = int.Parse(valueStr);
            UpdateGUI();
        }

        public void SetFloat(string valueStr)
        {
            floatValue = float.Parse(valueStr);
            UpdateGUI();
        }

        public void SetString(string valueStr)
        {
            stringValue = valueStr;
            UpdateGUI();
        }

        public void Save()
        {
            persistentDataManager.SaveAll();
        }

        public void Load()
        {
            persistentDataManager.LoadAll();
            UpdateGUI();
        }

        void UpdateGUI()
        {
            intText.text = $"int is {intValue}";
            floatText.text = $"float is {floatValue}";
            stringText.text = $"string is \"{stringValue}\"";
        }
    }
}

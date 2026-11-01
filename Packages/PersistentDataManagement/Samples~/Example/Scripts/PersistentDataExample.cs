using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Arman.PersistentDataManagement.Samples
{
    public class ExamplePersistentDataSerializer : IPersistentDataSerializer
    {
        PersistentDataExample _persistentDataExample;

        public ExamplePersistentDataSerializer(PersistentDataExample persistentDataExample)
        {
            this._persistentDataExample = persistentDataExample;
        }

        public string Key()
        {
            return "ExampleSerializer";
        }

        public void DeserializeFrom(IReadablePersistentDataEncoder persistentDataEncoder)
        {
            _persistentDataExample.IntValue = persistentDataEncoder.ReadInt("int");
            _persistentDataExample.FloatValue = persistentDataEncoder.ReadFloat("float");
            _persistentDataExample.StringValue = persistentDataEncoder.ReadString("string");
        }

        public void SerializeTo(IWritablePersistentDataEncoder persistentDataEncoder)
        {
            persistentDataEncoder.WriteInt("int", _persistentDataExample.IntValue);
            persistentDataEncoder.WriteFloat("float", _persistentDataExample.FloatValue);
            persistentDataEncoder.WriteString("string", _persistentDataExample.StringValue);
        }
    }

    public class PersistentDataExample : MonoBehaviour
    {
        IPersistentDataManager _persistentDataManager;

        [FormerlySerializedAs("intText")]
        public Text IntText;

        [FormerlySerializedAs("floatText")]
        public Text FloatText;

        [FormerlySerializedAs("stringText")]
        public Text StringText;

        [FormerlySerializedAs("intValue")]
        [HideInInspector]
        public int IntValue;

        [FormerlySerializedAs("floatValue")]
        [HideInInspector]
        public float FloatValue;

        [FormerlySerializedAs("stringValue")]
        [HideInInspector]
        public string StringValue;

        private void Awake()
        {
            _persistentDataManager = PersistentDataManagerFactory.CreateDefault();

            _persistentDataManager.Register(new ExamplePersistentDataSerializer(this));
            UpdateGUI();
        }

        public void SetInt(string valueStr)
        {
            IntValue = int.Parse(valueStr);
            UpdateGUI();
        }

        public void SetFloat(string valueStr)
        {
            FloatValue = float.Parse(valueStr);
            UpdateGUI();
        }

        public void SetString(string valueStr)
        {
            StringValue = valueStr;
            UpdateGUI();
        }

        public void Save()
        {
            _persistentDataManager.SaveAll();
        }

        public void Load()
        {
            _persistentDataManager.LoadAll();
            UpdateGUI();
        }

        void UpdateGUI()
        {
            IntText.text = $"int is {IntValue}";
            FloatText.text = $"float is {FloatValue}";
            StringText.text = $"string is \"{StringValue}\"";
        }
    }
}

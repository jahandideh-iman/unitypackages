using Arman.Foundation.Core.PersistentDataManagement;
using Arman.Foundation.Unity.PersistentDataManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arman.Samples.PersistentDataManagement
{

    public class ExamplePersistentDataSerializer : PersistentDataSerializer
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


        public void DeserializeFrom(ReadablePersistentDataWrapper persistentDataWrapper)
        {
            persistentDataExample.intValue = persistentDataWrapper.ReadInt("int");
            persistentDataExample.floatValue = persistentDataWrapper.ReadFloat("float");
            persistentDataExample.stringValue = persistentDataWrapper.ReadString("string");
        }

        public void SerializeTo(WritablePersistentDataWrapper persistentDataWrapper)
        {
            persistentDataWrapper.WriteInt("int", persistentDataExample.intValue);
            persistentDataWrapper.WriteFloat("float", persistentDataExample.floatValue);
            persistentDataWrapper.WriteString("string", persistentDataExample.stringValue);
        }
    }
    public class PersistentDataExample : MonoBehaviour
    {
        PersistentDataManager persistentDataManager;

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
            persistentDataManager = new BasicPersistentDataManager();
            persistentDataManager.SetPersistentDataIOStreamFactory(new FileBasedPersistetDataIOStreamFactory(Application.persistentDataPath));
            persistentDataManager.SetPersistentDataWrapper(new JSONPersistentDataWrapper());

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

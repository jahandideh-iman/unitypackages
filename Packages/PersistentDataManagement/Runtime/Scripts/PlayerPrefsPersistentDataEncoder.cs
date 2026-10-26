using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Arman.PersistentDataManagement
{
    // NOTE: This voilates the PersistentDataEncoder's interface. Do not use this class.s
    public class PlayerPrefsPersistentDataEncoder : IPersistentDataEncoder
    {
        private StringBuilder blockPath = new StringBuilder();
        private Stack<string> blockStack = new Stack<string>();

        public PlayerPrefsPersistentDataEncoder()
        {
            PushBlock("");
        }

        public void Clear()
        {
            // PlayerPrefs implementation doesn't clear anything.
        }

        public void WriteTo(StreamWriter stream)
        {
            // PlayerPrefs implementation ignores the stream.
        }

        public void ReadFrom(StreamReader stream)
        {
            // PlayerPrefs implementation ignores the stream.
        }

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(ConvertToBlockKey(key));
        }

        public int ReadInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(ConvertToBlockKey(key), defaultValue);
        }

        public IWritablePersistentDataEncoder WriteInt(string key, int value)
        {
            PlayerPrefs.SetInt(ConvertToBlockKey(key), value);
            return this;
        }

        public string ReadString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(ConvertToBlockKey(key), defaultValue);
        }

        public IWritablePersistentDataEncoder WriteString(string key, string value)
        {
            PlayerPrefs.SetString(ConvertToBlockKey(key), value);
            return this;
        }

        public float ReadFloat(string key, float defaultValue = 0f)
        {
            return PlayerPrefs.GetFloat(ConvertToBlockKey(key), defaultValue);
        }

        public IWritablePersistentDataEncoder WriteFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(ConvertToBlockKey(key), value);
            return this;
        }

        public bool ReadBoolean(string key, bool defaultValue = false)
        {
            return PlayerPrefs.GetInt(ConvertToBlockKey(key), defaultValue ? 1 : 0) == 0
                ? false
                : true;
        }

        public IWritablePersistentDataEncoder WriteBoolean(string key, bool value)
        {
            PlayerPrefs.SetInt(ConvertToBlockKey(key), value == false ? 0 : 1);
            return this;
        }

        public IWritablePersistentDataEncoder BeginWritingBlock(string key)
        {
            PushBlock(key);
            return this;
        }

        public IWritablePersistentDataEncoder EndWritingBlock()
        {
            PopBlock();
            return this;
        }

        public void BeginReadingBlock(string key)
        {
            PushBlock(key);
        }

        public void EndReadingBlock()
        {
            PopBlock();
        }

        private void PushBlock(string block)
        {
            blockStack.Push(block);
            blockPath.Append("_");
            blockPath.Append(block);
        }

        private void PopBlock()
        {
            var block = blockStack.Pop();
            // Removing block name
            blockPath.Remove(blockPath.Length - block.Length, block.Length);
            // Removing separator
            blockPath.Remove(blockPath.Length - 1, 1);
        }

        private string ConvertToBlockKey(string key)
        {
            return $"{blockPath.ToString()}_{key}";
        }
    }
}

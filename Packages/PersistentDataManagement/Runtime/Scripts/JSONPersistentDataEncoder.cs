using System.Collections.Generic;
using System.IO;
using NiceJson;

namespace Arman.PersistentDataManagement
{
    public class JSONPersistentDataEncoder : IPersistentDataEncoder
    {
        private JsonObject root;
        private Stack<JsonObject> blockStack;

        public JSONPersistentDataEncoder()
        {
            root = new JsonObject();
            blockStack = new Stack<JsonObject>();
            blockStack.Push(root);
        }

        public void Clear()
        {
            root.Clear();
            blockStack.Clear();
            blockStack.Push(root);
        }

        public void WriteTo(StreamWriter stream)
        {
            stream.Write(root.ToJsonString());
        }

        public void ReadFrom(StreamReader stream)
        {
            var content = stream.ReadToEnd();

            root = string.IsNullOrWhiteSpace(content)
                ? new JsonObject()
                : (JsonObject)JsonNode.ParseJsonString(content);

            blockStack.Push(root);
        }

        public bool HasKey(string key)
        {
            return CurrentBlock().ContainsKey(key);
        }

        public int ReadInt(string key, int defaultValue = 0)
        {
            return HasKey(key) ? (int)CurrentBlock()[key] : defaultValue;
        }

        public IWritablePersistentDataEncoder WriteInt(string key, int value)
        {
            CurrentBlock().Add(key, value);
            return this;
        }

        public string ReadString(string key, string defaultValue = "")
        {
            return HasKey(key) ? (string)CurrentBlock()[key] : defaultValue;
        }

        public IWritablePersistentDataEncoder WriteString(string key, string value)
        {
            CurrentBlock().Add(key, value);
            return this;
        }

        public float ReadFloat(string key, float defaultValue = 0f)
        {
            return HasKey(key) ? (float)CurrentBlock()[key] : defaultValue;
        }

        public IWritablePersistentDataEncoder WriteFloat(string key, float value)
        {
            CurrentBlock().Add(key, value);
            return this;
        }

        public bool ReadBoolean(string key, bool defaultValue = false)
        {
            return HasKey(key) ? (bool)CurrentBlock()[key] : default;
        }

        public IWritablePersistentDataEncoder WriteBoolean(string key, bool value)
        {
            CurrentBlock().Add(key, value);
            return this;
        }

        public IWritablePersistentDataEncoder BeginWritingBlock(string key)
        {
            var block = new JsonObject();
            CurrentBlock().Add(key, block);
            PushBlock(block);
            return this;
        }

        public IWritablePersistentDataEncoder EndWritingBlock()
        {
            PopBlock();
            return this;
        }

        public void BeginReadingBlock(string key)
        {
            var block = CurrentBlock().ContainsKey(key)
                ? (JsonObject)CurrentBlock()[key]
                : new JsonObject();

            PushBlock(block);
        }

        public void EndReadingBlock()
        {
            PopBlock();
        }

        private JsonObject CurrentBlock()
        {
            return blockStack.Peek();
        }

        private void PushBlock(JsonObject jsonObject)
        {
            blockStack.Push(jsonObject);
        }

        private void PopBlock()
        {
            blockStack.Pop();
        }
    }
}

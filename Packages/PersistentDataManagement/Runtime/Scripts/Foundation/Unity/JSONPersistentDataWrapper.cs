

using System.Collections.Generic;
using System.IO;
using Arman.Foundation.Core.PersistentDataManagement;
using NiceJson;

namespace Arman.Foundation.Unity.PersistentDataManagement
{
    public class JSONPersistentDataWrapper : PersistentDataWrapper
    {
        private JsonObject root;
        private Stack<JsonObject> blockStack;

        public JSONPersistentDataWrapper()
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
            root = (JsonObject) JsonNode.ParseJsonString(stream.ReadToEnd());
            blockStack.Push(root);
        }

        public bool HasKey(string key)
        {
            return CurrentBlock().ContainsKey(key);
        }

        public int ReadInt(string key)
        {
            return CurrentBlock()[key];
        }

        public WritablePersistentDataWrapper WriteInt(string key, int value)
        {
            CurrentBlock().Add(key, value);
            return this;
        }

        public string ReadString(string key)
        {
            return CurrentBlock()[key];
        }

        public WritablePersistentDataWrapper WriteString(string key, string value)
        {
            CurrentBlock().Add(key, value);
            return this;
        }

        public float ReadtFloat(string key)
        {
            return CurrentBlock()[key];
        }

        public WritablePersistentDataWrapper WriteFloat(string key, float value)
        {
            CurrentBlock().Add(key, value);
            return this;
        }

        public bool ReadBoolean(string key)
        {
            return CurrentBlock()[key];
        }

        public WritablePersistentDataWrapper WriteBoolean(string key, bool value)
        {
            CurrentBlock().Add(key, value);
            return this;
        }

        public WritablePersistentDataWrapper BeginWritingBlock(string key)
        {
            var block = new JsonObject();
            CurrentBlock().Add(key, block);
            PushBlock(block);
            return this;
        }

        public WritablePersistentDataWrapper EndWritingBlock()
        {
            PopBlock();
            return this;
        }

        public void BeginReadingBlock(string key)
        {
            PushBlock((JsonObject)CurrentBlock()[key]);
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
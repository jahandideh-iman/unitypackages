using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NiceJson;

namespace Arman.PersistentDataManagement
{
    public class JSONPersistentDataEncoder : IPersistentDataEncoder
    {
        private JsonObject _root;
        private Stack<JsonObject> _blockStack;

        public JSONPersistentDataEncoder()
        {
            _root = new JsonObject();
            _blockStack = new Stack<JsonObject>();
            _blockStack.Push(_root);
        }

        public void Clear()
        {
            _root.Clear();
            _blockStack.Clear();
            _blockStack.Push(_root);
        }

        public void WriteTo(StreamWriter stream)
        {
            stream.Write(_root.ToJsonString());
        }

        public void ReadFrom(StreamReader stream)
        {
            var content = stream.ReadToEnd();

            _root = string.IsNullOrWhiteSpace(content)
                ? new JsonObject()
                : (JsonObject)JsonNode.ParseJsonString(content);

            _blockStack.Push(_root);
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
            CurrentBlock().Add(key, ToDecimal(value));
            return this;
        }

        public bool ReadBoolean(string key, bool defaultValue = false)
        {
            return HasKey(key) ? (bool)CurrentBlock()[key] : defaultValue;
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

        // NiceJson writes a decimal with the invariant culture but a float with the device's,
        // which puts a comma decimal separator into the JSON on locales such as de-DE. Reading
        // parses every fractional number back as a decimal either way. The round-trip "R"
        // string keeps every digit the float needs; a direct (decimal) cast keeps only seven.
        private static decimal ToDecimal(float value)
        {
            return decimal.Parse(
                value.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture
            );
        }

        private JsonObject CurrentBlock()
        {
            return _blockStack.Peek();
        }

        private void PushBlock(JsonObject jsonObject)
        {
            _blockStack.Push(jsonObject);
        }

        private void PopBlock()
        {
            _blockStack.Pop();
        }
    }
}

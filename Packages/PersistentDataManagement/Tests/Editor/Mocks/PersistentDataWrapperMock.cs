
using System;
using System.IO;

namespace Arman.PersistentDataManagement.Tests
{
    public class PersistentDataWrapperMock : IPersistentDataWrapper
    {
        public Action<StreamWriter> onWriteAction = delegate { };
        public Action<StreamReader> onReadAction = delegate { };
        public Action onClearAction = delegate { };

        public void Clear()
        {
            onClearAction();
        }


        public void WriteTo(StreamWriter stream)
        {
            onWriteAction(stream);
        }


        public void ReadFrom(StreamReader stream)
        {
            onReadAction(stream);
        }


        public bool HasKey(string key)
        {
            return true;
        }


        public void BeginReadingBlock(string key)
        {
            
        }

        public void EndReadingBlock()
        {
            
        }

        public IWritablePersistentDataWrapper WriteInt(string key, int value)
        {
            return this;
        }

        public IWritablePersistentDataWrapper WriteFloat(string key, float value)
        {
            throw new NotImplementedException();
        }

        public IWritablePersistentDataWrapper WriteBoolean(string key, bool value)
        {
            throw new NotImplementedException();
        }

        public IWritablePersistentDataWrapper WriteString(string key, string value)
        {
            throw new NotImplementedException();
        }

        public IWritablePersistentDataWrapper BeginWritingBlock(string key)
        {
            return this;
        }

        public IWritablePersistentDataWrapper EndWritingBlock()
        {
            return this;
        }

        public int ReadInt(string key, int defaultValue = 0)
        {
            throw new NotImplementedException();
        }

        public float ReadFloat(string key, float defaultValue = 0)
        {
            throw new NotImplementedException();
        }

        public bool ReadBoolean(string key, bool defaultValue = false)
        {
            throw new NotImplementedException();
        }

        public string ReadString(string key, string defaultValue = "")
        {
            throw new NotImplementedException();
        }
    }
}
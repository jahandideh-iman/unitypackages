
using Arman.Foundation.Core.PersistentDataManagement;
using System;
using System.IO;

namespace Arman.Mocks.Foundation.Core.PersistentDataManagement
{
    public class PersistentDataWrapperMock : PersistentDataWrapper
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

        public WritablePersistentDataWrapper WriteInt(string key, int value)
        {
            return this;
        }

        public WritablePersistentDataWrapper WriteFloat(string key, float value)
        {
            throw new NotImplementedException();
        }

        public WritablePersistentDataWrapper WriteBoolean(string key, bool value)
        {
            throw new NotImplementedException();
        }

        public WritablePersistentDataWrapper WriteString(string key, string value)
        {
            throw new NotImplementedException();
        }

        public WritablePersistentDataWrapper BeginWritingBlock(string key)
        {
            return this;
        }

        public WritablePersistentDataWrapper EndWritingBlock()
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
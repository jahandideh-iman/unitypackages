
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
            throw new NotImplementedException();
        }

        public int ReadInt(string key)
        {
            throw new NotImplementedException();
        }

        public float ReadtFloat(string key)
        {
            throw new NotImplementedException();
        }

        public bool ReadBoolean(string key)
        {
            throw new NotImplementedException();
        }

        public string ReadString(string key)
        {
            throw new NotImplementedException();
        }

        public void BeginReadingBlock(string key)
        {
            throw new NotImplementedException();
        }

        public void EndReadingBlock()
        {
            throw new NotImplementedException();
        }

        public WritablePersistentDataWrapper WriteInt(string key, int value)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public WritablePersistentDataWrapper EndWritingBlock()
        {
            throw new NotImplementedException();
        }
    }
}
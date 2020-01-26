
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

        public int LoadInt(string key)
        {
            throw new NotImplementedException();
        }

        public string LoadString(string key)
        {
            throw new NotImplementedException();
        }


        public void SaveInt(string key, int value)
        {
            throw new System.NotImplementedException();
        }

        public void SaveString(string key, string value)
        {
            throw new System.NotImplementedException();
        }

    }
}
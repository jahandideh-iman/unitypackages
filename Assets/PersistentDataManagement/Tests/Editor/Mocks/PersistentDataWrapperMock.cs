
using Arman.Foundation.Core.PersistentDataManagement;
using System;
using System.IO;

namespace Arman.Mocks.Foundation.Core.PersistentDataManagement
{
    public class PersistentDataWrapperMock : PersistentDataWrapper
    {
        public Action<StreamWriter> onWriteAction = delegate { };
        public Action onClearAction = delegate { };

        public void Clear()
        {
            onClearAction();
        }

        public void SaveInt(string key, int value)
        {
            throw new System.NotImplementedException();
        }

        public void SaveString(string key, string value)
        {
            throw new System.NotImplementedException();
        }

        public void WriteTo(StreamWriter writer)
        {
            onWriteAction(writer);
        }
    }
}
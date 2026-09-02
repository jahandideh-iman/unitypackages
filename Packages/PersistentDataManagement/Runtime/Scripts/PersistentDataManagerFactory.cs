using UnityEngine;

namespace Arman.PersistentDataManagement
{
    /// <summary>
    /// Builds a ready-to-use <see cref="IPersistentDataManager"/> backed by JSON files
    /// under <see cref="Application.persistentDataPath"/>.
    /// </summary>
    public static class PersistentDataManagerFactory
    {
        public static IPersistentDataManager Create()
        {
            var manager = new BasicPersistentDataManager();
            manager.SetPersistentDataIOStreamFactory(new FileBasedPersistetDataIOStreamFactory(Application.persistentDataPath));
            manager.SetPersistentDataWrapper(new JSONPersistentDataWrapper());
            return manager;
        }
    }
}

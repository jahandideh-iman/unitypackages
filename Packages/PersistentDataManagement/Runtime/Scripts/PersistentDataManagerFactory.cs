using UnityEngine;

namespace Arman.PersistentDataManagement
{
    /// <summary>
    /// Builds a ready-to-use <see cref="IPersistentDataManager"/> backed by JSON files
    /// under <see cref="Application.persistentDataPath"/>.
    /// </summary>
    public static class PersistentDataManagerFactory
    {
        public static IPersistentDataManager CreateDefault(int saveVersion = 0)
        {
            return new PersistentDataManager(
                new FileBasedPersistetDataIOStreamFactory(Application.persistentDataPath),
                new JSONPersistentDataWrapper(),
                saveVersion);
        }
    }
}

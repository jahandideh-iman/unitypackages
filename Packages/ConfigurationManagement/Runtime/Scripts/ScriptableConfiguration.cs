using UnityEngine;

namespace Arman.ConfigurationManagement
{
    public abstract class ScriptableConfiguration : ScriptableObject, IConfigurer
    {
        public abstract void RegisterSelf(IConfigurationManager manager);


        // TODO: Move this to a better place.
        public T[] ShallowCopy<T>(T[] array) where T : ScriptableObject
        {
            var coppies = new T[array.Length];

            for (int i = 0; i < array.Length; i++)
                coppies[i] = Instantiate(array[i]);

            return coppies;
        }
    }
}
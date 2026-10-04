using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arman.UIManagement.Tests
{
    public static class UnityComponentCreationExtensions
    {
        public static GameObject CreateGameObject(
            string name = "",
            Action<GameObject>? action = null
        )
        {
            var gameObject = new GameObject(name);
            action?.Invoke(gameObject);
            return gameObject;
        }

        public static GameObject Child(
            this GameObject parent,
            string name = "",
            Action<GameObject>? action = null
        )
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent.transform, false);
            action?.Invoke(gameObject);
            return gameObject;
        }

        public static T AddComponent<T>(this GameObject gameObject, Action<T> preAwake)
            where T : Component
        {
            gameObject.SetActive(false);
            var component = gameObject.AddComponent<T>();
            preAwake.Invoke(component);
            gameObject.SetActive(true);
            return component;
        }

        public static T Out<T>(this T component, out T output)
            where T : Component
        {
            output = component;
            return component;
        }

        public static void CleanUpScene()
        {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (
                    go.GetComponents<MonoBehaviour>()
                        .Any(component => component.GetType().Name == "PlaymodeTestsController")
                )
                {
                    continue;
                }
                GameObject.Destroy(go);
            }
        }
    }
}

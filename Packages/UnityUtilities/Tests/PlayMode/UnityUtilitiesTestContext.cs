using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Arman.UnityUtilities.Tests
{
    // Creates the objects a test needs, and destroys them after the test.
    public class UnityUtilitiesTestContext
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void Teardown()
        {
            foreach (var obj in _created)
                if (obj != null)
                    Object.Destroy(obj);

            _created.Clear();
        }

        protected T CreateComponent<T>(string name)
            where T : Component
        {
            return Track(new GameObject(name)).AddComponent<T>();
        }

        protected T Track<T>(T obj)
            where T : Object
        {
            _created.Add(obj);
            return obj;
        }
    }
}

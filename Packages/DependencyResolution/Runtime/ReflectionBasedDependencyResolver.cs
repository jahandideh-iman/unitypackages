using System;
using System.Collections.Generic;
using System.Linq;

namespace Arman.DependencyResolution
{
    public partial class ReflectionBasedDependencyResolver : IDependencyResolver
    {
        private Dictionary<Type, IInternalEntry> _entries = new();

        public IResolutionEntry<T> RegisterType<T>()
        {
            return AddEntry<T>(Entry<T>.FromType());
        }

        public IResolutionEntry<T> RegisterFactory<T>(Delegate factory)
        {
            return AddEntry<T>(Entry<T>.FromFactory(factory));
        }

        public IResolutionEntry<T> RegisterInstance<T>(T instance)
        {
            return AddEntry<T>(Entry<T>.FromInstance(instance));
        }

        private Entry<T> AddEntry<T>(Entry<T> entry)
        {
            _entries[typeof(T)] = entry;
            return entry;
        }

        public IRepository Build()
        {
            var orderedEntries = TopologicalSort(_entries.Values);

            var rollingInstances = new Dictionary<Type, object>();

            foreach (var entry in orderedEntries)
            {
                var instance = entry.Build(rollingInstances);
                foreach (var type in entry.Targets())
                {
                    rollingInstances.Add(type, instance);
                }
            }

            return new DictionaryBasedRepository(rollingInstances);
        }

        private static IEnumerable<IInternalEntry> TopologicalSort(
            IEnumerable<IInternalEntry> entries
        )
        {
            var providers = new Dictionary<Type, IInternalEntry>();

            foreach (var entry in entries)
            {
                foreach (var target in entry.Targets())
                {
                    if (providers.ContainsKey(target))
                    {
                        throw new InvalidOperationException(
                            $"Type {target.Name} is provided by more than one entry"
                        );
                    }

                    providers.Add(target, entry);
                }
            }

            var sortedList = new List<IInternalEntry>();
            var visited = new HashSet<IInternalEntry>();
            var visiting = new HashSet<IInternalEntry>();

            foreach (var entry in entries)
            {
                Visit(entry);
            }

            return sortedList;

            void Visit(IInternalEntry entry)
            {
                if (visiting.Contains(entry))
                {
                    throw new InvalidOperationException(
                        $"Circular dependency detected involving types: {string.Join(", ", entry.Targets().Select(target => target.Name))}"
                    );
                }

                if (visited.Contains(entry))
                {
                    return;
                }

                visiting.Add(entry);

                foreach (var dependency in entry.Dependencies())
                {
                    if (!providers.TryGetValue(dependency, out var provider))
                    {
                        throw new InvalidOperationException(
                            $"No entry provides {dependency.Name}, required by {string.Join(", ", entry.Targets().Select(target => target.Name))}"
                        );
                    }

                    Visit(provider);
                }

                visiting.Remove(entry);
                visited.Add(entry);
                sortedList.Add(entry);
            }
        }
    }
}

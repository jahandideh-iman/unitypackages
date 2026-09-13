using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;


namespace Arman.DependencyResolution
{
    public partial class ReflectionBasedDependencyResolver
    {
        private interface IInternalEntry
        {
            object Build(IReadOnlyDictionary<Type, object> resolved);

            HashSet<Type> Dependencies();

            HashSet<Type> Targets();
        }
        private class Entry<T> : IResolutionEntry<T>, IInternalEntry
        {
            private static readonly HashSet<Type> _emptyHashset = new();

            private Func<IReadOnlyDictionary<Type, object>, T> _factory;
            private HashSet<Type> _dependencies;
            private HashSet<Type> _targets = new();

            private Entry(Func<IReadOnlyDictionary<Type, object>, T> factory, HashSet<Type> dependencies)
            {

                _factory = factory;
                _dependencies = dependencies;
                _targets.Add(typeof(T));
            }


            public object Build(IReadOnlyDictionary<Type, object> resolved)
            {
                return _factory.Invoke(resolved)!;
            }

            public HashSet<Type> Dependencies()
            {
                return _dependencies;
            }

            public HashSet<Type> Targets()
            {
                return _targets;
            }


            public IResolutionEntry<T> As<U>()
            {
                if (!typeof(U).IsAssignableFrom(typeof(T)))
                {
                    throw new ArgumentException($"Can not register type {typeof(T)} to {typeof(U)}");
                }
                _targets.Add(typeof(U));
                return this;
            }

            internal static Entry<T> FromInstance(T instance)
            {
                return new Entry<T>(Factory, dependencies: _emptyHashset);

                T Factory(IReadOnlyDictionary<Type, object> resolved)
                {
                    return instance;
                }
            }

            internal static Entry<T> FromFactory(Delegate factory)
            {
                var method = factory.Method;
                if (!typeof(T).IsAssignableFrom(method.ReturnType))
                {
                    throw new ArgumentException($"Factory {method.Name} returns {method.ReturnType}, which is not assignable to {typeof(T)}", nameof(factory));
                }

                var parameterTypes = method.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToArray();

                return new Entry<T>(Factory, dependencies: new HashSet<Type>(parameterTypes));

                T Factory(IReadOnlyDictionary<Type, object> resolved)
                {
                    var arguments = parameterTypes.Select(type => resolved[type]).ToArray();

                    try
                    {
                        return (T)factory.DynamicInvoke(arguments)!;
                    }
                    catch (TargetInvocationException exception) when (exception.InnerException != null)
                    {
                        // Surface the factory's own exception instead of the reflection wrapper
                        ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                        throw;
                    }
                }
            }

            internal static Entry<T> FromType()
            {
                var constructor = typeof(T).GetConstructors().First();

                var parameterTypes = constructor.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToArray();

                return new Entry<T>(Factory, dependencies: new HashSet<Type>(parameterTypes));

                T Factory(IReadOnlyDictionary<Type, object> resolved)
                {
                    var arguments = parameterTypes.Select(type => resolved[type]).ToArray();

                    try
                    {
                        return (T)constructor.Invoke(arguments)!;
                    }
                    catch (TargetInvocationException exception) when (exception.InnerException != null)
                    {
                        // Surface the factory's own exception instead of the reflection wrapper
                        ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                        throw;
                    }
                }
            }

        }

    }
}

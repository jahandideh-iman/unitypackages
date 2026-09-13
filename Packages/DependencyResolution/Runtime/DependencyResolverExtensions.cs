using System;


namespace Arman.DependencyResolution
{
    public static class DependencyResolverExtensions
    {
        public static IResolutionEntry<TResult> RegisterFactory<TResult>(this IDependencyResolver dependencyResolver, Func<TResult> factory)
        {
            return dependencyResolver.RegisterFactory<TResult>(factory);
        }

        public static IResolutionEntry<TResult> RegisterFactory<TResult, T1>(this IDependencyResolver dependencyResolver, Func<T1, TResult> factory)
        {
            return dependencyResolver.RegisterFactory<TResult>(factory);
        }

        public static IResolutionEntry<TResult> RegisterFactory<TResult, T1, T2>(this IDependencyResolver dependencyResolver, Func<T1, T2, TResult> factory)
        {
            return dependencyResolver.RegisterFactory<TResult>(factory);
        }

        public static IResolutionEntry<TResult> RegisterFactory<TResult, T1, T2, T3>(this IDependencyResolver dependencyResolver, Func<T1, T2, T3, TResult> factory)
        {
            return dependencyResolver.RegisterFactory<TResult>(factory);
        }

        public static IResolutionEntry<TResult> RegisterFactory<TResult, T1, T2, T3, T4>(this IDependencyResolver dependencyResolver, Func<T1, T2, T3, T4, TResult> factory)
        {
            return dependencyResolver.RegisterFactory<TResult>(factory);
        }

        public static IResolutionEntry<TResult> RegisterFactory<TResult, T1, T2, T3, T4, T5>(this IDependencyResolver dependencyResolver, Func<T1, T2, T3, T4, T5, TResult> factory)
        {
            return dependencyResolver.RegisterFactory<TResult>(factory);
        }

        public static IResolutionEntry<TResult> RegisterFactory<TResult, T1, T2, T3, T4, T5, T6>(this IDependencyResolver dependencyResolver, Func<T1, T2, T3, T4, T5, T6, TResult> factory)
        {
            return dependencyResolver.RegisterFactory<TResult>(factory);
        }

        public static IResolutionEntry<TResult> RegisterFactory<TResult, T1, T2, T3, T4, T5, T6, T7>(this IDependencyResolver dependencyResolver, Func<T1, T2, T3, T4, T5, T6, T7, TResult> factory)
        {
            return dependencyResolver.RegisterFactory<TResult>(factory);
        }

        public static IResolutionEntry<TResult> RegisterFactory<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(this IDependencyResolver dependencyResolver, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> factory)
        {
            return dependencyResolver.RegisterFactory<TResult>(factory);
        }

        public static IResolutionEntry<TResult> RegisterFactory<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IDependencyResolver dependencyResolver, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> factory)
        {
            return dependencyResolver.RegisterFactory<TResult>(factory);
        }
    }
}

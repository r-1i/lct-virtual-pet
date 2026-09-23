using System;
using System.Collections.Generic;

namespace Core
{
    /// <summary>Tiny registry for the game's core services. Bootstrap registers everything in Awake; everyone else fetches in Start (Unity runs every Awake before any Start, so Start is always safe here).</summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        public static void Register<T>(T instance) where T : class
        {
            Services[typeof(T)] = instance;
        }

        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out object instance))
            {
                return (T)instance;
            }

            throw new InvalidOperationException(
                $"ServiceLocator: {typeof(T).Name} is not registered. Fetch services in Start(), not Awake(), and make sure Bootstrap is in the scene.");
        }

        public static bool TryGet<T>(out T instance) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object raw))
            {
                instance = (T)raw;
                return true;
            }

            instance = null;
            return false;
        }

        /// <summary>Clears all registrations. Bootstrap calls this first so re-entering Play Mode never reuses services from a previous run.</summary>
        public static void Clear()
        {
            Services.Clear();
        }
    }
}

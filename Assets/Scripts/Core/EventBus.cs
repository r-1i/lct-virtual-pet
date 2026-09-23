using System;
using System.Collections.Generic;

namespace Core
{
    /// <summary>Static pub/sub for state that several independent widgets across different zones need to react to (coins, stats, job state, inventory). Subscribe in OnEnable, unsubscribe in OnDisable.</summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> Subscribers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler)
        {
            Type type = typeof(T);
            Subscribers[type] = Subscribers.TryGetValue(type, out Delegate existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            Type type = typeof(T);
            if (!Subscribers.TryGetValue(type, out Delegate existing))
            {
                return;
            }

            Delegate remaining = Delegate.Remove(existing, handler);
            if (remaining == null)
            {
                Subscribers.Remove(type);
            }
            else
            {
                Subscribers[type] = remaining;
            }
        }

        public static void Publish<T>(T eventData)
        {
            if (Subscribers.TryGetValue(typeof(T), out Delegate existing))
            {
                ((Action<T>)existing)?.Invoke(eventData);
            }
        }

        /// <summary>Clears all subscriptions. Bootstrap calls this first so re-entering Play Mode never keeps handlers from a previous run.</summary>
        public static void Clear()
        {
            Subscribers.Clear();
        }
    }
}

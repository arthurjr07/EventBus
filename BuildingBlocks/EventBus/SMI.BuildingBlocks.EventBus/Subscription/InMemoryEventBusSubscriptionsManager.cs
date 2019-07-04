using Google.Protobuf;
using MediatR;
using SIM.BuildingBlocks.EventBus.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMI.BuildingBlocks.EventBus.Subscription
{
    /// <summary>
    /// The in memory implementation of subscription manager. keeps all subscription information only
    /// in runtime.
    /// </summary>
    public class InMemoryEventBusSubscriptionsManager : IEventBusSubscriptionsManager
    {
        private readonly List<Type> _eventTypes;
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;

        /// <summary>
        /// The field handler for removal of event.
        /// </summary>
        public event EventHandler<string> OnEventRemoved;

        /// <summary>
        /// The indicator whether the manager has empty subscriptions.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return !_handlers.Keys.Any();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryEventBusSubscriptionsManager"/> class.
        /// </summary>
        public InMemoryEventBusSubscriptionsManager()
        {
            _handlers = new Dictionary<string, List<SubscriptionInfo>>();
            _eventTypes = new List<Type>();
        }

        /// <summary>
        /// the method for adding more event subscriptions.
        /// </summary>
        /// <typeparam name="T">The event.</typeparam>
        /// <typeparam name="Th">The event handler.</typeparam>
        public void AddSubscription<T, Th>()
            where T : IMessage
            where Th : IEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            DoAddSubscription(typeof(Th), eventName);
            _eventTypes.Add(typeof(T));
        }

        /// <summary>
        /// The method for clearing all subscription.
        /// </summary>
        public void Clear()
        {
            _handlers.Clear();
        }

        /// <summary>
        /// The method for retrieving the key for an event type.
        /// </summary>
        /// <typeparam name="T">The type of the event you would like to inquire.</typeparam>
        /// <returns>The string key of the event subscription, if it exists.</returns>
        public string GetEventKey<T>()
        {
            return typeof(T).Name;
        }

        /// <summary>
        /// The retrieving method for the type of event in registry using a string name.
        /// </summary>
        /// <param name="eventName">The string event name.</param>
        /// <returns>The type of the event.</returns>
        public Type GetEventTypeByName(string eventName)
        {
            return _eventTypes.SingleOrDefault(t => t.Name == eventName);
        }

        /// <summary>
        /// The method for retirieving the collection of subscription.
        /// </summary>
        /// <typeparam name="T">
        /// The type of event that would be the query parameter for filtering subscriptions.
        /// </typeparam>
        /// <returns>The collection of the subscription information.</returns>
        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IMessage
        {
            var key = GetEventKey<T>();
            return GetHandlersForEvent(key);
        }

        /// <summary>
        /// The method for retrieving the handlers for the event.
        /// </summary>
        /// <param name="eventName">The string key for the event being retrieved.</param>
        /// <returns>The collection of the subscription information</returns>
        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName)
        {
            if(!_handlers.Any())
            {
                return null;
            }
            return _handlers[eventName];
        }

        /// <summary>
        /// The method for querying whether or not the subscription manager has subscription for an event.
        /// </summary>
        /// <typeparam name="T">The type of the event being subscribed unto.</typeparam>
        /// <returns>The indicator whether the manager has subscription for the event.</returns>
        public bool HasSubscriptionsForEvent<T>() where T : IMessage
        {
            var key = GetEventKey<T>();
            return HasSubscriptionsForEvent(key);
        }

        /// <summary>
        /// The method for querying whther or not the subscription manager has subscription for an event.
        /// </summary>
        /// <param name="eventName">The string name of the event being queried.</param>
        /// <returns>The indicator of existence of the subscription in the manager.</returns>
        public bool HasSubscriptionsForEvent(string eventName)
        {
            return _handlers.ContainsKey(eventName);
        }

        /// <summary>
        /// The method for removing the subscription in the manager.
        /// </summary>
        /// <typeparam name="T">The type of the event.</typeparam>
        /// <typeparam name="Th">The event handler.</typeparam>
        public void RemoveSubscription<T, Th>()
            where Th : IEventHandler<T>
            where T : IMessage
        {
            var handlerToRemove = FindSubscriptionToRemove<T, Th>();
            var eventName = GetEventKey<T>();
            DoRemoveHandler(eventName, handlerToRemove);
        }

        /// <summary>
        /// The specific implementation for adding subscription for in memory manager.
        /// </summary>
        /// <param name="handlerType">The type of event handler.</param>
        /// <param name="eventName">The name of event.</param>
        private void DoAddSubscription(Type handlerType, string eventName)
        {
            if (!HasSubscriptionsForEvent(eventName))
            {
                _handlers.Add(eventName, new List<SubscriptionInfo>());
            }

            if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
            {
                throw new ArgumentException(
                    $"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
            }

            _handlers[eventName].Add(new SubscriptionInfo(handlerType));
        }

        /// <summary>
        /// the method for finding subscription to removed based on event name and type of the handler.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="handlerType"></param>
        /// <returns></returns>
        private SubscriptionInfo DoFindSubscriptionToRemove(string eventName, Type handlerType)
        {
            if (!HasSubscriptionsForEvent(eventName))
            {
                return null;
            }

            return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);
        }

        /// <summary>
        /// The specific implementation of hte removing of subscription information for the event.
        /// </summary>
        /// <param name="eventName">The event name.</param>
        /// <param name="subsToRemove">The subscription to remove.</param>
        private void DoRemoveHandler(string eventName, SubscriptionInfo subsToRemove)
        {
            if (subsToRemove != null)
            {
                _handlers[eventName].Remove(subsToRemove);
                if (!_handlers[eventName].Any())
                {
                    _handlers.Remove(eventName);
                    var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
                    if (eventType != null)
                    {
                        _eventTypes.Remove(eventType);
                    }
                    RaiseOnEventRemoved(eventName);
                }
            }
        }

        /// <summary>
        /// The method for retrieiving the subscription.
        /// </summary>
        /// <typeparam name="T">The event.</typeparam>
        /// <typeparam name="TH">The event handler.</typeparam>
        /// <returns></returns>
        private SubscriptionInfo FindSubscriptionToRemove<T, TH>()
             where T : IMessage
             where TH : IEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            return DoFindSubscriptionToRemove(eventName, typeof(TH));
        }

        /// <summary>
        /// The method implementation of removing hte event based on the event name.
        /// </summary>
        /// <param name="eventName">The event name.</param>
        private void RaiseOnEventRemoved(string eventName)
        {
            OnEventRemoved?.Invoke(this, eventName);
        }
    }
}

using Google.Protobuf;
using MediatR;
using SIM.BuildingBlocks.EventBus.Interfaces;
using System;
using System.Collections.Generic;

namespace SMI.BuildingBlocks.EventBus.Subscription
{
    /// <summary>
    /// The interface for event bus subscription manager.
    /// </summary>
    public interface IEventBusSubscriptionsManager
    {
        /// <summary>
        /// The handler whenever an event is removed.
        /// </summary>
        event EventHandler<string> OnEventRemoved;

        /// <summary>
        /// The property helper for Empty identitfication.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// The method for adding a subscription.
        /// </summary>
        /// <typeparam name="T">The event being subscribed into.</typeparam>
        /// <typeparam name="TH">The handler for the event.</typeparam>
        void AddSubscription<T, TH>()
           where T : IMessage
           where TH : IEventHandler<T>;

        /// <summary>
        /// the method for clearing all subscription.
        /// </summary>
        void Clear();

        /// <summary>
        /// The method for retrieving the key for an event type.
        /// </summary>
        /// <typeparam name="T">The type of the event you would like to inquire.</typeparam>
        /// <returns>The string key of the event subscription, if it exists.</returns>
        string GetEventKey<T>();

        /// <summary>
        /// The retrieving method for the type of event in registry using a string name.
        /// </summary>
        /// <param name="eventName">The string event name.</param>
        /// <returns>The type of the event.</returns>
        Type GetEventTypeByName(string eventName);

        /// <summary>
        /// The method for retirieving the collection of subscription.
        /// </summary>
        /// <typeparam name="T">
        /// The type of event that would be the query parameter for filtering subscriptions.
        /// </typeparam>
        /// <returns>The collection of the subscription information.</returns>
        IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IMessage;

        /// <summary>
        /// The method for retrieving the handlers for the event.
        /// </summary>
        /// <param name="eventName">The string key for the event being retrieved.</param>
        /// <returns>The collection of the subscription information</returns>
        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);

        /// <summary>
        /// The method for querying whether or not the subscription manager has subscription for an event.
        /// </summary>
        /// <typeparam name="T">The type of the event being subscribed unto.</typeparam>
        /// <returns>The indicator whether the manager has subscription for the event.</returns>
        bool HasSubscriptionsForEvent<T>() where T : IMessage;

        /// <summary>
        /// The method for querying whther or not the subscription manager has subscription for an event.
        /// </summary>
        /// <param name="eventName">The string name of the event being queried.</param>
        /// <returns>The indicator of existence of the subscription in the manager.</returns>
        bool HasSubscriptionsForEvent(string eventName);

        /// <summary>
        /// The method for removing the subscription in the manager.
        /// </summary>
        /// <typeparam name="T">The type of the event.</typeparam>
        /// <typeparam name="TH">The event handler.</typeparam>
        void RemoveSubscription<T, TH>()
             where TH : IEventHandler<T>
             where T : IMessage;
    }
}

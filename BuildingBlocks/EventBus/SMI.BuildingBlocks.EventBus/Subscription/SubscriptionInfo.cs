using System;

namespace SMI.BuildingBlocks.EventBus.Subscription
{
    /// <summary>
    /// The subscription information object.
    /// </summary>
    public class SubscriptionInfo
    {
        /// <summary>
        /// The handler for the subscription.
        /// </summary>
        public Type HandlerType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionInfo"/> class. used for
        /// subscription representation in inmemory implementation of subscription manager.
        /// </summary>
        /// <param name="handlerType">The type of the handler.</param>
        public SubscriptionInfo(Type handlerType)
        {
            HandlerType = handlerType;
        }
    }
}

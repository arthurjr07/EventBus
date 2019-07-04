using System;
using System.Collections.Generic;
using System.Text;

namespace SMI.BuildingBlocks.EventBus.Config
{
    /// <summary>
    /// The class that shows the configuration for the event bus.
    /// </summary>
    public class EventBusConfig
    {
        private readonly string defaultQueueName = "SMI_EVENTS_QUEUE";

        /// <summary>
        /// The event bus connection.
        /// </summary>
        public string EventBusConnection { get; set; }

        /// <summary>
        /// The event bus user name.
        /// </summary>
        public string EventBusUserName { get; set;}

        /// <summary>
        /// The event bus password.
        /// </summary>
        public string EventBusPassword { get; set; }

        /// <summary>
        /// The event bus retrycount.
        /// </summary>
        public string EventBusRetryCount { get; set; }

        /// <summary>
        /// The EventBusQueueName
        /// </summary>
        public string EventBusQueueName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventBusConfig"/> class.
        /// </summary>
        public EventBusConfig()
        {
            EventBusQueueName = defaultQueueName;
        }
    }
}

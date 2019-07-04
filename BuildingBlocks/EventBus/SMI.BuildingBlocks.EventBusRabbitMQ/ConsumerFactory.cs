using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SMI.BuildingBlocks.EventBusRabbitMQ
{
    /// <summary>
    /// The class that creates various types of RabbitMQConsumers.
    /// </summary>
    public class ConsumerFactory : IConsumerFactory
    {
        /// <summary>
        /// The method for retrieving basic consumer.
        /// </summary>
        /// <param name="model">The model for the consumer.</param>
        /// <returns>The event basic consumer instance.</returns>
        public EventingBasicConsumer CreateEventingBasicConsumer(IModel model)
        {
            return new EventingBasicConsumer(model);
        }
    }
}

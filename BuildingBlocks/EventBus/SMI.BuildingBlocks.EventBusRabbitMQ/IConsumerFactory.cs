using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SMI.BuildingBlocks.EventBusRabbitMQ
{
    /// <summary>
    /// The interface for consumer factory.
    /// </summary>
    public interface IConsumerFactory
    {
        /// <summary>
        /// The method for creating a basic consumer.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>The instance created.</returns>
        EventingBasicConsumer CreateEventingBasicConsumer(IModel model);
    }
}

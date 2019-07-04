using RabbitMQ.Client;
using System;

namespace SMI.BuildingBlocks.EventBusRabbitMQ
{
    /// <summary>
    /// The interface for the rabbit mq connection.
    /// </summary>
    public interface IRabbitMQPersistentConnection
         : IDisposable
    {
        /// <summary>
        /// Indicator for connecetivity.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// The method for creting connection.
        /// </summary>
        /// <returns></returns>
        IModel CreateModel();

        /// <summary>
        /// The method for trying to connect.
        /// </summary>
        /// <returns></returns>
        bool TryConnect();
    }
}

using Google.Protobuf;
using MediatR;
using SIM.BuildingBlocks.EventBus.Interfaces;

namespace SMI.BuildingBlocks.EventBus.Interfaces
{
    /// <summary>
    /// The interface for event bus.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// The method for publishing the message.
        /// </summary>
        /// <param name="event">The message being published.</param>
        void Publish(IMessage @event);

        /// <summary>
        /// The subscription method for the event bus.
        /// </summary>
        /// <typeparam name="T">The message being subscribed to.</typeparam>
        /// <typeparam name="TH">The handler for the message.</typeparam>
        void Subscribe<T, TH>()
            where T : IMessage
            where TH : IEventHandler<T>;

        /// <summary>
        /// The unsubscribe method for the event bus.
        /// </summary>
        /// <typeparam name="T">The message being unsubscribed.</typeparam>
        /// <typeparam name="TH">The handler for the message.</typeparam>
        void Unsubscribe<T, TH>()
            where T : IMessage
            where TH : IEventHandler<T>;
    }
}

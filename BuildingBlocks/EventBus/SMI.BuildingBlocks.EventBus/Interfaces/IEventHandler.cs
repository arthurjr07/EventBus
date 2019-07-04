using Google.Protobuf;
using MediatR;
using SMI.BuildingBlocks.EventBus.Interfaces;
using System.Threading.Tasks;

namespace SIM.BuildingBlocks.EventBus.Interfaces
{
    /// <summary>
    /// The generic event handler interface.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event being subscribed to.</typeparam>
    public interface IEventHandler<in TEvent> where TEvent : IMessage
    {
        Task HandleAsync(byte[] data);
    }
}

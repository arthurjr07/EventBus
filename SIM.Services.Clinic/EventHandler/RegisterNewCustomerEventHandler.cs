using SIM.BuildingBlocks.EventBus.Interfaces;
using SIM.BuildingBlocks.EventBus.Messages;
using SMI.BuildingBlocks.EventBus.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SIM.Services.Clinic.EventHandlers
{
    /// <summary>
    /// Handler for CreateUser
    /// </summary>
    public class RegisterNewCustomerEventHandler : IEventHandler<RegisterNewCustomerEvent>, IRegistrationHandler
    {
        public RegisterNewCustomerEventHandler()
        {
        }

        public  Task Handle(RegisterNewCustomerEvent notification, CancellationToken cancellationToken)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// The method for handling CreateUserEvent
        /// </summary>
        /// <param name="data">The data in bytes</param>
        /// <returns>Returns <see cref="Task"/></returns>
        public async Task HandleAsync(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var message = RegisterNewCustomerEvent.Parser.ParseFrom(data);

            await Handle(message, default).ConfigureAwait(false);
        }
    }
}

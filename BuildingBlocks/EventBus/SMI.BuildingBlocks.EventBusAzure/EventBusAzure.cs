using Google.Protobuf;
using MediatR;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIM.BuildingBlocks.EventBus.Interfaces;
using SMI.BuildingBlocks.EventBus.Config;
using SMI.BuildingBlocks.EventBus.Interfaces;
using SMI.BuildingBlocks.EventBus.Subscription;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMI.BuildingBlocks.EventBusAzure
{
    public class EventBusAzure : IEventBus
    {
        private const string ServiceBusConnectionString = "Endpoint=sb://sbtesting01.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=119tMjf3y9GZieyG1XIJnWvlgLTOBp2wLSQX73PbGbM=";
        private const string QueueName = "smi_events_queue";

        private readonly IEnumerable<IRegistrationHandler> _handler;

        private readonly IQueueClient queueClient;
        private readonly ILogger<EventBusAzure> _logger;
        private readonly IOptions<EventBusConfig> _configuration;
        private readonly IEventBusSubscriptionsManager _subsManager;

        public EventBusAzure(IOptions<EventBusConfig> configuration,
                              ILogger<EventBusAzure> logger,
                              IEventBusSubscriptionsManager subsManager,
                              IEnumerable<IRegistrationHandler> handler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _subsManager = subsManager ?? throw new ArgumentNullException(nameof(subsManager));
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            queueClient = CreateQueueClient();
        }

        public void Publish(IMessage @event)
        {
            var eventName = @event.GetType().Name;
            var body = @event.ToByteArray();

            var message = new Message(body);
            message.MessageId = eventName;

            // Send the message to the queue
            queueClient.SendAsync(message).GetAwaiter().GetResult();
        }

        public void Subscribe<T, TH>()
            where T : IMessage
            where TH : IEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();
            _subsManager.AddSubscription<T, TH>();
        }

        public void Unsubscribe<T, TH>()
            where T : IMessage
            where TH : IEventHandler<T>
        {
            throw new NotImplementedException();
        }

        private  QueueClient CreateQueueClient()
        {
            var queueClient = new QueueClient(ServiceBusConnectionString, QueueName);

            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };
            queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);

            return queueClient;
        }

        private async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            var isHandled = false;
            var eventName = message.MessageId;

            var subscriptions = _subsManager.GetHandlersForEvent(eventName);
            if (subscriptions == null || !subscriptions.Any())
            {
                return;
            }

            foreach (var subscription in subscriptions)
            {
                var eventType = subscription.HandlerType;
                var messageType = _subsManager.GetEventTypeByName(eventName);
                var integrationEvent = message.Body;
                var handler = _handler.FirstOrDefault(c => c.GetType() == eventType);
                var concreteType = typeof(IEventHandler<>).MakeGenericType(messageType);
                var methodTask = concreteType.GetMethod("HandleAsync");
                if (methodTask == null)
                {
                    continue;
                }
                isHandled = await InvokeMethodAsync(integrationEvent, handler, methodTask).ConfigureAwait(false);
            }

            if (isHandled)
            {
                await queueClient.CompleteAsync(message.SystemProperties.LockToken);
            }
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs arg)
        {
            _logger.LogError(arg.Exception, arg.Exception.Message);
            return Task.CompletedTask;
        }

        private async Task<bool> InvokeMethodAsync(byte[] integrationEvent, object handler, MethodBase methodTask)
        {
            var result = false;
            try
            {
                result = true;
                await ((Task)methodTask
                        .Invoke(handler, BindingFlags.Default, null, new object[] { integrationEvent }, CultureInfo.CurrentCulture))
                    .ConfigureAwait(false);
            }
            catch (TargetException ex)
            {
                result = false;
                _logger.LogError(ex, ex.Message);
            }

            return result;
        }
    }
}

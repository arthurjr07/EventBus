using Autofac;
using Google.Protobuf;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using SIM.BuildingBlocks.EventBus.Interfaces;
using SMI.BuildingBlocks.EventBus.Config;
using SMI.BuildingBlocks.EventBus.Interfaces;
using SMI.BuildingBlocks.EventBus.Subscription;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

namespace SMI.BuildingBlocks.EventBusRabbitMQ
{
    /// <summary>
    /// The event bus by rabbit message queue.
    /// </summary>
    public class EventBusRabbitMQ : IEventBus
    {
        private const string AUTOFAC_SCOPE_NAME = "cim_event_bus";
        private const string BROKER_NAME = "cim_event_bus";
        private const byte DeliveryModePersistent = 2;
        private readonly ILifetimeScope _autofac;
        private readonly ILogger<EventBusRabbitMQ> _logger;
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly IOptions<EventBusConfig> _configuration;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private IModel _consumerChannel;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildingBlocks.EventBusRabbitMQ"/> class.
        /// </summary>
        /// <param name="persistentConnection">The persistent connection for the event bus.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="autofac">The current lifetime scope.</param>
        /// <param name="subsManager">The submanager.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="queueName">The queue name.</param>
        /// <param name="retryCount">The retry count configured.</param>
        public EventBusRabbitMQ(IRabbitMQPersistentConnection persistentConnection,
                                ILogger<EventBusRabbitMQ> logger,
                                ILifetimeScope autofac,
                                IEventBusSubscriptionsManager subsManager,
                                IOptions<EventBusConfig> configuration)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _autofac = autofac ?? throw new ArgumentNullException(nameof(autofac));
            _consumerChannel = CreateConsumerChannel();
        }

        /// <summary>
        /// The executed event when a message is received.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="callback">the callback.</param>
        public static void EventReceived(object model, BasicDeliverEventArgs args, Action<byte[]> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            callback.Invoke(args.Body);
        }

        /// <summary>
        /// The executed event when a message is received.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The task for the asynchronous event.</returns>
        public async Task EventReceivedAsync(object model, BasicDeliverEventArgs args, IModel channel)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            var eventName = args.RoutingKey;

            var handled = await ProcessEventAsync(eventName, args.Body).ConfigureAwait(false);

            if (handled)
            {
                channel.BasicAck(args.DeliveryTag, false);
            }
            else
            {
                channel.BasicNack(args.DeliveryTag, false, true);
            }
        }

        /// <summary>
        /// The publish method.
        /// </summary>
        /// <param name="event">The event message.</param>
        public void Publish(IMessage @event)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            var policy = RetryPolicy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(int.Parse(_configuration.Value.EventBusRetryCount, CultureInfo.InvariantCulture), retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                 {
                     _logger.LogWarning(ex.ToString());
                 });

            using (var channel = _persistentConnection.CreateModel())
            {
                var eventName = @event.GetType().Name;

                channel.ExchangeDeclare(BROKER_NAME, "direct");

                var body = @event.ToByteArray();

                policy.Execute(() =>
                {
                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = DeliveryModePersistent;

                    channel.BasicPublish(BROKER_NAME,
                                     eventName,
                                     true,
                                     properties,
                                     body);
                });
            }
        }

        /// <summary>
        /// The subscribe method.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="action">The action to be invoked upon event trigger.</param>
        public void Subscribe(string topic, Action<byte[]> action)
        {
            using (var connection = GetConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(topic, false, false, false, null);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (sender, e) => EventReceived(sender, e, action);
                }
            }
        }

        /// <summary>
        /// The subscription method for message by an event handler.
        /// </summary>
        /// <typeparam name="T">The type of the message being handled.</typeparam>
        /// <typeparam name="TH">The event handler.</typeparam>
        public void Subscribe<T, TH>()
            where T : IMessage
            where TH : IEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();
            DoInternalSubscription(eventName);
            _subsManager.AddSubscription<T, TH>();
        }

        /// <summary>
        /// The unsubscribe method for event handlers.
        /// </summary>
        /// <typeparam name="T">The type of message event to be unsubscribed from.</typeparam>
        /// <typeparam name="TH">The event handler.</typeparam>
        public void Unsubscribe<T, TH>()
            where T : IMessage
            where TH : IEventHandler<T>
        {
            throw new NotSupportedException();
        }

        private IConnection GetConnection()
        {
            return new ConnectionFactory
            {
                UserName = _configuration.Value.EventBusUserName,
                Password = _configuration.Value.EventBusPassword,
                VirtualHost = "/",
                HostName = _configuration.Value.EventBusConnection
            }.CreateConnection();
        }

        /// <summary>
        /// The method for creating the consumer channel.
        /// </summary>
        /// <returns>The channel created.</returns>
        private IModel CreateConsumerChannel()
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var channel = _persistentConnection.CreateModel();

            channel.ExchangeDeclare(BROKER_NAME, "direct");

            channel.QueueDeclare(_configuration.Value.EventBusQueueName, true, false, false, null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (sender, e) => await EventReceivedAsync(sender, e, channel).ConfigureAwait(false);

            channel.BasicConsume(_configuration.Value.EventBusQueueName, false, consumer);

            channel.CallbackException += OnException;

            return channel;
        }

        /// <summary>
        /// The internal subscription method.
        /// </summary>
        /// <param name="eventName">The name of event.</param>
        private void DoInternalSubscription(string eventName)
        {
            var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }

                using (var channel = _persistentConnection.CreateModel())
                {
                    channel.QueueBind(_configuration.Value.EventBusQueueName, BROKER_NAME, eventName);
                }
            }
        }

        private void HandleException(Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }

        /// <summary>
        /// The method that is invoked on exceptions.
        /// </summary>
        /// <param name="sender">The source of the exception.</param>
        /// <param name="args">The arguments.</param>
        private void OnException(object sender, CallbackExceptionEventArgs args)
        {
            _consumerChannel.Dispose();
            _consumerChannel = CreateConsumerChannel();
        }

        /// <summary>
        /// The invocation for the event.
        /// </summary>
        /// <param name="eventName">The name of event.</param>
        /// <param name="message">The message in bytes.</param>
        /// <returns>The indicator for success.</returns>
        private async Task<bool> ProcessEventAsync(string eventName, byte[] message)
        {
            var result = false;
            if (!_subsManager.HasSubscriptionsForEvent(eventName))
            {
                return false;
            }
            using (var scope = _autofac.BeginLifetimeScope(AUTOFAC_SCOPE_NAME))
            {
                var subscriptions = _subsManager.GetHandlersForEvent(eventName);
                if (subscriptions == null || !subscriptions.Any())
                {
                    return false;
                }

                foreach (var subscription in subscriptions)
                {
                    var eventType = _subsManager.GetEventTypeByName(eventName);
                    var integrationEvent = message;
                    var handler = scope.ResolveOptional(subscription.HandlerType);
                    var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);
                    var methodTask = concreteType.GetMethod("HandleAsync");
                    if (methodTask == null)
                    {
                        continue;
                    }
                    result = await InvokeMethodAsync(integrationEvent, handler, methodTask).ConfigureAwait(false);
                }
            }
            return result;
        }

        /// <summary>
        /// Invokes the method asynchronous.
        /// </summary>
        /// <param name="integrationEvent">The integration event.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="methodTask">The method task.</param>
        /// <returns>Boolean indicating the result.</returns>
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
                HandleException(ex);
            }
            catch (ArgumentException ex)
            {
                HandleException(ex);
            }
            catch (TargetInvocationException ex)
            {
                HandleException(ex);
            }
            catch (TargetParameterCountException ex)
            {
                HandleException(ex);
            }
            catch (MethodAccessException ex)
            {
                HandleException(ex);
            }
            catch (InvalidOperationException ex)
            {
                HandleException(ex);
            }
            catch (NotSupportedException ex)
            {
                HandleException(ex);
            }

            return result;
        }
    }
}

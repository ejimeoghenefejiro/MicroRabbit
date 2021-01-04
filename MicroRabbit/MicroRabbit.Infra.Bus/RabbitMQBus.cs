using MediatR;
using MicroRabbit.Domain.Core.Bus;
using MicroRabbit.Domain.Core.Commands;
using MicroRabbit.Domain.Core.Events;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRabbit.Infra.Bus
{
    public sealed class RabbitMQBus : IEventBus// A sealled class cannot be inherited 
    {
        private readonly IMediator _mediator;
        private readonly Dictionary<string, List<Type>> _handlers; // dictionary of handlers 
        private readonly List<Type> _eventTypes; // generic so it can handle any type of events. We dont have to rewrite method everytime we need to create an event

        public RabbitMQBus( IMediator mediator)
        {
            _mediator = mediator;
            _handlers = new Dictionary<string, List<Type>>();
            _eventTypes = new List<Type>();
        }

        public Task SendCommand<T>(T command) where T : Command
        {
            return _mediator.Send(command); // expecting a generic type to send the command 
        }

        //Publish message to RabbittMQ Server
        public void Publish<T>(T @event) where T : Event
        {
            var factory = new ConnectionFactory() { HostName= "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var eventName = @event.GetType().Name; // get event name  by using reflection. I can take any name and extra the name of that event
                channel.QueueDeclare(eventName, false, false, false, null);

                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish("", eventName, null, body);
            }
           
        }

        public void Subscribe<T, TH>()
            where T : Event
            where TH : IEventHandler<T>
        {
            var eventName = typeof(T).Name; // Making use of generic and reflection
            var handlerType = typeof(TH);

            if (!_eventTypes.Contains(typeof(T)))
            {
                _eventTypes.Add(typeof(T));
            }
            if (!_handlers.ContainsKey(eventName))
            {
                _handlers.Add(eventName, new List<Type>());
            }
            if (_handlers[eventName].Any(s =>s.GetType() == handlerType))
            {
                throw new ArgumentException(
                    $"Handler Type{handlerType.Name} already is registered for '{eventName}'", nameof(handlerType));
            }

            _handlers[eventName].Add(handlerType);
            StartBasicConsume<T>();
        }

        private void StartBasicConsume<T>() where T : Event
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                DispatchConsumersAsync = true
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            var eventName = typeof(T).Name;
            channel.QueueDeclare(eventName, false, false, false, null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += Consumer_Received; // Delegate are placeholder for event

            channel.BasicConsume(eventName, true, consumer);



        }
        // The Consumer_Received is listening for incoming messages 
        private async Task Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            var eventName = e.RoutingKey;  // Get the event name through the routing key. The queue  has a routing key
            var message = Encoding.UTF8.GetString(e.Body);

            try
            {
                await ProcessEvent(eventName, message).ConfigureAwait(false); // Know which event is subscribe and do all the work in the background
            }
            catch (Exception)
            {

                throw;
            }
        }

        // Dynamically  create handler based on the handler type in our dictionary  of handlers and then invoke event handler for that type of event
        private async Task ProcessEvent(string eventName, string message)
        {
            if (_handlers.ContainsKey(eventName))  // The key for the dictionary is eventName
            {
                var subscriptions = _handlers[eventName]; // This is a dictionary list of type 
                foreach (var subscription in subscriptions) // Looping through dictionary list of type
                {
                    var handler = Activator.CreateInstance(subscription); //dynamic approaches to our generics. With the CreateInstance is like create new Class
                    if (handler == null) continue; // If the handler is null continue  looping through until we find one 
                    var eventType = _eventTypes.SingleOrDefault(t => t.Name == eventName);
                    var @event = JsonConvert.DeserializeObject(message, eventType);

                    var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);

                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { eventType }); // All routing for microservice take place here 
                   
                }
            }
        }
    }
}

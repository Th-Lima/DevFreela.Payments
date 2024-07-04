using DevFreela.Payments.API.Models;
using DevFreela.Payments.API.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace DevFreela.Payments.API.Consumers
{
    public class ProcessPaymentConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IServiceProvider _serviceProvider;

        private const string QUEUE = "Payments";
        private const string PAYMENTS_APPROVED_QUEUE = "PaymentsApproved";

        public ProcessPaymentConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory()
            {
                HostName = "localhost"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: QUEUE,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueDeclare(queue: PAYMENTS_APPROVED_QUEUE,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (sender, eventArgs) =>
            {
                var byteArray = eventArgs.Body.ToArray();

                var PaymentInfoJson = Encoding.UTF8.GetString(byteArray);

                var paymentInfo = JsonSerializer.Deserialize<PaymentInfoInputModel>(PaymentInfoJson);

                ProcessPayment(paymentInfo);

                var paymentsApproved = new PaymentApprovedIntegrationEvent(paymentInfo.Id, paymentInfo.FullName);
                var paymentsApprovedJson = JsonSerializer.Serialize(paymentsApproved);
                var paymentApprovedBytes = Encoding.UTF8.GetBytes(paymentsApprovedJson);

                _channel.BasicPublish(exchange: "",
                    routingKey: PAYMENTS_APPROVED_QUEUE,
                    basicProperties: null,
                    body: paymentApprovedBytes);

                _channel.BasicAck(eventArgs.DeliveryTag, false);
            };

            _channel.BasicConsume(QUEUE, false, consumer);

            return Task.CompletedTask;
        }

        public void ProcessPayment(PaymentInfoInputModel paymentInfoInputModel)
        {
            using var scope = _serviceProvider.CreateScope();

            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

            paymentService.Process(paymentInfoInputModel);
        }
    }
}

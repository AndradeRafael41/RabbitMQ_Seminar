using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

public class RpcServer
{
    private readonly Dictionary<string, IOperationService> _services;

    public RpcServer()
    {
        _services = new Dictionary<string, IOperationService>
        {
            { "msg", new MessageService() },
            { "file", new FileService() },
            { "calc", new MathService() }
        };
    }

    public void Start()
    {


        int tentativas = 0;
        int maxTentativas = 10;
        IModel? channel = null;

        while (tentativas < maxTentativas)
        {
            try
            {
                Console.WriteLine($"Tentando conectar ao RabbitMQ ({tentativas + 1}/{maxTentativas})...");

                // abrindo conexão com RabbitMQ
                var factory = new ConnectionFactory()
                {
                    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq",
                    UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
                    Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest"
                };
                var connection = factory.CreateConnection();
                channel = connection.CreateModel();

                Console.WriteLine("Conectado ao RabbitMQ!");
                break;
            }
            catch (Exception ex)
            {
                tentativas++;
                Console.WriteLine($"Falha ao conectar: {ex.Message}");

                if (tentativas >= maxTentativas)
                {
                    Console.WriteLine("Não foi possível conectar ao RabbitMQ. Encerrando servidor.");
                    return;
                }

                Thread.Sleep(3000);
            }

        }

        try
        {
            // Declarando fila para receber as chamadas RPC
            var queue = Environment.GetEnvironmentVariable("QUEUE_RPC") ?? "fila_rpc";
            channel.QueueDeclare(queue, false, false, false);

            // Exchange que faz broadcasting para todos os subs
            channel.ExchangeDeclare(
                exchange: "async_pubsub",
                type: ExchangeType.Fanout,
                durable: false,
                autoDelete: false
            );

            // Cada instância do servidor terá sua própria fila
            // Quando o servidor desconectar, a fila é automaticamente deletada
            var asyncQueue = channel.QueueDeclare(
                queue: "",
                durable: false,
                exclusive: true,
                autoDelete: true
            ).QueueName;

            // Vincula a fila ao Fanout Exchange
            channel.QueueBind(
                queue: asyncQueue,
                exchange: "async_pubsub",
                routingKey: ""
            );

            // Criando consumidor para processar mensagens da fila RPC
            var consumer = new EventingBasicConsumer(channel);

            // Evento disparado quando uma mensagem é recebida na fila (RPC)
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var messageJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var request = JsonSerializer.Deserialize<RequestMessage>(messageJson);

                    Console.WriteLine($"\n[RPC] Processando requisição: {request?.Operation}");
                    Console.WriteLine($"[RPC] Payload: {request?.Payload}");

                    // chamada do método de processamento da requisição
                    if (request == null)
                    {
                        Console.WriteLine("[RPC] Request inválida");
                        channel?.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    var response = await ProcessAsync(request);

                    Console.WriteLine($"[RPC] Resposta: {response}");

                    // preparando resposta para o cliente
                    var replyProps = channel?.CreateBasicProperties();
                    if (replyProps != null)
                    {
                        replyProps.CorrelationId = ea.BasicProperties.CorrelationId;
                    }

                    // serializando a resposta em bytes para envio
                    var responseBytes = Encoding.UTF8.GetBytes(response);

                    // envia respota para a fila de resposta do cliente
                    channel?.BasicPublish(
                        exchange: "",
                        routingKey: ea.BasicProperties.ReplyTo,
                        basicProperties: replyProps,
                        body: responseBytes
                    );

                    channel?.BasicAck(ea.DeliveryTag, false);
                }

                catch (Exception ex)
                {
                    Console.WriteLine($"[RPC] Erro ao processar mensagem: {ex.Message}");

                    // descarta mensagem com erro (não requeue)
                    channel?.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            // Subscriber
            var asyncConsumer = new EventingBasicConsumer(channel);

            asyncConsumer.Received += async (model, ea) =>
            {
                try
                {
                    var messageJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var request = JsonSerializer.Deserialize<RequestMessage>(messageJson);

                    if (request == null)
                    {
                        Console.WriteLine("[PUB/SUB] Request inválida");
                        channel?.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    Console.WriteLine($"\n[PUB/SUB] Mensagem recebida via Broadcasting");
                    Console.WriteLine($"[PUB/SUB] Operação: {request.Operation}");
                    Console.WriteLine($"[PUB/SUB] Payload: {request.Payload}");

                    // Processa a mensagem mas não envia resposta
                    var response = await ProcessAsync(request);

                    Console.WriteLine($"[PUB/SUB] Resultado: {response}");

                    channel?.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PUB/SUB] Erro ao processar mensagem: {ex.Message}");
                    channel?.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            channel?.BasicConsume(queue, false, consumer);
            channel?.BasicConsume(asyncQueue, false, asyncConsumer);

            Console.WriteLine("\n╔═══════════════════════════════════════════════╗");
            Console.WriteLine("║     SERVIDOR RPC + PUB/SUB INICIADO         ║");
            Console.WriteLine("╚═══════════════════════════════════════════════╝");
            Console.WriteLine($"\n→ RPC: {queue}");
            Console.WriteLine($"→ Pub/Sub: async_pubsub (broadcasting)\n");

            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao iniciar servidor: {ex.Message}");
        }
    }

    // Método para processar a requisição e chamar o serviço correspondente
    private async Task<string> ProcessAsync(RequestMessage request)
    {
        if (request == null || request.Operation == null || request.Payload == null)
            return "Request inválida";

        // verifica se o serviço é válido dentro dos disponíveis e executa a operação
        if (_services.TryGetValue(request.Operation, out var service))
            return await service.ExecuteAsync(request.Payload);

        return "Operação inválida";
    }
}

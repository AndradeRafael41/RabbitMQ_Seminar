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
                    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq"
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

            // Criando consumidor para processar mensagens da fila
            var consumer = new EventingBasicConsumer(channel);


            // Evento disparado quando uma mensagem é recebida na fila (Assíncrono)
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var messageJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var request = JsonSerializer.Deserialize<RequestMessage>(messageJson);

                    // chamada do método de processamento da requisição
                    var response = await ProcessAsync(request);

                    // preparando resposta para o cliente
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = ea.BasicProperties.CorrelationId;

                    // serializando a resposta em bytes para envio
                    var responseBytes = Encoding.UTF8.GetBytes(response);

                    // envia respota para a fila de resposta do cliente    
                    channel.BasicPublish(
                        exchange: "",
                        routingKey: ea.BasicProperties.ReplyTo,
                        basicProperties: replyProps,
                        body: responseBytes
                    );

                    channel.BasicAck(ea.DeliveryTag, false);
                }

                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar mensagem: {ex.Message}");

                    // descarta mensagem com erro (não requeue)
                    channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            channel.BasicConsume(queue, false, consumer);

            Console.WriteLine("Servidor aguardando mensagens...");
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
        if (request == null)
            return "Request inválida";

        // verifica se o serviço é válido dentro dos disponíveis e executa a operação
        if (_services.TryGetValue(request.Operation, out var service))
            return await service.ExecuteAsync(request.Payload);

        return "Operação inválida";
    }
}
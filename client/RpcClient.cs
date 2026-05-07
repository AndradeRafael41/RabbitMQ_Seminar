using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

public class RpcClient : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queue;
    private readonly int _timeou;

    public RpcClient()
    {

        try
        {
            // abrindo a conexão com o RabbitMQ
            var host = Environment.GetEnvironmentVariable("RabbitMQ_HOST") ?? "localhost";
            _queue = Environment.GetEnvironmentVariable("QUEUE_RPC") ?? "fila_rpc";
            _timeou = int.Parse(Environment.GetEnvironmentVariable("RPC_TIMEOUT") ?? "5000");

            var factory = new ConnectionFactory()
            {
                HostName = host,
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // criando e configurando a fila RPC
            _channel.QueueDeclare(_queue, false, false, false);

            // Declarando Fanout Exchange para Publish-Subscribe
            _channel.ExchangeDeclare(
                exchange: "async_pubsub",
                type: ExchangeType.Fanout,
                durable: false,
                autoDelete: false
            );

            Console.WriteLine($"[OK] Conectado ao RabbitMQ em {host}");
            Console.WriteLine($"[OK] Exchange Pub/Sub configurado");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERRO] Falha ao conectar ao RabbitMQ: " + ex.Message);
            throw;
        }

    }

    // metodo sincrono (RPC)
    public string Call(string operation, string payload)
    {
        try
        {
            var request = new RequestMessage
            {
                Operation = operation,
                Payload = payload
            };

            // serializando a requisição
            var json = JsonSerializer.Serialize(request);
            var body = Encoding.UTF8.GetBytes(json);

            // criando o correlationId
            var correlationId = Guid.NewGuid().ToString();
            var replyQueue = _channel.QueueDeclare("", false, true, true).QueueName;

            var props = _channel.CreateBasicProperties(); ;
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueue;

            string? response = null;

            var consumer = new EventingBasicConsumer(_channel);

            // verifica se há alguma resposta corresponde ao correlationId disponível na fila
            consumer.Received += (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    response = Encoding.UTF8.GetString(ea.Body.ToArray());
                }
            };

            _channel.BasicConsume(replyQueue, true, consumer);

            //enviando uma requisição
            _channel.BasicPublish("", _queue, props, body);

            // definindo possível RPC_TIMEOUT
            int waited = 0;

            while (response == null && waited < _timeou)
            {
                Thread.Sleep(100);
                waited += 100;
            }

            return response ?? "[TIMEOUT] Tempo de resposta do servidor Excedido";

        }
        catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException)
        {
            return "[ERRO] RabbitMQ indisponível";
        }
        catch (Exception ex)
        {
            return "[ERRO] Falha na requisição: " + ex.Message;
        }
    }

    // Método Publish-Subscribe
    public void PublishAsync(string operation, string payload)
    {
        try
        {
            var request = new RequestMessage
            {
                Operation = operation,
                Payload = payload
            };

            // serializando a requisição
            var json = JsonSerializer.Serialize(request);
            var body = Encoding.UTF8.GetBytes(json);

            // A mensagem será recebida por TODOS os servidores conectados ao exchange "async_pubsub"
            _channel.BasicPublish(
                exchange: "async_pubsub",
                routingKey: "",
                basicProperties: null,
                body: body
            );

            Console.WriteLine($"\n[PUB/SUB] Mensagem publicada para TODOS os subscribers");
            Console.WriteLine($"[PUB/SUB] Operação: {operation}");
            Console.WriteLine($"[PUB/SUB] Payload: {payload}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao enviar mensagem assíncrona: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
            Console.WriteLine("[INFO] Conexão encerrada");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERRO] Falha ao encerrar conexão: " + ex.Message);
        }
    }
}

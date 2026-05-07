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
            var host = Environment.GetEnvironmentVariable("RabbitMQ_HOST")
                       ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST")
                       ?? "localhost";

            var user = Environment.GetEnvironmentVariable("RabbitMQ_USER")
                       ?? Environment.GetEnvironmentVariable("RABBITMQ_USER")
                       ?? "guest";

            var pass = Environment.GetEnvironmentVariable("RabbitMQ_PASS")
                       ?? Environment.GetEnvironmentVariable("RABBITMQ_PASS")
                       ?? "guest";

            var port = int.Parse(Environment.GetEnvironmentVariable("RabbitMQ_PORT")
                                 ?? Environment.GetEnvironmentVariable("RABBITMQ_PORT")
                                 ?? "5672");

            var vhost = Environment.GetEnvironmentVariable("RabbitMQ_VHOST")
                        ?? Environment.GetEnvironmentVariable("RABBITMQ_VHOST")
                        ?? "/";

            _queue = Environment.GetEnvironmentVariable("QUEUE_RPC") ?? "fila_rpc";
            _timeou = int.Parse(Environment.GetEnvironmentVariable("RPC_TIMEOUT") ?? "5000");

            var factory = new ConnectionFactory()
            {
                HostName = host,
                Port = port,
                UserName = user,
                Password = pass,
                VirtualHost = vhost,
                // timeouts mais tolerantes para redes externas / WAN
                RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
                SocketReadTimeout = TimeSpan.FromSeconds(30),
                SocketWriteTimeout = TimeSpan.FromSeconds(30),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            Console.WriteLine($"[INFO] Conectando em amqp://{user}@{host}:{port}{vhost} ...");

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // criando e configurando a fila
            _channel.QueueDeclare(_queue, false, false, false);

            Console.WriteLine($"[OK] Conectado ao RabbitMQ em {host}:{port}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERRO] Falha ao conectar ao RabbitMQ: " + ex.Message);
            if (ex.InnerException != null)
                Console.WriteLine("       Detalhe: " + ex.InnerException.Message);
            throw;
        }

    }

    // metodo sincrono (Apenas para o Cliente)
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

            // criando o correlationId (importante !)
            var correlationId = Guid.NewGuid().ToString();

            // criando uma fila temporária de resposta exclusiva com autoexclusão para o cliente
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

    // Método assíncrono (Fire-and-forget) - Envia mensagem sem esperar resposta
    public void SendAsync(string operation, string payload)
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

            // Envia para uma fila dedicada de processamento assíncrono
            var asyncQueue = Environment.GetEnvironmentVariable("QUEUE_ASYNC") ?? "fila_async";

            // Declara a fila se não existir
            _channel.QueueDeclare(asyncQueue, false, false, false);

            // Envia mensagem sem aguardar resposta
            _channel.BasicPublish("", asyncQueue, null, body);

            Console.WriteLine($"\n[OK] Mensagem enviada para processamento assíncrono");
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

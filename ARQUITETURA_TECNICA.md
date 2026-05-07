# Arquitetura Técnica - Sistema RPC RabbitMQ

## 📐 Visão Geral da Arquitetura

Este documento detalha a arquitetura técnica do sistema de comunicação distribuída usando RabbitMQ.

---

## 🏛️ Componentes do Sistema

### 1. RabbitMQ (Message Broker)

**Versão:** 3-management
**Portas:**
- 5672: AMQP protocol (comunicação)
- 15672: Management UI (interface web)

**Responsabilidades:**
- Receber mensagens dos clientes
- Rotear mensagens para filas apropriadas
- Entregar mensagens aos consumidores
- Gerenciar acknowledgments
- Fornecer interface de monitoramento

**Configuração:**
```yaml
Environment Variables:
  - RABBITMQ_DEFAULT_USER: guest
  - RABBITMQ_DEFAULT_PASS: guest

Health Check:
  - Command: rabbitmq-diagnostics -q ping
  - Interval: 10s
  - Timeout: 5s
  - Retries: 5
```

---

### 2. Servidor RPC

**Tecnologia:** .NET 8.0
**Tipo:** Console Application
**Containerizado:** Sim (Docker)

**Responsabilidades:**
- Conectar ao RabbitMQ
- Consumir mensagens das filas
- Processar requisições
- Enviar respostas ao cliente
- Executar operações de negócio

**Estrutura de Classes:**

```
RpcServer
├── Dictionary<string, IOperationService> _services
├── void Start()
└── async Task<string> ProcessAsync(RequestMessage)

Services/
├── IOperationService (interface)
├── MessageService : IOperationService
├── FileService : IOperationService
└── MathService : IOperationService
```

**Configuração:**
```
Environment Variables:
  - RABBITMQ_HOST: rabbitmq
  - QUEUE_RPC: fila_rpc
  - QUEUE_ASYNC: fila_async
  - FILE_PATH: /app/dados.txt
```

---

### 3. Cliente RPC

**Tecnologia:** .NET 9.0
**Tipo:** Console Application
**Containerizado:** Não (roda nativamente)

**Responsabilidades:**
- Conectar ao RabbitMQ
- Enviar requisições
- Aguardar respostas (RPC)
- Enviar mensagens assíncronas
- Interface com usuário

**Estrutura de Classes:**

```
RpcClient : IDisposable
├── IConnection _connection
├── IModel _channel
├── string Call(string operation, string payload)  // RPC
└── void SendAsync(string operation, string payload)  // Fire-and-forget
```

**Configuração:**
```
Environment Variables:
  - RabbitMQ_HOST: localhost (ou IP do servidor)
  - QUEUE_RPC: fila_rpc
  - QUEUE_ASYNC: fila_async
  - RPC_TIMEOUT: 5000 (ms)
```

---

## 🔄 Fluxos de Comunicação

### Fluxo RPC (Síncrono)

```
┌─────────────────────────────────────────────────────────────┐
│                    CLIENTE (Máquina 2)                      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ 1. Cria RequestMessage
                     │    { Operation: "calc", Payload: "soma,5,3" }
                     │
                     │ 2. Gera CorrelationId único
                     │    "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
                     │
                     │ 3. Cria fila temporária de resposta
                     │    "amq.gen-randomString"
                     │
                     │ 4. Publica mensagem
                     ↓
┌──────────────────────────────────────────────────────────┐
│                  RABBITMQ (Máquina 1)                    │
│                                                          │
│  Queue: fila_rpc                                         │
│  ┌──────────────────────────────────────────┐            │
│  │ Message:                                  │           │
│  │   Body: {"Operation":"calc",...}         │            │
│  │   CorrelationId: a1b2c3d4...             │            │
│  │   ReplyTo: amq.gen-randomString          │            │
│  └──────────────────────────────────────────┘            │
└────────────────────┬─────────────────────────────────────┘
                     │
                     │ 5. Roteia para consumidor
                     │
                     ↓
┌───────────────────────────────────────────────────────────┐
│                  SERVIDOR (Máquina 1)                     │
│                                                           │
│  6. Consumer recebe mensagem                              │
│  7. Deserializa RequestMessage                            │
│  8. Chama ProcessAsync()                                  │
│  9. Identifica serviço: "calc" → MathService              │
│  10. ExecuteAsync("soma,5,3")                             │
│  11. Retorna: "Soma: 5 e 3 = 8.00"                        │
│                                                           │
│  12. Cria mensagem de resposta                            │
│      - CorrelationId: a1b2c3d4... (mesmo!)                │
│      - Destino: amq.gen-randomString                      │
│                                                           │
│  13. Publica na fila de resposta                          │
│  14. BasicAck (confirma processamento)                    │
└────────────────────┬──────────────────────────────────────┘
                     │
                     │ 15. RabbitMQ roteia resposta
                     ↓
┌────────────────────────────────────────────────────────────┐
│                    CLIENTE (Máquina 2)                     │
│                                                            │
│  16. Consumer de resposta recebe                           │
│  17. Verifica CorrelationId                                │
│  18. Match! Armazena resposta                              │
│  19. While loop detecta resposta != null                   │
│  20. Retorna ao usuário: "Soma: 5 e 3 = 8.00"              │
└────────────────────────────────────────────────────────────┘
```

---

### Fluxo Assíncrono (Fire-and-Forget)

```
┌─────────────────────────────────────────────────────────────┐
│                    CLIENTE (Máquina 2)                      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ 1. Cria RequestMessage
                     │    { Operation: "msg", Payload: "Hello Async" }
                     │
                     │ 2. Publica em fila_async
                     │    (SEM CorrelationId ou ReplyTo)
                     │
                     │ 3. Retorna imediatamente
                     │    Console: "[OK] Mensagem enviada..."
                     ↓
┌────────────────────────────────────────────────────────────┐
│                  RABBITMQ (Máquina 1)                      │
│                                                            │
│  Queue: fila_async                                         │
│  ┌──────────────────────────────────────────┐              │
│  │ Message:                                 │              │
│  │   Body: {"Operation":"msg",...}          │              │
│  │   (sem CorrelationId)                    │              │
│  └──────────────────────────────────────────┘              │
└────────────────────┬───────────────────────────────────────┘
                     │
                     ↓
┌────────────────────────────────────────────────────────────┐
│                  SERVIDOR (Máquina 1)                      │
│                                                            │
│  4. AsyncConsumer recebe mensagem                          │
│  5. ProcessAsync()                                         │
│  6. Log no console (não envia resposta)                    │
│  7. BasicAck                                               │
└────────────────────────────────────────────────────────────┘
```

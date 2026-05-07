# 🎤 GUIA COMPLETO PARA APRESENTAÇÃO DO PROJETO

## 📌 VISÃO GERAL DO PROJETO

### O que foi desenvolvido?
Sistema de comunicação distribuída Cliente-Servidor usando **RabbitMQ** como middleware de mensageria, implementando:
- ✅ Padrão **RPC (Remote Procedure Call)** - comunicação síncrona com resposta
- ✅ Padrão **Fire-and-Forget** - comunicação assíncrona sem resposta
- ✅ **3 operações funcionais**: mensagem de texto, alteração de arquivo, cálculos matemáticos

### Por que usar RabbitMQ?
- **Desacoplamento**: Cliente e servidor não precisam conhecer a localização um do outro
- **Resiliência**: Mensagens são persistidas em filas, não se perdem se um serviço cair
- **Escalabilidade**: Fácil adicionar múltiplos servidores consumindo da mesma fila
- **Assíncrono**: Processamento não-bloqueante, melhor performance
- **Padrão da indústria**: Usado por empresas como Uber, Instagram, Reddit

---

## 🏗️ ARQUITETURA DO SISTEMA

### Componentes

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐
│   Cliente   │ ───────▶│  RabbitMQ   │◀─────── │  Servidor   │
│   .NET 9    │         │  (Broker)   │         │   .NET 8    │
│             │◀─────── │             │ ───────▶│             │
└─────────────┘         └──────────────┘         └─────────────┘
     │                                                   │
     │                  2 Filas:                        │
     │              ┌──────────────┐                    │
     │              │  fila_rpc    │ (com resposta)     │
     │              │  fila_async  │ (sem resposta)     │
     │              └──────────────┘                    │
     │                                                   │
     └──────────── Requisição/Resposta ─────────────────┘
```

### Tecnologias Utilizadas

| Componente | Tecnologia | Versão | Justificativa |
|------------|------------|--------|---------------|
| Cliente | C# (.NET 9) | 9.0 | Mais recente, melhor performance |
| Servidor | C# (.NET 8) | 8.0 | LTS (Long-Term Support), estável |
| Middleware | RabbitMQ | 3 | Padrão da indústria para mensageria |
| Biblioteca | RabbitMQ.Client | 6.8.1 | Biblioteca oficial |
| Serialização | System.Text.Json | Nativa | Alta performance, nativa do .NET |
| Containerização | Docker Compose | - | Facilita deploy e testes |

---

## 🔄 FLUXO COMPLETO DE UMA REQUISIÇÃO RPC

### Passo a Passo (Request-Reply Pattern)

#### 1️⃣ **Cliente prepara a requisição**
```csharp
var request = new RequestMessage {
    Operation = "calc",
    Payload = "soma,5,3"
};
```

#### 2️⃣ **Cliente serializa para JSON**
```json
{"Operation":"calc","Payload":"soma,5,3"}
```

#### 3️⃣ **Cliente gera CorrelationId único**
```csharp
var correlationId = Guid.NewGuid().ToString();
// Exemplo: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

**Por que CorrelationId é importante?**
- Permite **correlacionar** a resposta com a requisição correta
- Múltiplas requisições podem estar em andamento simultaneamente
- Garante que cada cliente receba a resposta da SUA requisição

#### 4️⃣ **Cliente cria fila temporária exclusiva para resposta**
```csharp
var replyQueue = _channel.QueueDeclare("", false, true, true).QueueName;
// Exemplo: "amq.gen-JzTY20BRgKO-HjkKzVnNxQ"
```

**Características da fila temporária:**
- Nome gerado automaticamente pelo RabbitMQ
- **Exclusiva**: Apenas essa conexão pode acessá-la
- **Auto-delete**: Apagada quando a conexão fechar
- **Não-durável**: Não persiste se o RabbitMQ reiniciar

#### 5️⃣ **Cliente configura propriedades da mensagem**
```csharp
var props = _channel.CreateBasicProperties();
props.CorrelationId = correlationId;    // Para correlação
props.ReplyTo = replyQueue;             // Onde enviar resposta
```

#### 6️⃣ **Cliente publica na fila RPC do servidor**
```csharp
_channel.BasicPublish(
    exchange: "",              // Exchange padrão
    routingKey: "fila_rpc",   // Fila de destino
    basicProperties: props,    // CorrelationId + ReplyTo
    body: jsonBytes           // Mensagem serializada
);
```

#### 7️⃣ **Servidor recebe e processa**
```csharp
consumer.Received += async (model, ea) => {
    // 1. Desserializa JSON
    var request = JsonSerializer.Deserialize<RequestMessage>(messageJson);

    // 2. Identifica o serviço (calc → MathService)
    var service = _services["calc"];

    // 3. Executa a operação
    var response = await service.ExecuteAsync("soma,5,3");
    // Resultado: "Soma: 5 e 3 = 8.00"

    // 4. Prepara resposta com MESMO CorrelationId
    var replyProps = channel.CreateBasicProperties();
    replyProps.CorrelationId = ea.BasicProperties.CorrelationId;

    // 5. Envia resposta para fila do cliente
    channel.BasicPublish(
        exchange: "",
        routingKey: ea.BasicProperties.ReplyTo,  // Fila temporária
        basicProperties: replyProps,
        body: responseBytes
    );

    // 6. Confirma processamento
    channel.BasicAck(ea.DeliveryTag, false);
};
```

#### 8️⃣ **Cliente aguarda resposta**
```csharp
while (response == null && waited < timeout) {
    Thread.Sleep(100);
    waited += 100;
}
```

**Comportamento do timeout:**
- Padrão: 5000ms (5 segundos)
- Se exceder: retorna "[TIMEOUT] Tempo de resposta do servidor Excedido"
- Configurável via variável de ambiente `RPC_TIMEOUT`

#### 9️⃣ **Cliente recebe resposta**
```csharp
consumer.Received += (model, ea) => {
    // Valida se é a resposta correta
    if (ea.BasicProperties.CorrelationId == correlationId) {
        response = Encoding.UTF8.GetString(ea.Body.ToArray());
        // response = "Soma: 5 e 3 = 8.00"
    }
};
```

---

## 🎯 AS 3 OPERAÇÕES IMPLEMENTADAS

### 1️⃣ Mensagem de Texto (MessageService)

**Código:** `msg`

**O que faz:**
- Recebe uma mensagem de texto
- Adiciona timestamp
- Conta caracteres
- Retorna confirmação

**Exemplo prático:**
```
Cliente envia: "Olá RabbitMQ!"
Servidor processa e responde: "[14:30:25] Servidor recebeu: "Olá RabbitMQ!" (Tamanho: 14 caracteres)"
```

**Por que é importante:**
- Demonstra comunicação básica
- Valida que o sistema está funcionando
- Útil para debugging e testes

### 2️⃣ Alteração de Arquivo (FileService)

**Código:** `file`

**O que faz:**
- Recebe texto para salvar
- Adiciona timestamp automático
- **Append** no arquivo (preserva histórico)
- Retorna tamanho total do arquivo

**Exemplo prático:**
```
Cliente envia: "Log importante do sistema"
Servidor salva em file.txt:
  [2026-05-06 14:30:25] Log importante do sistema

Responde: "✓ Conteúdo salvo em 'file.txt'. Tamanho total: 1024 bytes"
```

**Persistência:**
- Arquivo: `/data/file.txt` (dentro do container)
- Volume Docker mapeia para: `./server/file.txt` (no host)
- **Sobrevive** a reinicializações do container

**Por que usar async/await:**
```csharp
await File.AppendAllTextAsync(path, conteudoComTimestamp);
```
- I/O é operação lenta (disco)
- Não bloqueia a thread enquanto escreve
- Melhor performance em alta carga

### 3️⃣ Operações Matemáticas (MathService)

**Código:** `calc`

**7 operações disponíveis:**

| # | Operação | Comando | Exemplo | Resultado |
|---|----------|---------|---------|-----------|
| 1 | Soma | `soma` ou `+` | `soma,5,3` | Soma: 5 e 3 = 8.00 |
| 2 | Subtração | `sub` ou `-` | `sub,10,4` | Subtração: 10 e 4 = 6.00 |
| 3 | Multiplicação | `mult` ou `*` | `mult,7,6` | Multiplicação: 7 e 6 = 42.00 |
| 4 | Divisão | `div` ou `/` | `div,15,3` | Divisão: 15 e 3 = 5.00 |
| 5 | Potência | `pot` ou `^` | `pot,2,8` | Potência: 2 e 8 = 256.00 |
| 6 | Módulo | `mod` ou `%` | `mod,10,3` | Módulo: 10 e 3 = 1.00 |
| 7 | Raiz n-ésima | `raiz` | `raiz,27,3` | Raiz: 27 e 3 = 3.00 |

**Formato do payload:**
```
operacao,numero1,numero2
```

**Validações implementadas:**
- ✅ Formato correto (3 partes separadas por vírgula)
- ✅ Números válidos (double.TryParse)
- ✅ Divisão por zero (retorna erro)
- ✅ Módulo por zero (retorna erro)
- ✅ Raiz com índice zero (retorna erro)

**Exemplo de validação:**
```csharp
case "div":
    if (b == 0)
        return Task.FromResult("Erro: Divisão por zero não permitida");
    resultado = a / b;
    break;
```

**Por que múltiplos aliases?**
- Facilita uso: `soma` = `+`
- Mais intuitivo para usuários
- Flexibilidade de interface

---

## 🔑 CONCEITOS-CHAVE PARA EXPLICAR

### 1. Padrão Strategy (Design Pattern)

**O que é:**
Permite trocar algoritmos/comportamentos em tempo de execução através de uma interface comum.

**Como foi usado:**
```csharp
// Interface comum
public interface IOperationService {
    Task<string> ExecuteAsync(string payload);
}

// Implementações específicas
public class MessageService : IOperationService { ... }
public class FileService : IOperationService { ... }
public class MathService : IOperationService { ... }

// Registro dinâmico
var _services = new Dictionary<string, IOperationService> {
    { "msg", new MessageService() },
    { "file", new FileService() },
    { "calc", new MathService() }
};

// Execução polimórfica
var service = _services[operation];
var result = await service.ExecuteAsync(payload);
```

**Vantagens:**
- ✅ Fácil adicionar novos serviços (só criar nova classe)
- ✅ Não precisa modificar código existente (Open/Closed Principle)
- ✅ Cada serviço tem sua lógica isolada
- ✅ Testável individualmente

### 2. BasicAck vs BasicNack

**BasicAck (Acknowledgment positivo):**
```csharp
channel.BasicAck(ea.DeliveryTag, false);
```
- Confirma que a mensagem foi processada com **sucesso**
- RabbitMQ **remove** a mensagem da fila
- Garante "at-least-once delivery"

**BasicNack (Negative acknowledgment):**
```csharp
channel.BasicNack(ea.DeliveryTag, false, false);
                                        // ↑ requeue=false
```
- Indica que houve **erro** no processamento
- `requeue=false`: **descarta** a mensagem (não tenta novamente)
- `requeue=true`: coloca de volta na fila para retry

**Por que requeue=false no projeto?**
- Evita **loop infinito** de mensagens inválidas
- Se o payload está errado, não adianta tentar novamente
- Em produção, mensagens com erro iriam para uma "Dead Letter Queue"

### 3. Fire-and-Forget vs Request-Reply

**Request-Reply (RPC):**
```
Cliente → [Requisição] → Servidor
Cliente ← [Resposta]   ← Servidor
Cliente aguarda resposta
```
- Cliente **bloqueia** até receber resposta ou timeout
- Usa fila temporária exclusiva
- Usa CorrelationId
- Exemplo: operações críticas que precisam confirmação

**Fire-and-Forget (Async):**
```
Cliente → [Mensagem] → Servidor
Cliente continua execução imediatamente
```
- Cliente **não aguarda** resposta
- Não cria fila de resposta
- Sem CorrelationId necessário
- Exemplo: logs, notificações, métricas

**Quando usar cada um?**
| Cenário | Padrão |
|---------|--------|
| Transferência bancária | Request-Reply (precisa confirmar) |
| Log de auditoria | Fire-and-Forget (só registrar) |
| Consulta de saldo | Request-Reply (precisa do valor) |
| Envio de email | Fire-and-Forget (não bloqueia usuário) |
| Cálculo matemático | Request-Reply (precisa do resultado) |
| Incremento de contador | Fire-and-Forget (eventual consistency) |

### 4. Por que duas filas separadas?

**fila_rpc:**
- Para operações síncronas
- Servidor **deve** responder
- Cliente aguarda

**fila_async:**
- Para operações assíncronas
- Servidor **não responde**
- Cliente não aguarda

**Por que não usar uma única fila?**
- ❌ Servidor não saberia se deve responder ou não
- ❌ Cliente não saberia se deve aguardar ou não
- ❌ Misturaria responsabilidades
- ✅ Separação clara de padrões de comunicação

---

## 🐳 DOCKER E CONTAINERIZAÇÃO

### Por que usar Docker?

**Problemas que resolve:**
- ❌ "Funciona na minha máquina"
- ❌ Dependências conflitantes
- ❌ Configuração complexa de ambiente
- ❌ Difícil de replicar setup

**Vantagens:**
- ✅ Ambiente idêntico em qualquer máquina
- ✅ RabbitMQ já configurado e pronto
- ✅ Um comando para subir tudo
- ✅ Isolamento de recursos

### Arquitetura Docker Compose

```yaml
services:
  rabbitmq:          # Broker de mensagens
    - Porta 5672: AMQP (protocolo de mensageria)
    - Porta 15672: Interface web de gerenciamento
    - Healthcheck: valida se está pronto

  server:            # Servidor RPC
    - Aguarda RabbitMQ ficar healthy
    - Conecta automaticamente
    - Volume compartilhado para file.txt
```

### Healthcheck - Por que é importante?

**Problema sem healthcheck:**
```
1. Docker sobe RabbitMQ
2. Docker sobe Servidor
3. Servidor tenta conectar → ERRO! (RabbitMQ ainda iniciando)
```

**Solução com healthcheck:**
```yaml
healthcheck:
  test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
  interval: 10s    # Testa a cada 10 segundos
  timeout: 5s
  retries: 5       # 5 tentativas

depends_on:
  rabbitmq:
    condition: service_healthy  # Só inicia quando RabbitMQ OK
```

**O que acontece:**
1. Docker sobe RabbitMQ
2. Docker testa se RabbitMQ responde a ping
3. Quando healthy, **então** sobe o Servidor
4. Servidor conecta com sucesso

### Reconexão Automática no Servidor

Mesmo com healthcheck, o servidor implementa retry:

```csharp
int tentativas = 0;
int maxTentativas = 10;

while (tentativas < maxTentativas) {
    try {
        var factory = new ConnectionFactory() {
            HostName = "rabbitmq"
        };
        var connection = factory.CreateConnection();
        break;  // Sucesso!
    }
    catch (Exception ex) {
        tentativas++;
        Thread.Sleep(3000);  // Aguarda 3s antes de tentar novamente
    }
}
```

**Por que?**
- Dupla proteção (healthcheck + retry no código)
- Funciona mesmo sem Docker
- Resiliência em caso de queda temporária do RabbitMQ

---

## 📊 VARIÁVEIS DE AMBIENTE

### Servidor

| Variável | Padrão | Descrição |
|----------|--------|-----------|
| `RABBITMQ_HOST` | `rabbitmq` | Endereço do broker |
| `QUEUE_RPC` | `fila_rpc` | Nome da fila RPC |
| `QUEUE_ASYNC` | `fila_async` | Nome da fila assíncrona |

### Cliente

| Variável | Padrão | Descrição |
|----------|--------|-----------|
| `RabbitMQ_HOST` | `localhost` | Endereço do broker |
| `QUEUE_RPC` | `fila_rpc` | Nome da fila RPC |
| `RPC_TIMEOUT` | `5000` | Timeout em ms |

**Por que usar variáveis de ambiente?**
- ✅ Configuração sem recompilar
- ✅ Diferentes ambientes (dev, prod)
- ✅ Segurança (senhas não no código)
- ✅ Flexibilidade

---

## 🎬 DEMONSTRAÇÃO PRÁTICA

### Como rodar o projeto

**Opção 1: Com Docker (Recomendado)**
```bash
# Sobe tudo (RabbitMQ + Servidor)
docker-compose up -d

# Verifica se está rodando
docker ps

# Roda o cliente (fora do Docker)
cd client
dotnet run
```

**Opção 2: Sem Docker (Local)**
```bash
# Terminal 1: RabbitMQ (precisa estar instalado)
rabbitmq-server

# Terminal 2: Servidor
cd server
dotnet run

# Terminal 3: Cliente
cd client
dotnet run
```

### Teste das 3 operações

**1. Mensagem de texto**
```
Opção: 1
→ Digite a mensagem: Teste de comunicação RPC
[RESPOSTA]
[14:30:25] Servidor recebeu: "Teste de comunicação RPC" (Tamanho: 25 caracteres)
```

**2. Arquivo**
```
Opção: 2
→ Digite o texto para salvar: Registro de teste
[RESPOSTA]
✓ Conteúdo salvo em 'file.txt'. Tamanho total: 256 bytes
```

Verificar arquivo:
```bash
cat server/file.txt
# Saída:
# [2026-05-06 14:30:25] Registro de teste
```

**3. Operação matemática**
```
Opção: 3
Operação: 5  (Potência)
Primeiro número: 2
Segundo número: 10
[RESPOSTA]
Potência: 2 e 10 = 1024.00
```

**4. Mensagem assíncrona**
```
Opção: 4
→ Mensagem para envio assíncrono: Log do sistema
[OK] Mensagem enviada para processamento assíncrono
(Não aguarda resposta)
```

No servidor você verá:
```
[ASYNC] Processando mensagem assíncrona: msg
[ASYNC] Payload: Log do sistema
[ASYNC] Resultado: [14:30:25] Servidor recebeu: "Log do sistema" (Tamanho: 14 caracteres)
```

### Acesso à Interface Web do RabbitMQ

```
URL: http://localhost:15672
Usuário: guest
Senha: guest
```

**O que mostrar:**
1. Aba **Queues**: Ver `fila_rpc` e `fila_async`
2. Clicar em uma fila: Ver estatísticas de mensagens
3. **Get messages**: Ver mensagens na fila (se houver)
4. Aba **Connections**: Ver cliente e servidor conectados

---

## ❓ PERGUNTAS FREQUENTES E RESPOSTAS

### "Por que não usar HTTP/REST em vez de RabbitMQ?"

**HTTP/REST:**
- ✅ Mais simples
- ❌ Cliente precisa saber endereço do servidor
- ❌ Se servidor cair, requisição perde
- ❌ Bloqueante (aguarda resposta)
- ❌ Difícil escalar múltiplos servidores

**RabbitMQ:**
- ✅ Desacoplamento total
- ✅ Mensagens persistidas (não se perdem)
- ✅ Fácil adicionar múltiplos consumidores
- ✅ Assíncrono por padrão
- ✅ Controle fino de confirmações (ack/nack)

**Casos de uso ideais para RabbitMQ:**
- Sistemas distribuídos
- Microserviços
- Processamento assíncrono
- Workqueues
- Pub/Sub

### "O que acontece se o servidor cair no meio de uma requisição?"

**Cenário:**
1. Cliente envia mensagem
2. Servidor recebe mas **não** envia BasicAck ainda
3. Servidor cai (crash)

**O que acontece:**
- RabbitMQ detecta que a conexão do servidor caiu
- Mensagem **volta** para a fila (requeue automático)
- Quando servidor voltar, processa a mensagem novamente
- Cliente aguarda até timeout, depois retorna erro

**Resiliência:**
- ✅ Mensagem não se perde
- ✅ Reprocessamento automático
- ✅ Cliente recebe feedback (timeout)

### "Como funciona o timeout no cliente?"

```csharp
int waited = 0;
int timeout = 5000;  // 5 segundos

while (response == null && waited < timeout) {
    Thread.Sleep(100);   // Aguarda 100ms
    waited += 100;       // Incrementa contador
}

if (response == null) {
    return "[TIMEOUT] Tempo de resposta do servidor Excedido";
}
```

**Comportamento:**
- Verifica a cada 100ms se recebeu resposta
- Se passar 5 segundos (5000ms) sem resposta → timeout
- Configurável via `RPC_TIMEOUT`

### "Por que o cliente usa fila temporária em vez de fila fixa?"

**Opção 1: Fila fixa (NÃO usado)**
```
Cliente1 → fila_rpc → Servidor
Cliente1 ← fila_resposta_fixa ← Servidor
Cliente2 ← fila_resposta_fixa ← Servidor  ❌ PROBLEMA!
```
- Cliente1 pode receber resposta do Cliente2
- Precisa filtrar por CorrelationId
- Fila acumula respostas antigas

**Opção 2: Fila temporária exclusiva (USADO)**
```
Cliente1 → fila_rpc → Servidor
Cliente1 ← amq.gen-abc123 ← Servidor  ✅
Cliente2 ← amq.gen-xyz789 ← Servidor  ✅
```
- Cada cliente tem SUA fila
- Impossível receber resposta de outro
- Fila apaga automaticamente
- Mais limpo e seguro

### "O que é e para que serve o padrão Strategy?"

**Definição:**
Padrão de projeto que define uma família de algoritmos, encapsula cada um deles e os torna intercambiáveis.

**Problema resolvido:**
```csharp
// SEM Strategy - código ruim
if (operation == "msg") {
    // lógica de mensagem aqui
} else if (operation == "file") {
    // lógica de arquivo aqui
} else if (operation == "calc") {
    // lógica de cálculo aqui
}
// Difícil de manter e estender
```

**COM Strategy - código bom:**
```csharp
// 1. Interface comum
public interface IOperationService {
    Task<string> ExecuteAsync(string payload);
}

// 2. Cada operação em sua classe
public class MessageService : IOperationService { ... }
public class FileService : IOperationService { ... }
public class MathService : IOperationService { ... }

// 3. Uso polimórfico
var service = _services[operation];
return await service.ExecuteAsync(payload);
```

**Benefícios:**
- ✅ Fácil adicionar nova operação (nova classe)
- ✅ Cada serviço tem responsabilidade única
- ✅ Testável separadamente
- ✅ Segue princípios SOLID

### "Por que usar async/await no FileService?"

**Sem async (bloqueante):**
```csharp
File.AppendAllText(path, content);  // Bloqueia thread até terminar
// Thread fica parada esperando disco escrever
// Em alta carga, esgota threads disponíveis
```

**Com async (não-bloqueante):**
```csharp
await File.AppendAllTextAsync(path, content);  // Libera thread
// Thread volta para pool
// Quando I/O termina, continua execução
// Melhor uso de recursos
```

**Benefícios:**
- ✅ Maior throughput (mais requisições simultâneas)
- ✅ Melhor uso de recursos (menos threads)
- ✅ Escalabilidade

---

## 🎯 PONTOS-CHAVE PARA DESTACAR NA APRESENTAÇÃO

### 1. Arquitetura Moderna
- ✅ Microserviços
- ✅ Event-driven
- ✅ Desacoplamento
- ✅ Containerização

### 2. Padrões de Projeto
- ✅ RPC (Request-Reply)
- ✅ Fire-and-Forget
- ✅ Strategy Pattern
- ✅ IDisposable (RAII)

### 3. Boas Práticas
- ✅ Tratamento de erros em múltiplas camadas
- ✅ Validação de entrada
- ✅ Código assíncrono onde necessário
- ✅ Configuração via variáveis de ambiente
- ✅ Reconexão automática

### 4. Funcionalidades Completas
- ✅ 3 operações distintas (texto, arquivo, matemática)
- ✅ 7 operações matemáticas
- ✅ Persistência de dados
- ✅ Timeout configurável
- ✅ Interface de gerenciamento (RabbitMQ UI)

### 5. Resiliência
- ✅ Healthcheck no Docker
- ✅ Retry automático de conexão
- ✅ Acknowledgment de mensagens
- ✅ Filas temporárias
- ✅ Tratamento de exceções

---

## 📝 ROTEIRO SUGERIDO DE APRESENTAÇÃO

### 1. Introdução (2 min)
- Apresentar o problema: comunicação entre sistemas distribuídos
- Apresentar a solução: RabbitMQ como middleware
- Mostrar arquitetura geral

### 2. Conceitos Fundamentais (3 min)
- O que é RabbitMQ
- Padrão RPC vs Fire-and-Forget
- Por que usar mensageria

### 3. Implementação (5 min)
- Mostrar código do Cliente (RpcClient.cs)
- Mostrar código do Servidor (RpcServer.cs)
- Explicar padrão Strategy
- Mostrar um serviço (MathService.cs)

### 4. Demonstração Prática (5 min)
- Rodar docker-compose
- Executar cliente
- Testar as 3 operações
- Mostrar interface web do RabbitMQ
- Mostrar arquivo persistido

### 5. Aspectos Técnicos (3 min)
- Docker Compose e containerização
- Variáveis de ambiente
- Tratamento de erros
- Resiliência

### 6. Conclusão (2 min)
- Requisitos atendidos
- Aprendizados
- Possíveis melhorias futuras
- Perguntas

---

## 🚀 POSSÍVEIS MELHORIAS FUTURAS (se perguntarem)

### Funcionalidades
- [ ] Autenticação e autorização
- [ ] Criptografia de mensagens
- [ ] Dead Letter Queue para mensagens com erro
- [ ] Múltiplos servidores (load balancing)
- [ ] Métricas e monitoramento (Prometheus)
- [ ] Circuit breaker para resiliência

### Código
- [ ] Testes unitários
- [ ] Testes de integração
- [ ] Logs estruturados (Serilog)
- [ ] Injeção de dependências
- [ ] Configuration provider (.NET)

### Infraestrutura
- [ ] Deploy em Kubernetes
- [ ] CI/CD pipeline
- [ ] Ambientes separados (dev/staging/prod)
- [ ] Backup de mensagens
- [ ] Alta disponibilidade do RabbitMQ (cluster)

---

## 🎓 CONCLUSÃO

Você implementou com sucesso:

✅ Sistema distribuído com comunicação via mensageria
✅ Padrões RPC e Fire-and-Forget
✅ 3 operações funcionais completas
✅ Tratamento robusto de erros
✅ Containerização com Docker
✅ Código limpo e bem estruturado
✅ Arquitetura escalável e extensível

**Este projeto demonstra:**
- Compreensão de sistemas distribuídos
- Conhecimento de padrões de mensageria
- Capacidade de implementar soluções resilientes
- Uso de boas práticas de desenvolvimento
- Domínio de tecnologias modernas (.NET, Docker, RabbitMQ)

**Boa apresentação! 🎤**

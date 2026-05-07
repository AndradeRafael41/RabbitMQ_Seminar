# Sistema RPC e Publish-Subscribe com RabbitMQ

## Descrição

Este projeto implementa um sistema de comunicação distribuída utilizando RabbitMQ como middleware de mensageria. O sistema demonstra dois padrões fundamentais de comunicação:

1. **RPC (Remote Procedure Call)**: Comunicação síncrona request-response entre cliente e servidor
2. **Publish-Subscribe**: Broadcasting assíncrono de mensagens para múltiplos consumidores

## Tecnologias Utilizadas

- **Linguagem**: C# (.NET 8)
- **Middleware**: RabbitMQ 3 com Management Plugin
- **Biblioteca**: RabbitMQ.Client 6.8.1
- **Containerização**: Docker (apenas para RabbitMQ Broker)
- **Protocolo**: AMQP (Advanced Message Queuing Protocol)
- **Sistemas Operacionais**: Windows (Cliente), Linux (Servidor e Broker)

## Arquitetura do Sistema

### Componentes

O sistema é executado em 3 computadores separados:

**Computador 1: Cliente (Windows)**
- Aplicação .NET 8 console
- Sistema operacional: Windows
- Publica mensagens RPC e Pub/Sub
- Aguarda respostas de chamadas RPC
- Não aguarda resposta em Pub/Sub
- Conecta-se ao RabbitMQ remotamente

**Computador 2: Servidor (Linux)**
- Aplicação .NET 8 console
- Sistema operacional: Linux
- Consome mensagens da fila RPC
- Subscreve ao exchange Pub/Sub
- Processa requisições e retorna respostas
- Conecta-se ao RabbitMQ remotamente

**Computador 3: RabbitMQ Broker (Linux com Docker)**
- Sistema operacional: Linux
- RabbitMQ executado em container Docker
- Gerencia filas e exchanges
- Roteia mensagens entre cliente e servidores
- Interface web de gerenciamento na porta 15672
- Aceita conexões remotas nas portas 5672 (AMQP) e 15672 (Management)

### Padrões de Comunicação Implementados

#### 1. RPC (Remote Procedure Call)

O padrão RPC permite que o cliente execute procedimentos remotos no servidor e aguarde a resposta, simulando uma chamada de função local.

**Funcionamento:**

1. Cliente cria uma fila de resposta exclusiva e temporária
2. Cliente publica mensagem na fila RPC com:
   - CorrelationId: identificador único da requisição
   - ReplyTo: nome da fila de resposta
   - Payload: dados da requisição serializados em JSON
3. Servidor consome mensagem da fila RPC
4. Servidor processa a requisição
5. Servidor publica resposta na fila indicada em ReplyTo
6. Cliente recebe resposta correlacionando pelo CorrelationId
7. Cliente retorna resultado ao usuário

**Características:**
- Comunicação síncrona do ponto de vista do cliente
- Timeout configurável (padrão: 5 segundos)
- Apenas um servidor processa cada requisição
- Garante resposta ao cliente

**Fila utilizada:**
- `fila_rpc`: fila persistente para requisições RPC

#### 2. Publish-Subscribe

O padrão Pub/Sub permite broadcasting de mensagens para múltiplos consumidores simultaneamente usando Fanout Exchange.

**Funcionamento:**

1. Exchange Fanout "async_pubsub" é declarado
2. Cada servidor cria fila exclusiva e temporária
3. Cada servidor vincula (bind) sua fila ao exchange
4. Cliente publica mensagem no exchange (sem routing key)
5. RabbitMQ duplica mensagem para TODAS as filas vinculadas
6. Todos os servidores recebem e processam a mesma mensagem
7. Não há resposta ao cliente

**Características:**
- Comunicação assíncrona (fire-and-forget)
- Broadcasting para múltiplos consumidores
- Todos os servidores conectados processam a mensagem
- Filas exclusivas são deletadas quando servidor desconecta
- Ideal para notificações, logs e eventos distribuídos

**Exchange utilizado:**
- `async_pubsub`: Fanout Exchange para broadcasting

## Como Executar o Projeto

O projeto é executado em 3 computadores distintos conectados na mesma rede.

### Pré-requisitos

**Todos os computadores:**
- Conectados na mesma rede local
- Firewall configurado para permitir comunicação nas portas 5672 e 15672

**Computador 1 (Cliente - Windows):**
- .NET 8 SDK instalado
- Windows 10 ou superior

**Computador 2 (Servidor - Linux):**
- .NET 8 SDK instalado
- Distribuição Linux (Ubuntu, Debian, etc.)

**Computador 3 (RabbitMQ - Linux):**
- Docker instalado
- Distribuição Linux (Ubuntu, Debian, etc.)

### Passo 1: Configurar RabbitMQ (Computador 3 - Linux)

**Executar RabbitMQ com Docker:**

```bash
docker run -d \
  --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=guest \
  -e RABBITMQ_DEFAULT_PASS=guest \
  rabbitmq:3-management
```

**Verificar se o container está rodando:**

```bash
docker ps | grep rabbitmq
```

**Anotar o endereço IP do computador:**

```bash
ip addr show | grep inet
# Anote o IP (ex: 192.168.1.100)
```

**Verificar logs do RabbitMQ (opcional):**

```bash
docker logs rabbitmq
```

### Passo 2: Executar Servidor (Computador 2 - Linux)

**Navegar até o diretório do servidor:**

```bash
cd server
```

**Configurar endereço do RabbitMQ:**

```bash
export RABBITMQ_HOST="192.168.1.100"  # IP do Computador 3
export RABBITMQ_USER="guest"           # Usuário RabbitMQ
export RABBITMQ_PASS="guest"           # Senha RabbitMQ
```

**Executar o servidor:**

```bash
dotnet run
```

O servidor deve mostrar:
```
Conectado ao RabbitMQ!

╔═══════════════════════════════════════════════╗
║     SERVIDOR RPC + PUB/SUB INICIADO         ║
╚═══════════════════════════════════════════════╝

→ RPC: fila_rpc
→ Pub/Sub: async_pubsub (broadcasting)
```

### Passo 3: Executar Cliente (Computador 1 - Windows)

**Navegar até o diretório do cliente:**

```powershell
cd client
```

**Configurar endereço do RabbitMQ:**

```powershell
$env:RabbitMQ_HOST="192.168.1.100"  # IP do Computador 3
```

**Executar o cliente:**

```powershell
dotnet run
```

O menu interativo será exibido:

```
╔══════════════════════════════════════╗
║      CLIENTE RPC - RabbitMQ         ║
╚══════════════════════════════════════╝

[OPERAÇÕES RPC - Com Resposta]
  1 - Enviar mensagem de texto
  2 - Escrever em arquivo no servidor
  3 - Operações matemáticas

[PUBLISH-SUBSCRIBE - Broadcasting]
  4 - Publicar mensagem para TODOS os servidores

[SISTEMA]
  0 - Sair
```

### Testando Múltiplos Servidores (Pub/Sub)

Para demonstrar o padrão Publish-Subscribe com múltiplos servidores:

**No Computador 2 (Servidor - Linux), abra múltiplos terminais:**

**Terminal 1:**
```bash
export RABBITMQ_HOST="192.168.1.100"
cd server
dotnet run
```

**Terminal 2:**
```bash
export RABBITMQ_HOST="192.168.1.100"
cd server
dotnet run
```

**Terminal 3:**
```bash
export RABBITMQ_HOST="192.168.1.100"
cd server
dotnet run
```

**No Computador 1 (Cliente - Windows):**
```powershell
cd client
dotnet run
# Selecione opção 4 (Publish-Subscribe)
```

Todos os servidores receberão e processarão a mesma mensagem simultaneamente.

### Acessar Interface de Gerenciamento

De qualquer computador na rede, acesse:

```
http://192.168.1.100:15672
```

(Substitua pelo IP real do Computador 3)

- Usuário: guest
- Senha: guest

Na interface você pode:
- Visualizar filas ativas (fila_rpc)
- Monitorar exchange (async_pubsub)
- Ver mensagens em trânsito
- Acompanhar conexões ativas (cliente e servidor)
- Verificar bindings entre filas e exchanges

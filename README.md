# Sistema RPC Distribuído com RabbitMQ

## 🚀 Início Rápido

**Quer começar a usar agora?** Leia o **[GUIA_PRATICO.md](GUIA_PRATICO.md)** para instruções passo a passo!

- ✅ Como rodar em 1 PC só (desenvolvimento local)
- ✅ Como rodar em 2 PCs diferentes (distribuído)
- ✅ Explicação dos scripts start-server e start-client
- ✅ Solução de problemas comuns

---

## 📋 Descrição do Projeto

Este projeto demonstra a comunicação entre processos distribuídos utilizando **RabbitMQ** como middleware de mensageria. O sistema implementa um padrão RPC (Remote Procedure Call) e processamento assíncrono de mensagens.

### Requisitos Atendidos

✅ **Cliente e Servidor distribuídos** comunicando-se via RabbitMQ
✅ **Três operações básicas implementadas:**
- 🗨️ Resposta a mensagem de texto
- 📝 Alteração de arquivo texto no servidor
- 🧮 Cálculo de funções matemáticas (7 operações disponíveis)

✅ **Recursos adicionais:**
- 📨 Filas de mensagens (RPC e Assíncrona)
- ⚡ Processamento assíncrono (Fire-and-forget)
- 🔄 Tratamento robusto de erros
- 🐳 Containerização com Docker

---

## 🏗️ Arquitetura

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐
│   Cliente   │ ───RPC───│  RabbitMQ   │ ───RPC───│  Servidor   │
│   (.NET 9)  │          │  Middleware │          │  (.NET 8)   │
│             │ ─Async──│             │ ─Async──│             │
└─────────────┘         └──────────────┘         └─────────────┘
     ↑                                                   ↓
     │                                              ┌─────────┐
     │                                              │Services │
     │                                              ├─────────┤
     └──────────────────────────────────────────────│  MSG    │
                   Requisição/Resposta               │  FILE   │
                                                     │  MATH   │
                                                     └─────────┘
```

---

## 🚀 Funcionalidades

### Operações RPC (Síncrono - com resposta)

#### 1️⃣ Mensagem de Texto
Envia uma mensagem e recebe confirmação do servidor com timestamp.

**Exemplo:**
```
→ Cliente: "Olá Servidor!"
← Servidor: "[14:30:25] Servidor recebeu: "Olá Servidor!" (Tamanho: 14 caracteres)"
```

#### 2️⃣ Alteração de Arquivo
Escreve conteúdo em um arquivo no servidor com timestamp.

**Exemplo:**
```
→ Cliente: "Dados importantes"
← Servidor: "✓ Conteúdo salvo no arquivo. Tamanho total: 256 bytes"
```

#### 3️⃣ Operações Matemáticas
Realiza cálculos matemáticos no servidor:

| Operação      | Comando    | Exemplo        | Resultado      |
|---------------|------------|----------------|----------------|
| Soma          | `soma`     | soma,5,3       | Soma: 5 e 3 = 8.00 |
| Subtração     | `sub`      | sub,10,4       | Subtração: 10 e 4 = 6.00 |
| Multiplicação | `mult`     | mult,7,6       | Multiplicação: 7 e 6 = 42.00 |
| Divisão       | `div`      | div,15,3       | Divisão: 15 e 3 = 5.00 |
| Potência      | `pot`      | pot,2,8        | Potência: 2 e 8 = 256.00 |
| Módulo        | `mod`      | mod,10,3       | Módulo: 10 e 3 = 1.00 |
| Raiz n-ésima  | `raiz`     | raiz,27,3      | Raiz: 27 e 3 = 3.00 |

### Operações Assíncronas (Fire-and-forget)

#### 4️⃣ Mensagem Assíncrona
Envia mensagem sem aguardar resposta, processada em segundo plano.

---

## 🛠️ Tecnologias Utilizadas

- **Linguagem:** C# (.NET 8 para servidor, .NET 9 para cliente)
- **Middleware:** RabbitMQ 3
- **Biblioteca:** RabbitMQ.Client 6.8.1
- **Containerização:** Docker & Docker Compose
- **Padrões:** RPC, Pub/Sub, Message Queue

---

## 📦 Estrutura do Projeto

```
RabbitMQ_Seminar/
├── client/                      # Aplicação Cliente
│   ├── Program.cs              # Interface do usuário
│   ├── RpcClient.cs            # Lógica RPC e Async
│   ├── client.csproj
│   └── models/
│       └── RequestMessage.cs   # Modelo de requisição
│
├── server/                      # Aplicação Servidor
│   ├── Program.cs              # Inicialização
│   ├── RpcServer.cs            # Gerenciador de filas
│   ├── Dockerfile
│   ├── server.csproj
│   ├── Models/
│   │   └── RequestMessage.cs
│   └── Services/
│       ├── interface/
│       │   └── IOperationService.cs
│       ├── MessageService.cs   # Processamento de mensagens
│       ├── FileService.cs      # Operações de arquivo
│       └── MathService.cs      # Cálculos matemáticos
│
├── docker-compose.yml           # Orquestração dos serviços
└── README.md
```

---

## 🔧 Instalação e Execução

### Pré-requisitos

- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Execução Rápida (1 PC)

```powershell
# Terminal 1 - Servidor
docker-compose up --build

# Terminal 2 - Cliente (em outra janela)
cd client
dotnet run
```

**Pronto!** O cliente conecta automaticamente em `localhost`.

### Execução em 2 PCs Diferentes

**📖 Leia o [GUIA_PRATICO.md](GUIA_PRATICO.md) para instruções detalhadas!**

Resumo:
1. **PC 1:** Execute `docker-compose up` e anote o IP
2. **PC 2:** Configure `$env:RabbitMQ_HOST="IP_DO_PC1"` e execute o cliente

### Scripts de Automação (Opcionais)

Os scripts `start-server` e `start-client` facilitam a execução:

```powershell
# Servidor (mostra IPs disponíveis e inicia)
.\start-server.ps1

# Cliente (pergunta configurações e inicia)
.\start-client.ps1
```

**Quando usar os scripts?**
- ✅ Para testes rápidos com configuração guiada
- ✅ Quando não lembra os comandos
- ❌ Não use em produção ou scripts automatizados

**Detalhes completos no [GUIA_PRATICO.md](GUIA_PRATICO.md)**

---

## 🖥️ Interface do Cliente

```
╔══════════════════════════════════════╗
║      CLIENTE RPC - RabbitMQ         ║
╚══════════════════════════════════════╝

[OPERAÇÕES RPC - Com Resposta]
  1 - Enviar mensagem de texto
  2 - Escrever em arquivo no servidor
  3 - Operações matemáticas

[OPERAÇÕES ASSÍNCRONAS - Sem Resposta]
  4 - Enviar mensagem async (Fire-and-forget)

[SISTEMA]
  0 - Sair
─────────────────────────────────────

Opção: _
```

---

## 🔍 Monitoramento RabbitMQ

Acesse: `http://localhost:15672` (ou `http://IP_DO_SERVIDOR:15672`)

**Login:** guest / guest

Explore:
- **Queues:** Veja `fila_rpc` e `fila_async` em ação
- **Connections:** Veja cliente e servidor conectados
- **Mensagens processadas em tempo real**

---

## ⚙️ Configuração Avançada

### Variáveis de Ambiente

### Cliente

| Variável       | Padrão      | Descrição                    |
|----------------|-------------|------------------------------|
| RabbitMQ_HOST  | localhost   | Endereço do RabbitMQ         |
| QUEUE_RPC      | fila_rpc    | Nome da fila RPC             |
| QUEUE_ASYNC    | fila_async  | Nome da fila assíncrona      |
| RPC_TIMEOUT    | 5000        | Timeout RPC em ms            |

### Servidor

| Variável       | Padrão      | Descrição                    |
|----------------|-------------|------------------------------|
| RABBITMQ_HOST  | rabbitmq    | Endereço do RabbitMQ         |
| QUEUE_RPC      | fila_rpc    | Nome da fila RPC             |
| QUEUE_ASYNC    | fila_async  | Nome da fila assíncrona      |
| FILE_PATH      | /app/dados.txt | Caminho do arquivo de dados |

**Como usar:**
```powershell
# Windows
$env:RabbitMQ_HOST="192.168.1.100"
$env:RPC_TIMEOUT="10000"

# Linux/Mac
export RabbitMQ_HOST="192.168.1.100"
export RPC_TIMEOUT="10000"
```

---

## 🐛 Solução de Problemas

**Problemas comuns?** Consulte o **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** com 19+ problemas e soluções!

Problemas rápidos:

```powershell
# Cliente não conecta?
docker ps | grep rabbitmq  # Verifica se está rodando

# Timeout?
$env:RPC_TIMEOUT="10000"  # Aumenta timeout

# Rebuild completo
docker-compose down -v
docker-compose up --build
```

---

## 📚 Documentação Adicional

- **[GUIA_PRATICO.md](GUIA_PRATICO.md)** - Como usar o sistema (leia primeiro!)
- **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** - Solução de problemas detalhada
- **[ARQUITETURA_TECNICA.md](ARQUITETURA_TECNICA.md)** - Detalhes técnicos e padrões
- **[MELHORIAS_FUTURAS.md](MELHORIAS_FUTURAS.md)** - Ideias para extensão

---

## 🎯 Comandos Úteis

```powershell
# Ver logs do servidor
docker logs rabbitmq_rpc_server

# Ver logs em tempo real
docker logs -f rabbitmq_rpc_server

# Parar tudo
docker-compose down

# Listar filas no RabbitMQ
docker exec rabbitmq_server rabbitmqctl list_queues

# Verificar arquivo salvo
docker exec rabbitmq_rpc_server cat /app/dados.txt
```

---

## 📝 Notas Importantes

1. **Comunicação entre máquinas:** Certifique-se de que firewall está liberado (portas 5672 e 15672)
2. **Primeiro uso:** Leia o [GUIA_PRATICO.md](GUIA_PRATICO.md) para entender os cenários
3. **Desenvolvimento:** Use 1 PC só para testes rápidos
4. **Demonstração:** Use 2 PCs para mostrar comunicação distribuída real

---

## 🔗 Referências

- [RabbitMQ Official Documentation](https://www.rabbitmq.com/documentation.html)
- [RabbitMQ Tutorials](https://www.rabbitmq.com/getstarted.html)
- [.NET RabbitMQ Client](https://www.rabbitmq.com/dotnet.html)

---

**Última atualização:** Abril 2026

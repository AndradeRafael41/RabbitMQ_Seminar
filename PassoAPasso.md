# Passo a passo para a stack

```text
PC 1: Linux Server  -> roda o projeto server
PC 2: Linux Broker  -> roda RabbitMQ
PC 3: Windows Client -> roda o projeto client
```

Substituam abaixo:

```text
IP_BROKER = IP do computador Linux que roda RabbitMQ
IP_SERVER = IP do computador Linux que roda o server
IP_CLIENT = IP do Windows client
```

**1. No PC Broker Linux**

Na pasta do projeto:

```bash
cd ~/RD/RabbitMQ_Seminar
docker compose up rabbitmq
```

Verifique se subiu:

```bash
docker ps
```

Abrir no navegador:

```text
http://IP_BROKER:15672
```

Login:

```text
guest
guest
```

Portas necessárias no broker:

```text
5672  -> comunicação RabbitMQ
15672 -> painel web RabbitMQ
```

Se tiver firewall ativo:

```bash
sudo ufw allow 5672/tcp
sudo ufw allow 15672/tcp
```

**2. No PC Server Linux**

Na pasta do projeto:

```bash
cd ~/RD/RabbitMQ_Seminar/server
export RABBITMQ_HOST="200.235.92.106"
export RABBITMQ_USER="server"
export RABBITMQ_PASS="server"
dotnet run
```

Exemplo:

```bash
export RABBITMQ_HOST="200.235.88.10"
dotnet run
```

O servidor deve mostrar algo como:

```text
Conectado ao RabbitMQ!
SERVIDOR RPC RABBITMQ INICIADO
Fila RPC: fila_rpc
Fila Async: fila_async
```

**3. No PC Client Windows**

No PowerShell, dentro da pasta do projeto:

```powershell
cd .\RabbitMQ_Seminar\client
$env:RabbitMQ_HOST="IP_BROKER"
dotnet run
```

Exemplo:

```powershell
$env:RabbitMQ_HOST="200.235.88.10"
dotnet run
```

Depois use o menu do cliente normalmente.

**4. Fluxo esperado**

```text
Windows Client
    -> envia mensagem para IP_BROKER:5672
RabbitMQ Broker Linux
    -> entrega mensagem na fila fila_rpc
Linux Server
    -> consome, processa e responde
RabbitMQ Broker Linux
    -> devolve resposta ao Client
Windows Client
    -> mostra resultado no terminal
```

**5. Testes rápidos antes de rodar**

No Server Linux:

```bash
ping IP_BROKER
nc -vz IP_BROKER 5672
```

No Windows Client:

```powershell
ping IP_BROKER
Test-NetConnection IP_BROKER -Port 5672
Test-NetConnection IP_BROKER -Port 15672
```

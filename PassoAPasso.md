*1. No PC Broker Linux*

Na pasta do projeto:

bash
cd ~/RD/RabbitMQ_Seminar
docker compose up rabbitmq


Verifique se subiu:

bash
docker ps


Abrir no navegador:

text
http://IP_BROKER:15672


Login:

text
guest
guest


Portas necessárias no broker:

text
5672  -> comunicação RabbitMQ
15672 -> painel web RabbitMQ


Se tiver firewall ativo:

bash
sudo ufw allow 5672/tcp
sudo ufw allow 15672/tcp


*2. No PC Server Linux*

Na pasta do projeto:

bash
cd ~/RD/RabbitMQ_Seminar/server
export RABBITMQ_HOST="200.235.92.106"
export RABBITMQ_USER="server"
export RABBITMQ_PASS="server"
dotnet run


Exemplo:

bash
export RABBITMQ_HOST="200.235.88.10"
dotnet run


O servidor deve mostrar algo como:

text
Conectado ao RabbitMQ!
SERVIDOR RPC RABBITMQ INICIADO
Fila RPC: fila_rpc
Fila Async: fila_async


*3. No PC Client Windows*

No PowerShell, dentro da pasta do projeto:

powershell
cd .\RabbitMQ_Seminar\client
$env:RabbitMQ_HOST="IP_BROKER"
dotnet run


Exemplo:

powershell
$env:RabbitMQ_HOST="200.235.88.10"
dotnet run

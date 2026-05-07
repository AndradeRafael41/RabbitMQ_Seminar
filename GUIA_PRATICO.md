# Guia Prático - Como Usar o Sistema RabbitMQ RPC

## 📖 O que são os scripts de inicialização?

Os scripts `start-server` e `start-client` são facilitadores que automatizam o processo de configuração e execução. **Eles são OPCIONAIS** - você pode executar tudo manualmente se preferir.

### **start-server.ps1 / start-server.sh**

**O que faz:**
1. Verifica se Docker está instalado
2. Mostra os IPs da sua máquina (útil para conectar de outra máquina)
3. Executa `docker-compose up --build`

**Quando usar:**
- ✅ Quando você quer iniciar o servidor RabbitMQ + Servidor RPC rapidamente
- ✅ Quando você não lembra os comandos Docker
- ✅ Para ver os IPs disponíveis da máquina

**Quando NÃO usar:**
- ❌ Se você prefere controle manual com comandos Docker
- ❌ Se você quer rodar em background (o script roda em foreground)

### **start-client.ps1 / start-client.sh**

**O que faz:**
1. Verifica se .NET está instalado
2. Pergunta o endereço do RabbitMQ (localhost ou IP remoto)
3. Pergunta o timeout desejado
4. Configura as variáveis de ambiente
5. Executa o cliente (`dotnet run`)

**Quando usar:**
- ✅ Quando você não quer digitar variáveis de ambiente manualmente
- ✅ Quando conectar em um servidor remoto e quer ser guiado
- ✅ Para testes rápidos com configuração interativa

**Quando NÃO usar:**
- ❌ Se você prefere controle total das variáveis de ambiente
- ❌ Em scripts automatizados (use comandos diretos)

---

## 🖥️ Cenário 1: Tudo em UM PC só (Desenvolvimento Local)

Este é o cenário mais simples para testar o sistema.

### **Método 1: Usando os scripts (Mais Fácil)**

#### Passo 1: Iniciar Servidor

```powershell
# Windows PowerShell
.\start-server.ps1
```

```bash
# Linux/Mac
chmod +x start-server.sh
./start-server.sh
```

Você verá algo como:
```
╔══════════════════════════════════════════════╗
║ Servidor RPC RabbitMQ - Script de Startup  ║
╚══════════════════════════════════════════════╝

✓ Docker detectado: Docker version 24.0.5
✓ Docker Compose detectado: Docker Compose version 2.20.2

Informações de Rede:
====================
IPs disponíveis nesta máquina:
  → 192.168.1.50
  → 172.20.10.2

Iniciando servidor com Docker Compose...
```

**Aguarde até ver:**
```
╔════════════════════════════════════════╗
║   SERVIDOR RPC RABBITMQ INICIADO      ║
╚════════════════════════════════════════╝

→ Fila RPC: fila_rpc
→ Fila Async: fila_async

[INFO] Aguardando mensagens...
```

#### Passo 2: Abrir OUTRO terminal e iniciar Cliente

```powershell
# Windows PowerShell - NOVO TERMINAL
.\start-client.ps1
```

Quando perguntar:
```
Endereço do RabbitMQ (padrão: localhost):
```
**Apenas aperte ENTER** (vai usar localhost)

```
Timeout RPC em ms (padrão: 5000):
```
**Apenas aperte ENTER** (vai usar 5000ms)

Pronto! Cliente conectado.

---

### **Método 2: Comandos Manuais (Controle Total)**

#### Terminal 1 - Servidor:

```powershell
# Na raiz do projeto
docker-compose up --build
```

#### Terminal 2 - Cliente:

```powershell
# Navegar até pasta client
cd client

# Executar
dotnet run
```

**Simples assim!** Como você não configurou variáveis de ambiente, o cliente usa `localhost` por padrão.

---

## 🌐 Cenário 2: Cliente em UM PC, Servidor em OUTRO PC

Útil para demonstrar comunicação distribuída real.

### **Pré-requisitos:**
- Ambos PCs na mesma rede (WiFi/Ethernet)
- Firewall liberado nas portas 5672 e 15672

### **PC 1 (Servidor)**

#### Passo 1: Iniciar servidor

```powershell
# Usando script
.\start-server.ps1

# OU manualmente
docker-compose up --build
```

#### Passo 2: Descobrir o IP

O script já mostra, mas se rodou manualmente:

```powershell
# Windows
ipconfig
# Procure por "Endereço IPv4" na interface de rede ativa
# Exemplo: 192.168.1.100
```

```bash
# Linux/Mac
hostname -I
# Ou
ip addr show
# Exemplo: 192.168.1.100
```

#### Passo 3: Liberar Firewall

**Windows:**
```powershell
# Executar como Administrador
New-NetFirewallRule -DisplayName "RabbitMQ AMQP" -Direction Inbound -Port 5672 -Protocol TCP -Action Allow
New-NetFirewallRule -DisplayName "RabbitMQ Management" -Direction Inbound -Port 15672 -Protocol TCP -Action Allow
```

**Linux:**
```bash
sudo ufw allow 5672/tcp
sudo ufw allow 15672/tcp
sudo ufw reload
```

### **PC 2 (Cliente)**

#### Método 1: Usando script

```powershell
.\start-client.ps1
```

Quando perguntar:
```
Endereço do RabbitMQ (padrão: localhost): 192.168.1.100
```
**Digite o IP do PC servidor**

#### Método 2: Manual

```powershell
# Configurar IP do servidor
$env:RabbitMQ_HOST="192.168.1.100"

# Executar cliente
cd client
dotnet run
```

```bash
# Linux/Mac
export RabbitMQ_HOST="192.168.1.100"
cd client
dotnet run
```

---

## 🧪 Como Testar se Está Funcionando

### Teste Rápido:

1. **No cliente**, selecione opção `1` (Mensagem de texto)
2. Digite: `Teste de conexão`
3. Você deve ver: `[RESPOSTA] [HH:MM:SS] Servidor recebeu: "Teste de conexão" (Tamanho: 16 caracteres)`

Se viu isso, **está funcionando!** ✅

### Teste Completo:

```
Opção: 1
→ Digite a mensagem: Hello RabbitMQ
[RESPOSTA]
[14:30:25] Servidor recebeu: "Hello RabbitMQ" (Tamanho: 14 caracteres)

Opção: 2
→ Digite o texto para salvar: Log de teste
[RESPOSTA]
✓ Conteúdo salvo no arquivo. Tamanho total: 45 bytes

Opção: 3
Operação: 1 (Soma)
Primeiro número: 15
Segundo número: 27
[RESPOSTA]
Soma: 15 e 27 = 42.00

Opção: 4
→ Mensagem para envio assíncrono: Teste async
[OK] Mensagem enviada para processamento assíncrono
```

---

## 🔍 Interface Web de Monitoramento

Acesse o RabbitMQ Management:

- **Local:** http://localhost:15672
- **Remoto:** http://192.168.1.100:15672 (substitua pelo IP do servidor)

**Login:**
- Usuário: `guest`
- Senha: `guest`

**O que ver:**
- **Aba "Queues":** Veja as filas `fila_rpc` e `fila_async`
- **Aba "Connections":** Veja cliente e servidor conectados
- **Mensagens processadas em tempo real**

---

## ❓ Perguntas Frequentes

### **P: Preciso usar os scripts?**
**R:** Não! São apenas facilitadores. Você pode executar tudo manualmente com:
- Servidor: `docker-compose up`
- Cliente: `cd client && dotnet run`

### **P: Os scripts modificam algo no meu sistema?**
**R:** Não permanentemente. Eles apenas:
- Configuram variáveis de ambiente temporárias (só para aquela sessão)
- Executam Docker Compose e dotnet run

### **P: Posso rodar múltiplos clientes?**
**R:** Sim! Abra vários terminais e execute o cliente em cada um.

### **P: O que acontece se eu fechar o terminal do servidor?**
**R:** O Docker Compose para. Use `docker-compose up -d` para rodar em background.

### **P: Como paro tudo?**
**R:**
- Servidor: Ctrl+C no terminal (ou `docker-compose down`)
- Cliente: Opção `0` no menu ou Ctrl+C

### **P: Posso rodar o servidor sem Docker?**
**R:** Tecnicamente sim, mas precisaria instalar RabbitMQ manualmente. Docker é mais fácil.

---

## 🚨 Problemas Comuns

### Servidor não inicia

```powershell
# Verificar se Docker está rodando
docker ps

# Se não estiver, iniciar Docker Desktop
# Depois tentar novamente
```

### Cliente não conecta

```powershell
# Verificar se variável está correta
echo $env:RabbitMQ_HOST  # Windows
echo $RabbitMQ_HOST      # Linux

# Deve mostrar "localhost" ou IP do servidor
# Se estiver errado, configurar novamente:
$env:RabbitMQ_HOST="localhost"
```

### Firewall bloqueando (máquinas diferentes)

```powershell
# Testar conectividade
Test-NetConnection -ComputerName 192.168.1.100 -Port 5672

# Se falhar, liberar firewall (ver seção acima)
```

---

## 📊 Resumo dos Comandos

### **Inicialização Rápida (1 PC)**

```powershell
# Terminal 1
docker-compose up

# Terminal 2
cd client
dotnet run
```

### **Inicialização com Scripts (1 PC)**

```powershell
# Terminal 1
.\start-server.ps1

# Terminal 2
.\start-client.ps1
```

### **Inicialização para 2 PCs**

```powershell
# PC Servidor
docker-compose up
# Anotar IP: ipconfig

# PC Cliente
$env:RabbitMQ_HOST="192.168.1.100"  # Substituir pelo IP real
cd client
dotnet run
```

---

## 🎯 Próximos Passos

Depois de testar localmente:

1. ✅ Experimente com 2 PCs diferentes
2. ✅ Teste todas as operações matemáticas
3. ✅ Explore a interface web do RabbitMQ
4. ✅ Veja os logs do servidor: `docker logs rabbitmq_rpc_server`
5. ✅ Leia [TROUBLESHOOTING.md](TROUBLESHOOTING.md) para soluções de problemas

---

**Dica:** Se você só quer testar rapidamente, use os scripts. Se quer entender cada passo ou automatizar, use os comandos manuais.

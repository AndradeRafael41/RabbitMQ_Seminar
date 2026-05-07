# ========== STAGE 1: Build ==========
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copia tudo do contexto (igual ao Dockerfile do server)
COPY . .

# entra na pasta do client
WORKDIR /src/client

# restore + publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# ========== STAGE 2: Runtime ==========
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY --from=build /app/out .

# o client é interativo (Console.ReadLine), então rode o compose com "-it"
ENTRYPOINT ["dotnet", "client.dll"]

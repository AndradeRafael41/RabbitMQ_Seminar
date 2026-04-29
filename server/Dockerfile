FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 👇 copia TUDO primeiro
COPY . .

# 👇 entra na pasta do server
WORKDIR /src/server

# restore agora funciona
RUN dotnet restore

# build
RUN dotnet publish -c Release -o /app/out

# runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "server.dll"]
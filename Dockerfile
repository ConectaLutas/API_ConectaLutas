# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia os arquivos e restaura pacotes
COPY . . 
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copia os arquivos compilados da etapa anterior
COPY --from=build /app/out .

# Define o ambiente como produção dentro do container
ENV ASPNETCORE_ENVIRONMENT=Production

# Expondo a porta padrão da aplicação
EXPOSE 5000

# Comando para iniciar a API
ENTRYPOINT ["dotnet", "PlataformaJiujitsu.dll"]

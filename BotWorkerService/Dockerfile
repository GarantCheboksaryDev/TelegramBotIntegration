FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["BotWorkerService/BotWorkerService.csproj", "BotWorkerService/"]
RUN dotnet restore "BotWorkerService/BotWorkerService.csproj"
COPY . .
WORKDIR "/src/BotWorkerService"
RUN dotnet build "BotWorkerService.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "BotWorkerService.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "BotWorkerService.dll"]
# Этап 1: Сборка приложения (Бэкенд + Фронтенд)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Устанавливаем Node.js (v22, как указано в package.json) для сборки Vue
RUN curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y nodejs

# Копируем файлы проектов для кэширования восстановления пакетов
COPY ["AlgoTrace.slnx", "."]
COPY ["AlgoTrace.Server/AlgoTrace.Server.csproj", "AlgoTrace.Server/"]
COPY ["algotrace.client/algotrace.client.esproj", "algotrace.client/"]
COPY ["algotrace.client/package.json", "algotrace.client/"]
COPY ["algotrace.client/package-lock.json*", "algotrace.client/"]
RUN dotnet restore "AlgoTrace.Server/AlgoTrace.Server.csproj"

# Устанавливаем зависимости фронтенда (кэшируем этот шаг)
WORKDIR "/src/algotrace.client"
RUN npm install

# Копируем весь оставшийся код и собираем проект
WORKDIR "/src"
COPY . .
WORKDIR "/src/AlgoTrace.Server"
RUN dotnet publish "AlgoTrace.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Этап 2: Финальный образ для запуска (только runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AlgoTrace.Server.dll"]
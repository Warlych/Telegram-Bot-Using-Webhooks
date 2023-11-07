FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY ./TelegramBot.Infrastructure/TelegramBot.Infrastructure.csproj ./TelegramBot.Infrastructure/
COPY ./TelegramBot.Application/TelegramBot.Application.csproj ./TelegramBot.Application/
COPY ./TelegramBot/TelegramBot.csproj ./TelegramBot/
COPY ./TelegramBot.sln ./

RUN dotnet restore

COPY . ./
WORKDIR /src/TelegramBot
RUN dotnet build -c Release -o /app/build

FROM base AS runtime
WORKDIR /app
COPY --from=build /app/build ./
ENTRYPOINT ["dotnet", "TelegramBot.dll"]
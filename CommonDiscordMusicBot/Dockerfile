#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["CommonDiscordMusicBot/CommonDiscordMusicBot.csproj", "CommonDiscordMusicBot/"]
RUN dotnet restore "CommonDiscordMusicBot/CommonDiscordMusicBot.csproj"
COPY . .
WORKDIR "/src/CommonDiscordMusicBot"
RUN dotnet build "CommonDiscordMusicBot.csproj" -c Release -o /app/build
ENV TOKEN "OTA0MTMyMTY5OTQzODgzODI2.YX3Esw.F3q2B0t6FnGtfX61m8cTBV8O_5s"
ENV PORT "2333"
ENV AUTHENTICATION "youshallnotpass"
ENV HOSTNAME "172.17.0.2"

FROM build AS publish
RUN dotnet publish "CommonDiscordMusicBot.csproj" -c Release -o /app/publish
ENV TOKEN "OTA0MTMyMTY5OTQzODgzODI2.YX3Esw.F3q2B0t6FnGtfX61m8cTBV8O_5s"
ENV PORT "2333"
ENV AUTHENTICATION "youshallnotpass"
ENV HOSTNAME "172.17.0.2"

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CommonDiscordMusicBot.dll"]

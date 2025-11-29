# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY . .

RUN dotnet restore "WoWArmory/WoWArmory.csproj"

RUN dotnet publish WoWArmory -c Release -o WoWArmory/out

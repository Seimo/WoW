# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY . .

RUN dotnet restore "WoS.WebAPI/WoS.WebAPI.csproj"

RUN dotnet publish WoS.WebAPI -c Release -o WoS.WebAPI/out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
#EXPOSE 443
COPY --from=build /app/WoS.WebAPI/out .

ENTRYPOINT ["dotnet", "WoS.WebAPI.dll"]

# docker build --platform linux/amd64,linux/arm64 -t seimo01/wos_webapi -f Dockerfile .
# docker push seimo01/wos_webapi
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . ./Gatekeeper.Server/
WORKDIR /src/Gatekeeper.Server
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:5113
ENV Logging__LogLevel__Default=Error
EXPOSE 5113
ENTRYPOINT ["dotnet", "Gatekeeper.Server.dll"]

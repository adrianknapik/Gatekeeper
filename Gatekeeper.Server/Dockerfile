FROM bitnami/dotnet-sdk:9 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM bitnami/dotnet-sdk:9
# installing sqlite3
RUN apt-get update && apt-get install -y sqlite3 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:5113
ENV Logging__LogLevel__Default=Error

EXPOSE 5113

ENTRYPOINT ["dotnet", "Gatekeeper.dll"]

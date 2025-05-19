FROM bitnami/dotnet-sdk:9 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM bitnami/dotnet-sdk:9

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:5000
ENV Logging__LogLevel__Default=Error

EXPOSE 5000

ENTRYPOINT ["dotnet", "TodoDemo.dll"]

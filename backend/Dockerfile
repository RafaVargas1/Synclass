# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Synclass.sln ./
COPY src/Synclass.Api/Synclass.Api.csproj src/Synclass.Api/
COPY src/Synclass.Domain/Synclass.Domain.csproj src/Synclass.Domain/
COPY src/Synclass.Infrastructure/Synclass.Infrastructure.csproj src/Synclass.Infrastructure/
COPY tests/Synclass.Api.Tests/Synclass.Api.Tests.csproj tests/Synclass.Api.Tests/
RUN dotnet restore Synclass.sln

COPY . .
RUN dotnet publish src/Synclass.Api/Synclass.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Synclass.Api.dll"]

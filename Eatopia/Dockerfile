FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Eatopia.sln ./
COPY src/Eatopia.Api/Eatopia.Api.csproj src/Eatopia.Api/
COPY src/Eatopia.Application/Eatopia.Application.csproj src/Eatopia.Application/
COPY src/Eatopia.Domain/Eatopia.Domain.csproj src/Eatopia.Domain/
COPY src/Eatopia.Infrastructure/Eatopia.Infrastructure.csproj src/Eatopia.Infrastructure/

RUN dotnet restore Eatopia.sln

COPY . .

WORKDIR /src/src/Eatopia.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

RUN mkdir -p /app/uploads
EXPOSE 8080

ENTRYPOINT ["dotnet", "Eatopia.Api.dll"]
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Eatopia/Eatopia.sln Eatopia/
COPY Eatopia/src/Eatopia.Api/Eatopia.Api.csproj Eatopia/src/Eatopia.Api/
COPY Eatopia/src/Eatopia.Application/Eatopia.Application.csproj Eatopia/src/Eatopia.Application/
COPY Eatopia/src/Eatopia.Domain/Eatopia.Domain.csproj Eatopia/src/Eatopia.Domain/
COPY Eatopia/src/Eatopia.Infrastructure/Eatopia.Infrastructure.csproj Eatopia/src/Eatopia.Infrastructure/

RUN dotnet restore Eatopia/Eatopia.sln

COPY Eatopia/ Eatopia/

WORKDIR /src/Eatopia/src/Eatopia.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

RUN mkdir -p /app/uploads
EXPOSE 8080

ENTRYPOINT ["dotnet", "Eatopia.Api.dll"]
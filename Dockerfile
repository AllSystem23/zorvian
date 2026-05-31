FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/Nexora.Web/Nexora.Web.csproj src/Nexora.Web/
COPY src/Nexora.Application/Nexora.Application.csproj src/Nexora.Application/
COPY src/Nexora.Core/Nexora.Core.csproj src/Nexora.Core/
COPY src/Nexora.Infrastructure/Nexora.Infrastructure.csproj src/Nexora.Infrastructure/
RUN dotnet restore src/Nexora.Web/Nexora.Web.csproj
COPY . .
RUN dotnet publish src/Nexora.Web/Nexora.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Jwt__Secret=NexoraSuperSecretKey2026!AtLeast32CharactersLong!
ENV Jwt__ExpirationMinutes=60
ENV Jwt__RefreshExpirationDays=7
ENV ConnectionStrings__NexoraDb=Host=ep-solitary-cake-apmacxlx-pooler.c-7.us-east-1.aws.neon.tech;Database=nexora;Username=neondb_owner;Password=npg_N2lCkzum3shZ;SSL Mode=Require;Trust Server Certificate=true
ENV Firebase__ProjectId=nexora-hr
ENTRYPOINT ["dotnet", "Nexora.Web.dll"]
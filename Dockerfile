FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/Nexora.Web/Nexora.Web.csproj src/Nexora.Web/
COPY src/Nexora.Application/Nexora.Application.csproj src/Nexora.Application/
COPY src/Nexora.Core/Nexora.Core.csproj src/Nexora.Core/
COPY src/Nexora.Infrastructure/Nexora.Infrastructure.csproj src/Nexora.Infrastructure/
RUN dotnet restore src/Nexora.Web/Nexora.Web.csproj
COPY . .
RUN dotnet publish src/Nexora.Web/Nexora.Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Nexora.Web.dll"]

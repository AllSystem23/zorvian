FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/Zorvian.Web/Zorvian.Web.csproj src/Zorvian.Web/
COPY src/Zorvian.Application/Zorvian.Application.csproj src/Zorvian.Application/
COPY src/Zorvian.Core/Zorvian.Core.csproj src/Zorvian.Core/
COPY src/Zorvian.Infrastructure/Zorvian.Infrastructure.csproj src/Zorvian.Infrastructure/
RUN dotnet restore src/Zorvian.Web/Zorvian.Web.csproj
COPY . .
RUN dotnet publish src/Zorvian.Web/Zorvian.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Zorvian.Web.dll"]
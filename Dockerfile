FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AICentralWeb/AICentralWeb.csproj", "AICentralWeb/"]
RUN dotnet restore "AICentralWeb/AICentralWeb.csproj"
COPY . .
WORKDIR "/src/AICentralWeb"
RUN dotnet build "AICentralWeb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AICentralWeb.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AICentralWeb.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["./LoadTest.csproj", "LoadTest/"]
RUN dotnet restore "LoadTest/LoadTest.csproj"
COPY ./ ./LoadTest
WORKDIR "/src/LoadTest"
RUN dotnet build "LoadTest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LoadTest.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LoadTest.dll"]

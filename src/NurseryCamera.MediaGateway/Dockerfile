FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8088

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/NurseryCamera.MediaGateway/NurseryCamera.MediaGateway.csproj", "src/NurseryCamera.MediaGateway/"]
RUN dotnet restore "src/NurseryCamera.MediaGateway/NurseryCamera.MediaGateway.csproj"
COPY . .
WORKDIR "/src/src/NurseryCamera.MediaGateway"
RUN dotnet publish "NurseryCamera.MediaGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8088
ENTRYPOINT ["dotnet", "NurseryCamera.MediaGateway.dll"]

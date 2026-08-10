# syntax=docker/dockerfile:1
# Build context is expected to be the repository root, e.g.
#   docker build -f src/NurseryCamera.Api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/NurseryCamera.Api/NurseryCamera.Api.csproj", "src/NurseryCamera.Api/"]
COPY ["src/NurseryCamera.Application/NurseryCamera.Application.csproj", "src/NurseryCamera.Application/"]
COPY ["src/NurseryCamera.Infrastructure/NurseryCamera.Infrastructure.csproj", "src/NurseryCamera.Infrastructure/"]
COPY ["src/NurseryCamera.Domain/NurseryCamera.Domain.csproj", "src/NurseryCamera.Domain/"]
RUN dotnet restore "src/NurseryCamera.Api/NurseryCamera.Api.csproj"

COPY src/ src/
WORKDIR /src/src/NurseryCamera.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && addgroup --system --gid 1000 nursery \
    && adduser --system --uid 1000 --gid 1000 --no-create-home nursery

COPY --from=build /app/publish .
USER nursery

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "NurseryCamera.Api.dll"]

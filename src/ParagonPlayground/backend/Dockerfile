# === Development stage ===
# Hot reload via dotnet watch with source volume mount
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development
WORKDIR /src
COPY . .

RUN apt-get update && \
    apt-get install -y unzip curl && \
    curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /vsdbg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
CMD ["dotnet", "watch", "run", "--project", "src/ParagonPlayground.Api/ParagonPlayground.Api.csproj", "--no-launch-profile"]

# === Build stage ===
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/ParagonPlayground.Api/ParagonPlayground.Api.csproj -c Debug -o /app

# === Debug stage ===
# Compiled binary with vsdbg for debugger attachment
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS debug
WORKDIR /app
EXPOSE 8080

RUN apt-get update && \
    apt-get install -y unzip curl && \
    curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /vsdbg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "ParagonPlayground.Api.dll"]

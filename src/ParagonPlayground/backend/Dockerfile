FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ParagonPlayground.slnx ./
COPY Directory.Build.props ./
COPY src/Directory.Build.props src/
COPY src/Directory.Packages.props src/
COPY src/ParagonPlayground.Domain/ParagonPlayground.Domain.csproj src/ParagonPlayground.Domain/
COPY src/ParagonPlayground.Infrastructure/ParagonPlayground.Infrastructure.csproj src/ParagonPlayground.Infrastructure/
COPY src/ParagonPlayground.Api/ParagonPlayground.Api.csproj src/ParagonPlayground.Api/
RUN dotnet restore src/ParagonPlayground.Api/ParagonPlayground.Api.csproj

COPY . .
RUN dotnet publish src/ParagonPlayground.Api/ParagonPlayground.Api.csproj -c Debug -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app .

RUN apt-get update && \
    apt-get install -y unzip curl && \
    curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /vsdbg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["dotnet", "ParagonPlayground.Api.dll"]

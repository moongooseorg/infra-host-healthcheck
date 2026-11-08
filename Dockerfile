FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY nuget.config ./
COPY src/*.csproj src/
RUN --mount=type=secret,id=github_user,env=GITHUB_USER \
    --mount=type=secret,id=github_token,env=GITHUB_TOKEN \
    dotnet restore src/infra-host-healthcheck.csproj
COPY src/ src/
RUN dotnet publish src/infra-host-healthcheck.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "infra-host-healthcheck.dll"]

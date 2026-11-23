# 1) Build the React app
FROM node:24-alpine AS client
WORKDIR /src/client
COPY client/package*.json ./
RUN npm ci
COPY client/ ./
RUN npm run build   # outputs to ../server/AwwTogether.Api/wwwroot

# 2) Publish the ASP.NET API (with the built site in wwwroot)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS server
WORKDIR /src
COPY server/AwwTogether.Api/AwwTogether.Api.csproj server/AwwTogether.Api/
RUN dotnet restore server/AwwTogether.Api/AwwTogether.Api.csproj
COPY server/ server/
COPY --from=client /src/server/AwwTogether.Api/wwwroot server/AwwTogether.Api/wwwroot
RUN dotnet publish server/AwwTogether.Api/AwwTogether.Api.csproj -c Release -o /app --no-restore

# 3) Run (built for Google Cloud Run)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=server /app ./
# Cloud Run sets $PORT (default 8080); the app listens on it.
# Inquiries go to Firestore because Cloud Run's disk is wiped on restart.
# To run this image somewhere else with SQLite instead, override:
#   -e Storage__Provider=Sqlite -e ConnectionStrings__Default="Data Source=/data/awwtogether.db" -v aww-data:/data
ENV PORT=8080 \
    Storage__Provider=Firestore
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "AwwTogether.Api.dll"]

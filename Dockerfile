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

# 3) Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=server /app ./
ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__Default="Data Source=/data/awwtogether.db"
# Mount a volume here so inquiries survive redeploys
VOLUME /data
EXPOSE 8080
ENTRYPOINT ["dotnet", "AwwTogether.Api.dll"]

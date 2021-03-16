FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY simple-http/*.csproj .
RUN dotnet restore -r linux-musl-x64

# copy and publish app and libraries
COPY simple-http/ .
RUN dotnet publish -c release -o /app -r linux-musl-x64 --no-restore /p:PublishTrimmed=true /p:TrimMode=Link

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime-deps:5.0-alpine-amd64
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["./simple-http"]

EXPOSE 8080
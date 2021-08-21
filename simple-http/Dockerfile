FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

WORKDIR /source

# copy csproj and restore as distinct layers
COPY simple-http/*.csproj .
RUN dotnet restore 

# copy and publish app and libraries
COPY simple-http/ .
RUN dotnet publish -c release -o /app --no-restore 

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:5.0

RUN mkdir -p /data && chmod 777 /data

RUN ls -alrt
RUN apt-get -y update
RUN apt-get -y install curl
RUN apt-get -y install gnupg2
RUN apt-get -y install redis-tools

RUN echo "deb [signed-by=/usr/share/keyrings/cloud.google.gpg] http://packages.cloud.google.com/apt cloud-sdk main" | tee -a /etc/apt/sources.list.d/google-cloud-sdk.list && \
 curl https://packages.cloud.google.com/apt/doc/apt-key.gpg | apt-key --keyring /usr/share/keyrings/cloud.google.gpg  add - && \
 apt-get update -y && \
 apt-get install google-cloud-sdk -y

RUN gcloud version
RUN gsutil version

WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["./simple-http"]

EXPOSE 8080

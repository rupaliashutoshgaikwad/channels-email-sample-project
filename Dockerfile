FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG GITHUB_USERNAME
ARG GITHUB_TOKEN
ARG SRC_PATH=src

WORKDIR /src

# copy only files needed for restore
ARG PROJ=CloudEmail.SampleProject.API.Client
COPY $SRC_PATH/$PROJ/$PROJ.csproj $PROJ/

# copy main project and restore
ARG PROJ=CloudEmail.SampleProject.API
COPY $SRC_PATH/$PROJ/$PROJ.csproj $PROJ/
COPY $SRC_PATH/.nuget .nuget
RUN dotnet restore $PROJ/$PROJ.csproj --configfile .nuget/NuGet.Config

# copy the rest
COPY $SRC_PATH ./

FROM build AS publish
WORKDIR /src/CloudEmail.SampleProject.API
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0
ARG SRC_PATH=src

RUN apt-get -y update; apt-get -y install curl ca-certificates
# Add RDS CA Certs to image
RUN curl -o /usr/local/share/ca-certificates/global-bundle.crt https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem
# Add CA Certs for smtp relay
COPY $SRC_PATH/certs/testserverca.pem /usr/local/share/ca-certificates/testserverca.crt
RUN ls -al /usr/local/share/ca-certificates
# Gov Cloud
RUN curl -o /usr/local/share/ca-certificates/gov-global-bundle.crt https://truststore.pki.us-gov-west-1.rds.amazonaws.com/global/global-bundle.pem
RUN update-ca-certificates
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CloudEmail.SampleProject.API.dll"]
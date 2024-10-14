FROM mcr.microsoft.com/dotnet/sdk:6.0
# Add RDS CA Certs to image
RUN curl -o /usr/local/share/ca-certificates/global-bundle.crt https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem
# Gov Cloud
RUN curl -o /usr/local/share/ca-certificates/gov-global-bundle.crt https://truststore.pki.us-gov-west-1.rds.amazonaws.com/global/global-bundle.pem
RUN update-ca-certificates
ARG ASPNETCORE_ENVIRONMENT
WORKDIR /app

# Copy project from build context into workdir
COPY src/CloudEmail.SampleProject.API CloudEmail.SampleProject.API
COPY src/CloudEmail.SampleProject.API.Automation CloudEmail.SampleProject.API.Automation
COPY src/.nuget .nuget
COPY src/CloudEmail.SampleProject.API.Client CloudEmail.SampleProject.API.Client
COPY src/CloudEmail.SampleProject.API.Models CloudEmail.SampleProject.API.Models

# Nuget Restore & msbuild
RUN dotnet build CloudEmail.SampleProject.API.Automation/CloudEmail.SampleProject.API.Automation.csproj -c Debug --configfile .nuget/NuGet.Config
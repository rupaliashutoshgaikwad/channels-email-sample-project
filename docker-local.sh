#!/bin/bash

docker build\
  -t send-email-api\
  --build-arg GITHUB_USERNAME="cxossp"\
  --build-arg GITHUB_TOKEN=${GITHUB_TOKEN}\
  .\
  &&\
docker run\
  --env ASPNETCORE_ENVIRONMENT="Local"\
  --env ASPNETCORE_URLS="http://*:80"\
  -p80:80\
  send-email-api

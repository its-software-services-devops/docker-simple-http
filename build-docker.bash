#!/bin/bash

#for docker local build
rm -rf ./simple-http/obj

sudo docker build -t project:local -f ./simple-http/Dockerfile .

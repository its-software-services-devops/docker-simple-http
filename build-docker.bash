#!/bin/bash

#for docker local build
rm -rf ./simple-http/obj

sudo docker build -t project -f ./simple-http/Dockerfile .

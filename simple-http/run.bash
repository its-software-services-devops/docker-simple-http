#!/bin/bash

export DELAY_TO_START_SEC=0
export DELAY_MSEC=0
export GCP_KEY_FILE_PATH=/c/Users/Seubpong.mon/Downloads/gcp-dmp-devops-aef03efe8336.json
export CONFIG_FILE=config-local.cfg
export TCP_CHECK_BQ_SIZE=10
export HTTP_CHECK_BQ_SIZE=10

export REDIS_CHECK_BQ_SIZE=10

dotnet run

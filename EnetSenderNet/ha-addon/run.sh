#!/bin/sh

export ENET_HOST=$(jq -r '.enet_host' /data/options.json)
export ENET_PORT=$(jq -r '.enet_port' /data/options.json)

exec dotnet EnetSenderNet.dll

#!/bin/sh

export ENET_HOST=$(sed -n 's/.*"enet_host"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' /data/options.json)
export ENET_PORT=$(sed -n 's/.*"enet_port"[[:space:]]*:[[:space:]]*\([0-9]*\).*/\1/p' /data/options.json)

exec /app/EnetSenderNet

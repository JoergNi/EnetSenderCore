#!/usr/bin/with-contenv bashio

export ENET_HOST=$(bashio::config 'enet_host')
export ENET_PORT=$(bashio::config 'enet_port')

exec ./EnetSenderNet

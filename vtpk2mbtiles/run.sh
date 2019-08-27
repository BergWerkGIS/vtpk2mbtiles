#!/usr/bin/env bash
set -eu

docker run -it --rm --name vtpk2mbtiles \
 --mount src="${HOME}/basemap",dst=/data,type=bind bergwerkgis/vtpk2mbtiles \
 /data/bmapv_vtpk_3857 \
 /data/bmapv.mbtiles \
 false


docker run -it --rm --name vtpk2mbtiles \
 --mount src="${HOME}/basemap",dst=/data,type=bind bergwerkgis/vtpk2mbtiles \
 /data/bmapv_vtpk_3857 \
 /data/bmapv-tiles \
 false

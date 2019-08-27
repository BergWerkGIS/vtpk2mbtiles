@echo off
::  docker build -t vtpk2mbtiles .
docker run -it --rm ^
 --name vtpk2mbtiles ^
 -m 4g ^
 --cpus=%NUMBER_OF_PROCESSORS% ^
 --mount src="C:\basemap",dst=/data,type=bind bergwerkgis/vtpk2mbtiles ^
 /data/bmapv_vtpk_3857 ^
 /data/bmapv.mbtiles ^
 false

FROM mcr.microsoft.com/dotnet/runtime-deps@sha256:c3f438e63517eaefe1cf80e4c7fa2d6c602f9d9295a44105465268eff68c8ad2
ENV TZ=Europe/Berlin
WORKDIR /app
COPY publish_single/ .
EXPOSE 8080
CMD ["/app/EnetSenderNet"]

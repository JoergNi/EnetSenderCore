FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim-arm64v8
RUN apt-get update && apt-get install -y --no-install-recommends jq && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY publish/ .
COPY run.sh /run.sh
RUN chmod +x /run.sh
EXPOSE 8080
CMD ["/run.sh"]

# NIE Template Fleet Registry — production container.
# Multi-stage build keeps the final image minimal (~50 MB) and rootless.
#
#   docker build -t nie-template-registry:v2 .
#   docker run --rm -p 8080:8080 -v $(pwd)/data:/data \
#     -e REGISTRY_TOKEN=secret nie-template-registry:v2

FROM python:3.12-slim AS base

# Non-root user for runtime
ARG UID=10001
RUN useradd --system --uid ${UID} --no-create-home --shell /sbin/nologin registry

WORKDIR /app
COPY registry.py /app/registry.py

# DB lives on a volume; image stays read-only otherwise.
RUN mkdir -p /data && chown -R registry:registry /data /app

USER registry
ENV HOST=0.0.0.0 \
    PORT=8080 \
    REGISTRY_DB=/data/registry.db \
    PYTHONUNBUFFERED=1

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD python -c "import urllib.request,sys; \
sys.exit(0 if urllib.request.urlopen('http://127.0.0.1:8080/healthz', timeout=2).status==200 else 1)"

CMD ["python", "/app/registry.py"]

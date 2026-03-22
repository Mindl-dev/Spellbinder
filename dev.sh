#!/bin/bash
# Dev build — parallel container on alternate ports for testing
# UDP 10611, TCP 10612, API 10613
# Uses the same image but a separate container + DB volume
set -e

cd "$(dirname "$0")"

CONTAINER="spellbinder-dev"

echo "Stopping $CONTAINER..."
podman stop "$CONTAINER" 2>/dev/null || true
podman rm "$CONTAINER" 2>/dev/null || true

mkdir -p Logs-dev

echo "Pruning old images..."
podman image prune -f >/dev/null 2>&1 || true

echo "Building..."
podman build -t spellbinder .

echo "Starting $CONTAINER (UDP 10611, TCP 10612, API 10613)..."
podman run -d --name "$CONTAINER" \
  -p 10611:10601/udp \
  -p 10612:10602/tcp \
  -p 10613:10603/tcp \
  -v ./Content:/app/Content \
  -v spellbinder-dev-data:/var/lib/mysql \
  -v ./Logs-dev:/app/Logs \
  spellbinder --dev "$@"

echo "Up. Waiting for server..."
sleep 3
podman logs --tail 10 "$CONTAINER"
echo ""
echo "Dev server running:"
echo "  TCP: 10612  UDP: 10611  API: 10613"
echo "  Logs: ./Logs-dev/"
echo "  Stop: podman stop $CONTAINER"

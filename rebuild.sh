#!/bin/bash
set -e

cd "$(dirname "$0")"

echo "Stopping spellbinder..."
podman stop spellbinder 2>/dev/null || true
podman rm spellbinder 2>/dev/null || true

echo "Building..."
podman build -t spellbinder .

echo "Starting..."
podman run -d --name spellbinder --network host \
  -v ./Content:/app/Content -v spellbinder-data:/var/lib/mysql spellbinder "$@"

echo "Up. Waiting for server..."
sleep 3
podman logs --tail 5 spellbinder

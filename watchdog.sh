#!/bin/bash
# Watchdog — polls /api/health every 10 seconds.
# Health endpoint checks arena tick recency, not just API liveness.
# If 3 consecutive failures (tick stale >5s or API down), dumps thread stacks and restarts.
#
# Usage: ./watchdog.sh &
# Safe to call from cron — exits if already running.

PIDFILE="/tmp/spellbinder-watchdog.pid"
if [ -f "$PIDFILE" ] && kill -0 "$(cat "$PIDFILE")" 2>/dev/null; then
    exit 0
fi
echo $$ > "$PIDFILE"
trap "rm -f $PIDFILE" EXIT

CONTAINER="spellbinder"
HEALTH_URL="http://localhost:10603/api/health"
FAIL_THRESHOLD=3
POLL_INTERVAL=10

fails=0

echo "[watchdog] Started. Polling $HEALTH_URL every ${POLL_INTERVAL}s (threshold: ${FAIL_THRESHOLD} failures)"

while true; do
    response=$(curl -s --max-time 5 "$HEALTH_URL" 2>/dev/null)
    http_ok=$?

    if [ $http_ok -eq 0 ] && echo "$response" | grep -q '"healthy":true'; then
        if [ $fails -gt 0 ]; then
            echo "[watchdog] $(date '+%H:%M:%S') Recovered after $fails failures"
        fi
        fails=0
    else
        fails=$((fails + 1))
        if [ $http_ok -ne 0 ]; then
            echo "[watchdog] $(date '+%H:%M:%S') API unreachable ($fails/$FAIL_THRESHOLD)"
        else
            echo "[watchdog] $(date '+%H:%M:%S') Tick stale: $response ($fails/$FAIL_THRESHOLD)"
        fi

        if [ $fails -ge $FAIL_THRESHOLD ]; then
            echo "[watchdog] $(date '+%H:%M:%S') Server hung — dumping thread stacks"
            podman exec "$CONTAINER" kill -QUIT 1 2>/dev/null
            sleep 2
            echo "=== THREAD DUMP ===" >> watchdog_dumps.log
            echo "$(date)" >> watchdog_dumps.log
            podman logs --tail 200 "$CONTAINER" >> watchdog_dumps.log 2>&1
            echo "===================" >> watchdog_dumps.log

            echo "[watchdog] $(date '+%H:%M:%S') Restarting container"
            podman restart "$CONTAINER"
            sleep 10
            fails=0
            echo "[watchdog] $(date '+%H:%M:%S') Restarted"
        fi
    fi

    sleep $POLL_INTERVAL
done

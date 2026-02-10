#!/bin/bash
PORT=5050
LIMIT=100

echo "=== Testing Rate Limiting (Limit: $LIMIT/min) ==="
echo "Sending $((LIMIT + 5)) requests..."

for i in $(seq 1 $((LIMIT + 5))); do
   # Send request in background to simulate concurrency, or sequential is fine if fast.
   # Using sequential to see output clearly.
   curl -s -o /dev/null -w "%{http_code}\n" http://localhost:$PORT/api/identity/health &
done

wait
echo -e "\nRequests sent."

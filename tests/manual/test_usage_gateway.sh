#!/bin/bash
PORT=5050 # Gateway Port
TENANT_ID="55555555-5555-5555-5555-555555555555"
METRIC="gateway_metric"

echo "=== Testing Usage via Gateway (Port $PORT) ==="

# 1. Report Usage
curl -X POST http://localhost:$PORT/api/billing/usage/events \
  -H "Content-Type: application/json" \
  -d "{\"tenantId\": \"$TENANT_ID\", \"metricKey\": \"$METRIC\", \"quantity\": 1}"

echo -e "\n"

# 2. Get Usage
curl -v "http://localhost:$PORT/api/billing/usage?tenantId=$TENANT_ID&metricKey=$METRIC"
echo -e "\n"

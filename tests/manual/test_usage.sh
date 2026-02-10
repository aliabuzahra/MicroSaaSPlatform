#!/bin/bash

TENANT_ID="55555555-5555-5555-5555-555555555555"
PLAN_ID="pri_12345"
METRIC="test_metric"
PORT=5002

echo "=== 1. Set Limit for Plan '$PLAN_ID' (Metric: $METRIC, Max: 10) ==="
curl -X POST http://localhost:$PORT/api/billing/usage/limits \
  -H "Content-Type: application/json" \
  -d "{\"planId\": \"$PLAN_ID\", \"metricKey\": \"$METRIC\", \"maxQuantity\": 10}"
echo -e "\n"

echo "=== 2. Report Usage (Quantity: 5) ==="
curl -X POST http://localhost:$PORT/api/billing/usage/events \
  -H "Content-Type: application/json" \
  -d "{\"tenantId\": \"$TENANT_ID\", \"metricKey\": \"$METRIC\", \"quantity\": 5}"
echo -e "\n"

echo "=== 3. Get Usage (Check Allocation) ==="
curl -v "http://localhost:$PORT/api/billing/usage?tenantId=$TENANT_ID&metricKey=$METRIC"
echo -e "\n"

echo "=== 4. Report More Usage (Quantity: 6) -> Total 11 (Exceeds 10) ==="
curl -X POST http://localhost:$PORT/api/billing/usage/events \
  -H "Content-Type: application/json" \
  -d "{\"tenantId\": \"$TENANT_ID\", \"metricKey\": \"$METRIC\", \"quantity\": 6}"
echo -e "\n"

echo "=== 5. Get Usage Again (Should be IsAllocated: false) ==="
curl -v "http://localhost:$PORT/api/billing/usage?tenantId=$TENANT_ID&metricKey=$METRIC"
echo -e "\n"

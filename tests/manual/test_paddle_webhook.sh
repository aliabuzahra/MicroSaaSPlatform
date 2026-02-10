#!/bin/bash

# Configuration
SECRET="paddle_secret_test_123"
TIMESTAMP=$(date +%s)
# Raw JSON Body (Minified to match what code sees)
BODY='{"event_type":"subscription.updated","data":{"status":"active","custom_data":{"tenantId":"55555555-5555-5555-5555-555555555555"},"items":[{"price":{"id":"pri_12345"}}]}}'

# Calculate HMAC SHA256
PAYLOAD="$TIMESTAMP:$BODY"
HASH=$(echo -n "$PAYLOAD" | openssl dgst -sha256 -hmac "$SECRET" | sed 's/^.* //')

# Signature Header
SIGNATURE="ts=$TIMESTAMP;h1=$HASH"

echo "Sending Webhook..."
echo "Signature: $SIGNATURE"
echo "Body: $BODY"

curl -v -X POST "http://localhost:5002/api/billing/webhook" \
-H "Content-Type: application/json" \
-H "Paddle-Signature: $SIGNATURE" \
-d "$BODY"

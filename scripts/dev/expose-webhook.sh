#!/bin/bash

# Port where Gateway is running
PORT=5050
WEBHOOK_path="/api/billing/webhook"

echo "=== Exposing Paddle Webhook ==="
echo "Target: http://localhost:$PORT$WEBHOOK_path"
echo ""

if command -v ngrok &> /dev/null; then
    echo "Starting ngrok on port $PORT..."
    echo "Copy the HTTPS URL and add $WEBHOOK_path to it."
    ngrok http $PORT
elif command -v npx &> /dev/null; then
    echo "ngrok not found. Using localtunnel via npx..."
    echo "Starting localtunnel on port $PORT..."
    npx localtunnel --port $PORT
else
    echo "Error: Neither 'ngrok' nor 'npx' found."
    echo "Please install ngrok: https://ngrok.com/download"
fi

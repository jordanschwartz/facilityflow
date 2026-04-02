#!/bin/bash

ROOT="$(cd "$(dirname "$0")" && pwd)"

# Kill any existing processes on our ports
echo "Stopping existing processes..."
lsof -ti:5000 | xargs kill -9 2>/dev/null
lsof -ti:5173 | xargs kill -9 2>/dev/null

# Ensure Docker / PostgreSQL is running
echo "Starting PostgreSQL..."
docker compose -f "$ROOT/docker-compose.yml" up -d
echo "Waiting for PostgreSQL to be ready..."
until docker exec facilityflow-db pg_isready -U postgres -q; do
  sleep 1
done
echo "PostgreSQL ready."

# Start backend in background, log to /tmp/facilityflow-backend.log
echo "Starting backend..."
cd "$ROOT/backend/FacilityFlow.Api"
dotnet run > /tmp/facilityflow-backend.log 2>&1 &
BACKEND_PID=$!

# Wait for backend to be listening
echo "Waiting for backend on :5000..."
until curl -s http://localhost:5000/swagger/index.html > /dev/null 2>&1; do
  sleep 1
done
echo "Backend ready (pid $BACKEND_PID)."

# Start frontend in background, log to /tmp/facilityflow-frontend.log
echo "Starting frontend..."
cd "$ROOT/frontend"
npm run dev > /tmp/facilityflow-frontend.log 2>&1 &
FRONTEND_PID=$!

echo ""
echo "✓ Backend:  http://localhost:5000  (logs: /tmp/facilityflow-backend.log)"
echo "✓ Swagger:  http://localhost:5000/swagger"
echo "✓ Frontend: http://localhost:5173  (logs: /tmp/facilityflow-frontend.log)"
echo "✓ pgAdmin:  http://localhost:5050"
echo ""
echo "Press Ctrl+C to stop everything."

# On Ctrl+C, kill both
trap "echo ''; echo 'Stopping...'; kill $BACKEND_PID $FRONTEND_PID 2>/dev/null; exit 0" SIGINT SIGTERM

wait

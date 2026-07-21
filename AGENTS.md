# Multi Micro-SaaS Platform

Monorepo with two parts:

- Frontend: React + Vite apps `apps/web` and `apps/admin`, plus shared `packages/ui` and `packages/features/*`. Managed with `pnpm` workspaces + Turborepo (`turbo`).
- Backend: .NET 9 microservices under `apps/services/*` (identity, tenant, billing, notifications, audit), a YARP reverse-proxy `apps/gateway`, and shared library `libs/SaaS.Shared.Kernel`. All in `SaaS.Platform.sln`.
- Infra: Postgres, RabbitMQ, Redis (see `docker-compose.yml`).

## Cursor Cloud specific instructions

Dependencies (Node/pnpm, .NET 9 SDK at `~/.dotnet`, Docker) are already installed in the VM image, and the update script runs `pnpm install` + `dotnet restore`. The notes below are startup/run caveats that are NOT handled automatically.

### .NET on PATH
`dotnet` lives at `~/.dotnet` and is added to PATH via `~/.bashrc`. Non-interactive shells may not pick it up; prefer `$HOME/.dotnet/dotnet ...` or `export PATH="$HOME/.dotnet:$PATH"` at the top of a script.

### Docker daemon (needed for infra)
The Docker daemon is NOT running by default. Start it once per session, then bring up ONLY the infra services (do not build the .NET service images for dev):

```
sudo dockerd &          # or: tmux session running `sudo dockerd`
sudo docker compose up -d db redis rabbitmq
```

Postgres → `localhost:5432`, RabbitMQ → `localhost:5672` (mgmt `15672`), Redis → `localhost:6379`.

### Running backend services in dev mode
Run each service with `dotnet run` (not the Docker images). Startup dependencies:
- `identity` needs Postgres AND RabbitMQ (the `/auth/register` endpoint publishes `UserRegisteredEvent` via MassTransit/RabbitMQ). It auto-creates its DB via `EnsureCreated()` in Development.
- `gateway` connects to Redis at startup (rate-limiting middleware) — Redis must be up first.

The gateway's `appsettings.json` routes to services on FIXED ports: identity → `localhost:5001`, tenant → `localhost:5002`, billing → `localhost:5003`. `launchSettings.json` uses different random ports, so override with `--urls` to match. Example minimal stack for the identity flow:

```
# identity on the port the gateway expects
(cd apps/services/identity && $HOME/.dotnet/dotnet run --no-launch-profile --urls http://localhost:5001)
# gateway on 5050 (the port the e2e tests expect)
(cd apps/gateway && $HOME/.dotnet/dotnet run --no-launch-profile --urls http://localhost:5050)
```

Smoke test (register + login through the gateway; requires `X-Tenant-Id`):

```
curl -X POST http://localhost:5050/api/identity/auth/register \
  -H 'Content-Type: application/json' -H "X-Tenant-Id: $(uuidgen)" \
  -d '{"email":"a@b.com","password":"Password123!","fullName":"A","tenantId":"<same-uuid>"}'
```

### Frontend dev servers
`pnpm --filter web dev` and `pnpm --filter admin dev`. Both default to Vite port 5173, so run one on an alternate port (e.g. `--port 5174`) if running simultaneously. Note `apps/admin/src/api/client.ts` hardcodes the gateway at `http://localhost:5000/api`; to exercise the admin tenant/billing screens against a live backend, run the gateway on port `5000` and also start the `tenant` and `billing` services.

### Lint / test / build (see `.github/workflows/ci.yml`)
- Frontend: `pnpm turbo lint`, `pnpm turbo build`.
- Backend: `dotnet build SaaS.Platform.sln -c Release`, `dotnet test SaaS.Platform.sln`.
- E2E (`tests/e2e`, vitest against a running gateway on `:5050`): `npm install && npm test`. The e2e suite includes a tenant-isolation login assertion that depends on the full stack.

Note: the root `package.json` devDependencies pin `eslint@^10`, which is newer than the per-app configs expect (peer warnings on install). Lint still passes today.

#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
api_project="${project_root}/src/TransactFlow.Api/TransactFlow.Api.csproj"
web_project="${project_root}/src/TransactFlow.Web"

cleanup() {
  jobs -p | xargs -r kill 2>/dev/null || true
}
trap cleanup EXIT INT TERM

DOTNET_CLI_HOME="${project_root}/.dotnet" dotnet run --project "${api_project}" &
api_pid=$!
echo "API démarrée (PID ${api_pid}) — http://localhost:5090"

(cd "${web_project}" && npm start) &
web_pid=$!
echo "Angular démarré (PID ${web_pid}) — http://localhost:4200"

wait -n "${api_pid}" "${web_pid}"


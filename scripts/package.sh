#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
web_project="${project_root}/src/TransactFlow.Web"
api_project="${project_root}/src/TransactFlow.Api/TransactFlow.Api.csproj"
package_dir="${project_root}/artifacts/transactflow"
dotnet_bin="${DOTNET_BIN:-}"

if [[ -z "${dotnet_bin}" ]]; then
  if command -v dotnet >/dev/null 2>&1; then
    dotnet_bin="$(command -v dotnet)"
  elif [[ -x /usr/local/share/dotnet/dotnet ]]; then
    dotnet_bin="/usr/local/share/dotnet/dotnet"
  else
    echo "dotnet est requis" >&2
    exit 1
  fi
fi

if [[ ! -d "${web_project}/node_modules" ]]; then
  (cd "${web_project}" && npm ci)
fi

(cd "${web_project}" && npm run build)

rm -rf "${package_dir}"
DOTNET_CLI_HOME="${project_root}/.dotnet" "${dotnet_bin}" publish "${api_project}" \
  --configuration Release \
  --output "${package_dir}"

mkdir -p "${package_dir}/wwwroot"
cp -R "${web_project}/dist/transactflow-web/browser/." "${package_dir}/wwwroot/"

echo "Package prêt dans ${package_dir}"
echo "Démarrage : dotnet ${package_dir}/TransactFlow.Api.dll"


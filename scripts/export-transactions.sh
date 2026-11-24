#!/usr/bin/env bash
set -euo pipefail

api_url="${TRANSACTFLOW_API_URL:-http://localhost:5090}"
output_dir="${TRANSACTFLOW_OUTPUT_DIR:-artifacts}"
json_file="${output_dir}/transactions.json"
csv_file="${output_dir}/transactions.csv"

command -v curl >/dev/null || { echo "curl est requis" >&2; exit 1; }
command -v jq >/dev/null || { echo "jq est requis" >&2; exit 1; }
mkdir -p "${output_dir}"

curl --fail --silent --show-error "${api_url}/api/transactions" -o "${json_file}"
jq -r '
  ["TransactionId","Description","Amount","TransactionType","Category","TransactionDate","UpdatedAt"],
  (.[] | [.id,.description,.amount,.type,.category,.date,.updatedAt]) | @csv
' "${json_file}" > "${csv_file}"

row_count="$(jq 'length' "${json_file}")"
echo "Export terminé : ${row_count} transactions dans ${csv_file}"


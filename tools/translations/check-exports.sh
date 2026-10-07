#!/usr/bin/env bash
# Regenerates every translation profile's export and fails if that changes, or
# creates, anything under translations/welsh-translations. Used by the
# pre-commit hook (.githooks/pre-commit) and the check-translations workflow.
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

output_dir=translations/welsh-translations

for profile in tools/translations/profiles/*.json; do
  dotnet run --project tools/translations/cli/cli.csproj -- export --profile "$(basename "$profile" .json)"
done

if ! git diff --quiet -- "$output_dir" || [ -n "$(git ls-files --others --exclude-standard -- "$output_dir")" ]; then
  echo "Translation exports are out of date. Run tools/translations/check-exports.sh and commit $output_dir." >&2
  exit 1
fi

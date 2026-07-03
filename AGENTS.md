# Repository Guidance

## Translations

The translation export/import workflow is documented in
[`tools/translations/README.md`](tools/translations/README.md).

Use `tools/translations/profiles/*.json` as the page matrix for translation
exports. When a profiled journey changes, re-audit its controller endpoints,
views, partials and RESX files before regenerating workbooks.

For CSoC, follow the refresh process in the translation tool README to find new
resources and keys. Start with a broad search across UI and application code,
then trace controller routes, feature gates, Razor views, partials and matching
RESX resources before updating `tools/translations/profiles/csoc.json`.
Profile pages that only reuse shared rows should still be recorded; export logs
that there is nothing to include for those pages and skips the empty workbook.

Verify profile changes with:

```bash
dotnet run --project tools/translations/cli/cli.csproj -- export --profile csoc --output /tmp/epr-packaging-csoc-translations
dotnet run --project tools/translations/cli/cli.csproj -- import --profile csoc --input /tmp/epr-packaging-csoc-translations
```

Do not create Welsh translations manually. Only import or copy Welsh text from
an approved source when the English string and UI placement match.

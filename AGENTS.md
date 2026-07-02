# Repository Guidance

## Translations

The translation export/import workflow is documented in
[`tools/translations/README.md`](tools/translations/README.md).

Use `tools/translations/profiles/*.json` as the page matrix for translation
exports. When a profiled journey changes, re-audit its controller endpoints,
views, partials and RESX files before regenerating workbooks.

For CSoC, follow the refresh process in the translation tool README to find new
resources and keys, then verify with:

```bash
dotnet run --project tools/translations -- export --profile csoc --output /tmp/epr-packaging-csoc-translations
dotnet run --project tools/translations -- import --profile csoc --input /tmp/epr-packaging-csoc-translations
```

Do not create Welsh translations manually. Only import or copy Welsh text from
an approved source when the English string and UI placement match.

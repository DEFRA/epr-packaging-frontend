# Repository Guidance

## Translations

The translation export/import workflow is documented in
[`tools/translations/README.md`](tools/translations/README.md).

Use `tools/translations/profiles/*.json` as the page matrix for translation
exports. When a profiled journey changes, re-audit its controller endpoints,
views, partials and RESX files before regenerating workbooks.
Set each profile page's `figmaUrl` to the exact Figma frame, prototype or design
link when one is known. Leave it as `null` only when the design URL is not yet
available, because the exporter includes this link in the translator workbook.

For CSoC, follow the refresh process in the translation tool README to find new
resources and keys. Start with a broad search across UI and application code,
then trace controller routes, feature gates, Razor views, partials and matching
RESX resources before updating `tools/translations/profiles/csoc.json`.
Profile pages that only reuse shared rows should still be recorded; export logs
that there is nothing to include for those pages and skips the empty workbook.
Exports write workbooks to an `xlsx` subdirectory and matching deterministic
review JSON sidecars to a `json` subdirectory.

Verify profile changes with:

```bash
dotnet run --project tools/translations/cli/cli.csproj -- export --profile csoc --output /tmp/epr-packaging-csoc-translations
dotnet run --project tools/translations/cli/cli.csproj -- import --profile csoc --input /tmp/epr-packaging-csoc-translations
```

For December waste, follow the refresh process in the translation tool README to find new
resources and keys. Start with a broad search across UI and application code,
then trace controller routes, feature gates, Razor views, partials and matching
RESX resources before updating `tools/translations/profiles/december-waste.json`.
Exports write workbooks to an `xlsx` subdirectory and matching deterministic
review JSON sidecars to a `json` subdirectory.

```bash
dotnet run --project tools/translations/cli/cli.csproj -- export --profile december-waste --output /tmp/epr-packaging-december-waste-translations
dotnet run --project tools/translations/cli/cli.csproj -- import --profile december-waste --input /tmp/epr-packaging-december-waste-translations
```

Do not create Welsh translations manually. Only import or copy Welsh text from
an approved source when the English string and UI placement match.

## Aggregate health checks

When adding a downstream health check, inspect the existing client’s
authentication flow. Do not use delegated-user clients from the header-protected
anonymous endpoint. Add service-token support only when an existing app-only
client configuration already exists; otherwise use a dedicated unauthenticated
named health client and retain the current downstream convention.

## Waste Obligations journey tests

The shared suite lives in
[DEFRA/waste-obligations-journey-tests](https://github.com/DEFRA/waste-obligations-journey-tests).
Read its [run instructions](https://github.com/DEFRA/waste-obligations-journey-tests/blob/main/README.md)
and [agent guidance](https://github.com/DEFRA/waste-obligations-journey-tests/blob/main/AGENTS.md)
when changing behavior used by the journey.

The backend, Waste Obligations frontend and packaging proxy PR workflows use
the [shared action](https://github.com/DEFRA/waste-obligations-journey-tests/blob/main/run-journey-tests/action.yml)
to run E2E, accessibility and passive security profiles against a CDP-only
Docker stack. Browser traffic enters through the packaging proxy; Azure
application navigation is omitted, but the remaining scenario must run.
Azure AD B2C login is still required. After CDP service deployment to dev,
the deployed suite exercises the full Azure-to-CDP journey. Passing the Docker
checks does not prove that deployed Azure navigation or configuration works.

### Environment and contract changes

For every added, renamed, removed or changed environment variable, feature
flag, default, credential, endpoint or dependency used by this journey:

1. Trace where the service reads the setting and which journey behavior it
   controls. Check the service examples/defaults and deployment configuration.
2. Check the journey repository's
   [CI Compose stack](https://github.com/DEFRA/waste-obligations-journey-tests/blob/main/ci/compose.yml),
   action inputs/environment and caller workflow. Add or amend the value where
   the target service actually receives it; a variable set only on the test
   runner does not configure another container. Update the journey `.env.example`
   only for settings consumed by the runner or required local setup.
3. Review service-owned Compose fragments, WireMock contracts, infrastructure
   initialisers and scenario seed data. Keep service dependency setup with its
   owning service, and shared orchestration/scenario data in the journey repo.
4. Check the deployed CDP and Azure configuration separately. Document required
   flag/secret changes and their owner; Docker values do not propagate there.
   Keep real credentials out of source control and logs.
5. Coordinate repository revisions when contracts change. The three CI callers
   select a matching journey branch or fall back to main; the action resolves
   explicit backend/frontend/proxy revisions, then matching branches, then
   published images with main setup assets. Verify the selected revisions
   contain all required changes before relying on a run.
6. Run the affected Docker journey profiles and the deployed journey where its
   behavior changes and the environment is available. Record mode, revision,
   pass/fail/skip counts and blockers. Explain in the change description which
   journey setup was updated, or why no journey configuration change is needed.
   Do not hide a configuration mismatch by skipping a whole scenario.

This Azure application owns the full journey's account home, year selection
and transition into Waste Obligations. It is not started in the CDP-only Docker
PR stack. Changes to `FeatureManagement__ShowMultiYearObligations`, navigation,
authentication or handoff URLs therefore need validation against the deployed
Azure application as well as the shared CDP assertions. A runner or Docker
feature flag cannot enable a flag in this application: record the required Azure
configuration change and owner. Do not assume this repository invokes the shared
CDP-only PR action; inspect its own workflows before choosing validation.

# Translation export and import tool

This tool mirrors the page-matrix workflow used by `waste-obligations-frontend`, adapted for this .NET MVC app and RESX resources.

It targets .NET 10 and uses the `.slnx` solution format:

```bash
dotnet build tools/translations/translations.slnx
dotnet test tools/translations/translations.slnx
```

The solution lives in `tools/translations`. The CLI project is in
`tools/translations/cli/cli.csproj`, and the test project is in
`tools/translations/tests/translations.Tests.csproj`.

The profile is the page matrix. It maps each workbook to a route, Razor view, feature flags, app settings, and the RESX files that render the content. Each workbook row has a hidden translation key in the format:

```text
src/FrontendSchemeRegistration.UI/Resources/.../SomeResource.en.resx::resource_key
```

That lets import update the matching Welsh file and key, for example `SomeResource.cy.resx`.

## CSoC profile

The first profile is `csoc`:

```bash
dotnet run --project tools/translations/cli/cli.csproj -- export --profile csoc
```

By default this writes one workbook per CSoC page with translation entries to:

```text
translations/welsh-translations/csoc
```

Export fails if a selected English RESX value starts or ends with whitespace.
Move spacing into the Razor view or layout instead of preserving it in
translator-owned strings.

The current CSoC profile covers:

- `/report-data/home-compliance-scheme` via `ComplianceSchemeLandingController`, where `FeatureManagement:ShowPrn` and `FeatureManagement:CsocEnabled` allow the CSoC bullet and paragraph in the PRN tile.
- `/report-data/manage-your-recycling-obligations` via `PrnsObligationController`, where `FeatureManagement:ShowPrn` and `FeatureManagement:CsocEnabled` allow the CSoC status card.
- `/report-data/home-self-managed` via `FrontendSchemeRegistrationController`, where `FeatureManagement:ShowPrn` and `FeatureManagement:CsocEnabled` allow the same shared CSoC bullet and paragraph in the PRN tile.
- `Csoc:WasteObligationsBaseAddress`, which is used by `CsocHelper` to build statement and certificate links.

Shared content is exported only once. If a profiled page only reuses entries
already included by an earlier page, export logs that there is nothing to
include for that page and does not generate an empty workbook.

To import translated workbooks:

```bash
dotnet run --project tools/translations/cli/cli.csproj -- import --profile csoc
```

Blank Welsh cells preserve the existing Welsh RESX value. Conflicting non-blank translations for the same hidden translation key fail the import.

Import also validates that translated values preserve source placeholders and
markup, and that translated values do not start or end with whitespace. Use
decoded tags such as `<strong>` in workbooks, not RESX/XML entities such as
`&lt;strong&gt;`; the import process writes the correct RESX encoding.

## Adding profiles or pages

Add or update JSON under `tools/translations/profiles`. A page entry should include:

- `route`: the public route or a short process label.
- `view`: the Razor view that renders the page.
- `featureFlags`: flags that affect whether the content is shown.
- `appSettings`: settings that influence the rendered content or links.
- `resources`: source `.en.resx` files plus optional `keys` or `keyPrefixes`.

If `keys` and `keyPrefixes` are omitted for a resource, all string entries from that RESX file are exported. Shared content is exported only once; later pages get a translator note naming the workbook that owns it. If no rows remain for a later page, no workbook is generated for that page.

## Refreshing a profile after UI changes

The exporter does not crawl the whole MVC app and infer ownership by itself. A
profile is the source of truth for which pages and RESX files belong in a
translation export. When CSoC or another profiled journey changes, re-audit the
profile before exporting.

For the `csoc` profile, use discovery paths rather than only checking the
files currently named in the profile:

1. Find current CSoC entry points:

   ```bash
   rg -n "Csoc|CSoC|CsocEnabled|CsocViewModel|Partials/Csoc|ComplianceDeclarationStatus|certificate of compliance|statement of compliance" src/FrontendSchemeRegistration.UI src/FrontendSchemeRegistration.Application -g '!bin/**' -g '!obj/**' -g '!node_modules/**'
   ```

2. For each hit, trace from the controller action or helper back to the route,
   feature gates and app settings. Check route constants in
   `src/FrontendSchemeRegistration.Application/Constants/PagePaths.cs`, action
   attributes such as `[Route(...)]` and `[FeatureGate(...)]`, and any calls to
   `CsocHelper.CreateViewModel` or `IFeatureManager.IsEnabledAsync`.

3. Trace each matching controller endpoint to its Razor view. For CSoC this
   currently starts with:

   - `ComplianceSchemeLandingController` / `/report-data/home-compliance-scheme`
   - `FrontendSchemeRegistrationController` / `/report-data/home-self-managed`
   - `PrnsObligationController` / `/report-data/manage-your-recycling-obligations`

4. In those views, follow every CSoC partial, view component or localizer call,
   such as:

   - `Partials/Csoc/_LandingBullet`
   - `Partials/Csoc/_LandingParagraph`
   - `Partials/Csoc/_ObligationsHome`

5. For each view or partial, find the matching English RESX file under
   `src/FrontendSchemeRegistration.UI/Resources`. Use the MVC resource path
   convention first, then confirm by searching for the resource keys used by
   the Razor file. Shared CSoC partial resources are currently under:

   ```text
   src/FrontendSchemeRegistration.UI/Resources/Views/Shared/Partials/Csoc
   ```

6. Add any new `.en.resx` file to `tools/translations/profiles/csoc.json`.
   If only some entries in the file are CSoC-specific, add `keys` or
   `keyPrefixes` to the resource entry. If the whole file belongs to the page,
   omit both and the exporter will include every string entry.

7. Keep a page in the profile even if it only renders shared CSoC resources
   already owned by another page. The exporter will log that there is nothing
   to include and skip the empty workbook, while the profile still records that
   the route was audited.

8. Record the feature flags and app settings that control visibility. For CSoC,
   check `FeatureManagement:CsocEnabled`, `FeatureManagement:ShowPrn`, and
   `Csoc:WasteObligationsBaseAddress`.

9. Export to a scratch directory and inspect the row counts and skipped-page
   logs:

   ```bash
   dotnet run --project tools/translations/cli/cli.csproj -- export --profile csoc --output /tmp/epr-packaging-csoc-translations
   ```

10. Import from the scratch export to prove the hidden translation keys still map
   back to real source and Welsh RESX files:

   ```bash
   dotnet run --project tools/translations/cli/cli.csproj -- import --profile csoc --input /tmp/epr-packaging-csoc-translations
   ```

11. Once the profile is correct, regenerate the default workbooks:

   ```bash
   dotnet run --project tools/translations/cli/cli.csproj -- export --profile csoc
   ```

Do not create Welsh translations manually. Only import or copy Welsh text from
an approved source when the English string and UI placement match.

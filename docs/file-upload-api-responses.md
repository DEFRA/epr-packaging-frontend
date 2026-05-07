# File Upload API Responses

This document captures the API responses the frontend expects when a user uploads a packaging data file. The flow involves two API calls:

1. **Initial upload** – `POST /api/v1/file-upload` (returns submission ID)
2. **Status polling** – `GET /api/v1/submissions/{id}` (FileUploading page polls every 5 seconds until validation completes)

The FileUploading page (`/report-data/file-uploading?submissionId={id}`) reloads every 5 seconds. On each request it fetches the submission and checks `PomDataComplete` or `Errors.Count > 0` to determine whether validation has finished. It then routes based on `ValidationPass`, `HasWarnings`, and `Errors`.

---

## 1. Initial File Upload Response

**Request:** `POST /api/v1/file-upload`

**Expected response (success):**

| Status | Headers | Body |
|--------|---------|------|
| `200 OK` | `Content-Type: application/json` | `{}` (empty object) |
| | `Location: /api/v1/submissions/{submissionId}` | |

The frontend extracts the submission ID from the `Location` header (the last path segment) and redirects the user to `/report-data/file-uploading?submissionId={id}`.

---

## 2. Success – Check File and Submit Page

**When:** Validation passes with no errors and no warnings.

**Expected `GET /api/v1/submissions/{id}` response:**

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "type": "Producer",
  "submissionPeriod": "2024",
  "pomDataComplete": true,
  "validationPass": true,
  "hasWarnings": false,
  "errors": [],
  "pomFileName": "packaging-data.csv",
  "pomFileUploadDateTime": "2024-01-15T10:30:00Z",
  "lastUploadedValidFile": {
    "fileName": "packaging-data.csv",
    "uploadedDateTime": "2024-01-15T10:30:00Z",
    "uploadedBy": "user@example.com",
    "fileId": "00000000-0000-0000-0000-000000000002"
  }
}
```

**Key fields:**
- `pomDataComplete`: `true` – validation finished
- `validationPass`: `true` – no blocking errors
- `hasWarnings`: `false`
- `errors`: `[]`

**Result:** User is redirected to **File Upload Check File and Submit** page (`FileUploadCheckFileAndSubmit`). They see file details and can submit to the regulator.

---

## 3. Errors – File Upload Failure Page

**When:** Validation fails (blocking errors). May also include warnings.

**Expected `GET /api/v1/submissions/{id}` response:**

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "type": "Producer",
  "submissionPeriod": "2024",
  "pomDataComplete": true,
  "validationPass": false,
  "hasWarnings": false,
  "errors": [
    "Row 5: Column 'organisation_id' contains an invalid value.",
    "Row 12: Required field 'packaging_activity' is missing."
  ],
  "pomFileName": "packaging-data.csv",
  "pomFileUploadDateTime": "2024-01-15T10:30:00Z"
}
```

**Key fields:**
- `pomDataComplete`: `true` – validation finished
- `validationPass`: `false` – blocking errors
- `hasWarnings`: optional; if `true`, copy refers to “errors and warnings”
- `errors`: non-empty array of error messages

**Result:** User is redirected to **File Upload Failure** page (`FileUploadFailure`). They see:
- Title: "Packaging data not uploaded – fix the errors and try again" (or "…fix the errors and check the warnings" if `hasWarnings` is true)
- Instructions to download an error report
- Link to upload a new file

**Exception errors path:** If `errors` is populated *before* `pomDataComplete` is true (e.g. upload rejected immediately), the user is redirected back to the **File Upload** form with `showErrors=true` and the errors shown in `ModelState` on that page.

---

## 4. Warnings – File Upload Warning Page

**When:** Validation passes but there are non-blocking warnings.

**Expected `GET /api/v1/submissions/{id}` response:**

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "type": "Producer",
  "submissionPeriod": "2024",
  "pomDataComplete": true,
  "validationPass": true,
  "hasWarnings": true,
  "errors": [],
  "pomFileName": "packaging-data.csv",
  "pomFileUploadDateTime": "2024-01-15T10:30:00Z",
  "lastUploadedValidFile": {
    "fileName": "packaging-data.csv",
    "uploadedDateTime": "2024-01-15T10:30:00Z",
    "uploadedBy": "user@example.com",
    "fileId": "00000000-0000-0000-0000-000000000002"
  }
}
```

**Key fields:**
- `pomDataComplete`: `true`
- `validationPass`: `true` – no blocking errors
- `hasWarnings`: `true`
- `errors`: `[]`

**Result:** User is redirected to **File Upload Warning** page (`FileUploadWarning`). They see:
- Title: "Check the warnings"
- Message that the file has been uploaded but there are warnings
- Link to download the warning report
- Options to upload a new file or keep the current one

---

## 5. Polling – Validation In Progress

**When:** File is uploaded but backend validation is still running.

**Expected `GET /api/v1/submissions/{id}` response:**

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "type": "Producer",
  "submissionPeriod": "2024",
  "pomDataComplete": false,
  "validationPass": false,
  "hasWarnings": false,
  "errors": []
}
```

**Key fields:**
- `pomDataComplete`: `false` – validation not finished
- `errors`: `[]` – no early errors

**Result:** FileUploading page is shown again (spinner). The page reloads every 5 seconds until `pomDataComplete` is `true` or `errors.Count > 0`.

---

## Routing Summary

| `pomDataComplete` | `errors.Count > 0` | `validationPass` | `hasWarnings` | Destination |
|-------------------|--------------------|------------------|---------------|-------------|
| `false` | `false` | - | - | FileUploading (keep polling) |
| `true` or `errors > 0` | `true` | - | - | FileUpload (show errors on form) |
| `true` | `false` | `false` | any | FileUploadFailure |
| `true` | `false` | `true` | `true` | FileUploadWarning |
| `true` | `false` | `true` | `false` | FileUploadCheckFileAndSubmit |

---

## Mock Server

The mock server in `FrontendSchemeRegistration.MockServer/WebApi/WebApi.cs` currently provides:

- `POST /api/v1/file-upload` → 200 with `Location: /api/v1/submissions/{guid}`
- `GET /api/v1/submissions` → `[]` (empty list)
- `GET /api/v1/submissions/{id}` → **not implemented**; adding a stub would allow testing the full file upload flow.

To support the file upload journey, add a `GET /api/v1/submissions/*` stub that returns a `PomSubmission`-shaped response with the appropriate `pomDataComplete`, `validationPass`, `hasWarnings`, and `errors` values for the scenario being tested.

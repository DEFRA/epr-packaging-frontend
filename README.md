# Frontend Scheme Registration

## Overview

Frontend Scheme Registration handles the front end POM and Organisation Details upload

## How To Run

### Prerequisites
In order to run the service you will need the following dependencies:
- .NET 8

#### epr-packaging-common
##### Developers working for a DEFRA supplier
In order to restore and build the source code for this project, access to the `epr-packaging-common` package store will need to have been setup.
 - Login to Azure DevOps
 - Navigate to [Personal Access Tokens](https://dev.azure.com/defragovuk/_usersSettings/tokens)
 - Create a new token
   - Enable the `Packaging (Read)` scope

Add the following to your `src/Nuget.Config`

```xml
<packageSourceCredentials>
  <epr-packaging-common>
    <add key="Username" value="<email address>" />
    <add key="ClearTextPassword" value="<personal access token>" />
  </epr-packaging-common>
</packageSourceCredentials>
```

##### Members of the public
Clone the [epr_common](https://dev.azure.com/defragovuk/RWD-CPR-EPR4P-ADO/_git/epr_common) repository and add it as a project to the solution you wish to use it in. By default the repository will reference the files as if they are coming from the NuGet package. You simply need to update the references to make them point to the newly added project.

### Run
Go to `src/FrontendSchemeRegistration.UI` directory and execute:

```
dotnet run
```

## Docker
Run in terminal at the solution source root:

```
docker build -t packagingfrontend -f FrontendSchemeRegistration.UI/Dockerfile .
```

To run the image, execute the following command:

```
docker run -p 5167:3000 --name packagingfrontendecontainer packagingfrontend
```

You can configure each appsetting by adding ```-e``` flag to the above command.

Do a GET Request to ```http://localhost:5167/admin/health``` to confirm that the service is running.

## Redis

### App settings
Add the following variables to appsettings.Development.json/appsettings.json:
```
"Redis__InstanceName": "epr-producers-",
"Redis__ConnectionString": "localhost:6379"
```

### To install Redis and Redis Stack
Recommended way of running Redis is to run it via Docker. In terminal run:
```
docker run -d --name epr-producers- -p 6379:6379 -p 8001:8001 redis/redis-stack:latest
```

### Inspect Redis keys in the session
To view the keys in Redis cache, open browser and point at: http://localhost:8001/redis-stack/browser

## MaxIssuesToProcess and MaxIssueReportSize app settings
This is currently set to 1000 in the app settings. Basic calculations/tests for this limit means the maximum error/warning report size will never be greater than 5MB.
If this issue limit increases the MaxIssueReportSize setting will also need changed to reflect this.

# How To Test

### Unit tests

On `src`, execute:

```
dotnet test
```

Some tests use [Verify](https://github.com/verifytests/verify) for snapshots to prevent the need for granular assertions. Plugins exist for IDEs such as Rider that allow group acceptance of changed snapshots.

### Time-travel testing

The UI can be time-travel tested by setting the `StartupUtcTimestampOverride` to an [RFC-3339](https://www.rfc-editor.org/rfc/rfc3339) compliant date, such as `2025-12-31T23:59:00Z`. This will be used as the initial timestamp when the service starts up. Time will progress as usual. Note that this should **NEVER** be set in production. This setting _will not_ set the system time in downstream services, so is only appropriate for testing UI logic. Note that this behaviour will be overwritten by the `Prn` configuration value if both `OverridePrnCurrentDateForTestingPurposes` and `ShowPrn` are set to `true`, in which case a _static_ value will be set for the current date.

### Running the mock server

It is possible to run the application locally against a mock server. This is useful for testing against a real API without having to set up a local instance of the API. The
mock server is used to replace the following endpoints:

```json
    "WebAPI:BaseEndpoint": "http://localhost:9091",
    "PaymentFacadeApi:BaseUrl": "http://localhost:9091",
    "EprAuthorizationConfig:FacadeBaseUrl": "http://localhost:9091/api/",
    "AccountsFacadeAPI:BaseEndpoint": "http://localhost:9091/api/"
```

Along with this the authentication can also be stubbed. This removes the requirement to connect to azure b2c. When the stub authentication is enabled, you are presented with
a login screen to enter a Id and email address. The Id is then used as part of the token generation for the user when communicating with the API. This means that we can
serve different responses based on the user that is logged in.

#### Running the mock server standalone (for manual testing)

Start the WireMock mock server in one terminal:

```bash
cd src
dotnet run --project FrontendSchemeRegistration.MockServer
```

Then start the UI application pointing at the mock server in another terminal. The environment **must** be `ComponentTest` for the stub login page to work (it returns 404 in any other environment). Use the `ComponentTest` launch profile:

```bash
cd src
dotnet run --project FrontendSchemeRegistration.UI --launch-profile ComponentTest \
  -- \
  --IsStubAuth=true \
  --UseLocalSession=true \
  --WebAPI:BaseEndpoint=http://localhost:9091 \
  --PaymentFacadeApi:BaseUrl=http://localhost:9091 \
  --EprAuthorizationConfig:FacadeBaseUrl=http://localhost:9091/api/ \
  --AccountsFacadeAPI:BaseEndpoint=http://localhost:9091/api/
```

Alternatively, skip the launch profile entirely and set the environment variable directly:

```bash
cd src
dotnet run --project FrontendSchemeRegistration.UI --no-launch-profile \
  --urls "https://localhost:7084;http://localhost:5145" \
  -- \
  --environment ComponentTest \
  --IsStubAuth=true \
  --UseLocalSession=true \
  --WebAPI:BaseEndpoint=http://localhost:9091 \
  --PaymentFacadeApi:BaseUrl=http://localhost:9091 \
  --EprAuthorizationConfig:FacadeBaseUrl=http://localhost:9091/api/ \
  --AccountsFacadeAPI:BaseEndpoint=http://localhost:9091/api/
```

You can then browse to `https://localhost:7084/report-data` and use the stub login screen. The port is defined in `Properties/launchSettings.json` (HTTPS: 7084, HTTP: 5145).

#### Component tests (WireMock integration tests)

The component test project (`FrontendSchemeRegistration.UI.Component.UnitTests`) runs the web application in-process using `WebApplicationFactory` with WireMock providing all external API responses. Tests are written as Reqnroll (BDD) feature files. Allure Report is configured for HTML test reports with support for attachments (e.g. screenshots, HTML dumps).

##### Running component tests locally

Run all WireMock-backed component tests:

```bash
cd src
dotnet test FrontendSchemeRegistration.UI.Component.UnitTests \
  --filter "Category=WireMockServer" \
  --logger "console;verbosity=detailed"
```

Run a specific feature file by scenario name:

```bash
cd src
dotnet test FrontendSchemeRegistration.UI.Component.UnitTests \
  --filter "Category=WireMockServer&Name~Producer" \
  --logger "console;verbosity=detailed"
```

##### Allure Report (HTML reports with attachments)

Component tests use [Allure.Reqnroll](https://allurereport.org/docs/reqnroll/) for HTML reporting. Test results are written to `allure-results/` at the repository root.

**Prerequisites:** [Allure Report CLI](https://allurereport.org/docs/v2/install/) (requires Java to be installed).

Run tests, then generate and view the report:

```bash
cd src
dotnet test FrontendSchemeRegistration.UI.Component.UnitTests \
  --filter "Category=WireMockServer" \
  --logger "console;verbosity=detailed"
```

From the repository root:

```bash
allure serve allure-results
```

This generates the report and opens it in your browser. Alternatively:

```bash
allure generate allure-results -o allure-report --clean
allure open allure-report
```

You can attach screenshots and other files from step definitions using `AllureApi.AddAttachment()` — see [Allure Reqnroll docs](https://allurereport.org/docs/reqnroll/).

##### Running component tests in CI/CD

A dedicated pipeline is available at `pipelines/component_test_pipeline.yaml`. To run from the command line in a CI environment:

```bash
dotnet test src/FrontendSchemeRegistration.UI.Component.UnitTests \
  --configuration Release \
  --filter "Category=WireMockServer" \
  --results-directory ./test-results \
  --logger "trx;LogFileName=component-tests.trx"
```

The test results will be output as a `.trx` file suitable for Azure DevOps test result publishing. For Allure reports in CI, ensure the `allure-results` directory is preserved as an artifact; then run `allure generate` in a subsequent job if needed.

##### Email-driven stub authentication

The stub login page has a single field: **Email**. The email address controls everything — user type, UK nation, and registration state. A `UserId` is auto-generated from the email (a deterministic SHA-256-based GUID) so each email address consistently maps to the same identity.

The `StubTokenAcquisition` encodes both the UserId and Email into the bearer token (format: `{userId}::{email}`). The mock server parses the email and checks for **keywords** (case-insensitive) to select different response variants.

###### User type keywords

Include a keyword in the email to set the user type. If no keyword matches, the default is **Compliance Scheme**.

| Keyword | User Type | Org Role | Homepage | Org Name |
|---------|-----------|----------|----------|----------|
| `producer` | Direct Producer | `Producer` | `/report-data/home-self-managed` | DIRECT PRODUCER LTD |
| `csmember` | CS-managed Producer | `Producer` | `/report-data/manage-compliance-scheme` | CS MEMBER PRODUCER LTD |
| *(default)* | Compliance Scheme | `Compliance Scheme` | `/report-data/home-compliance-scheme` | COMPLIANCE SCHEME LTD |

The routing is determined by two data points:
1. `organisationRole` in the `/api/user-accounts` response (`"Compliance Scheme"` vs `"Producer"`)
2. The `/api/compliance-schemes/get-for-producer/` response (204 = self-managed, 200 with data = CS-managed)

Three Reqnroll tags are still available as shortcuts for common scenarios:

| Tag | Email used |
|-----|-----------|
| `@AuthenticateComplianceSchemeUser` | `cs@test.com` |
| `@AuthenticateDirectProducerUser` | `producer@test.com` |
| `@AuthenticateDirectProducerNotStarted` | `producer.notstarted@test.com` (Not Started registration state; use for first-time file upload) |
| `@AuthenticateCsMemberProducerUser` | `csmember@test.com` |

For custom scenarios (e.g. specific nation + registration state), use the `Given I am logged in with email ...` step directly.

###### Registration state keywords

Include a keyword in the email to change the registration task list state:

| Email keyword | Task list state | Description |
|---------------|----------------|-------------|
| `notstarted` | Not Started | No file uploaded, all tasks locked |
| `uploaded` | File Uploaded (default) | File uploaded, awaiting processing |
| `fees` | Ready for Payment | File processed, fees calculated, payment not yet made |
| `paid` | Payment Complete | Fees paid, application not yet submitted |
| `completed` | Fully Submitted | Application submitted to regulator |
| `accepted` | Accepted | Accepted by regulator |
| `rejected` | Rejected | Rejected by regulator (file upload resets) |
| `queried` | Queried | Queried by regulator (file upload resets) |

**Examples:**

- `completed@test.com` → Compliance Scheme, England, fully submitted task list
- `producer.scotland.fees@test.com` → Direct Producer, Scotland, fees stage
- `csmember.wales@test.com` → CS-managed Producer, Wales, default state
- `cs.rejected@test.com` → Compliance Scheme, England, rejected registration
- `test@test.com` → Compliance Scheme (default), England (default), uploaded state (default)

If no keyword is matched, the default response files are used (`SmallProducerRegistrationApplicationDetails.json` / `LargeProducerRegistrationApplicationDetails.json`).

The response files are located in `FrontendSchemeRegistration.MockServer/WebApi/Responses/RegistrationTaskList/` and follow the naming convention `{SmallProducer|LargeProducer}{Suffix}.json`.

To extend this pattern to other endpoints, use `StubToken.ExtractEmail(req)` in the mock server's `WithCallback` handlers and check for keywords.

###### Packaging data (POM) sub-landing status keywords

The **File Upload Sub Landing** page (`/report-data/file-upload-sub-landing`) shows a status tag on each submission period card. Include one of the following prefixed keywords in the email to drive a specific POM status for the `January to June 2025` period:

| Email keyword | POM status shown | Description |
|---------------|-----------------|-------------|
| `pom.accepted` | Accepted by Regulator (green) | Submission accepted; warning banner shown |
| `pom.rejected` | Rejected by Regulator (red) | Submission rejected; resubmit link shown |
| `pom.resubfees.pending` | In Progress | Resubmission underway — fees **not yet viewed**; fee page shows £2,500 outstanding |
| `pom.resubfees.viewed` | In Progress | Resubmission underway — fees **viewed**; fee page shows zero outstanding (fee paid) |
|| `pom.resubfees.nomembers` | In Progress | Resubmission underway — **memberCount=0**; fee page shows “no additional fee to pay” |

**Examples:**

- `pom.accepted@test.com` → Compliance Scheme user, packaging data accepted
- `pom.rejected@test.com` → Compliance Scheme user, packaging data rejected
- `pom.resubfees.pending@test.com` → Compliance Scheme user, resubmission in progress; £2,500 fee due on fee page
- `pom.resubfees.viewed@test.com` → Compliance Scheme user, resubmission in progress; zero fee outstanding on fee page
- `pom.resubfees.nomembers@test.com` → Compliance Scheme user, resubmission in progress; memberCount=0, no additional fee to pay

> **Note:** The `pom.` prefix is required to distinguish these keywords from the registration state keywords above (e.g. `accepted@test.com` drives the registration state, not POM status).

For the `pom.resubfees.*` emails the full resubmission fee journey is supported end-to-end:

1. Sub-landing page (`/report-data/file-upload-sub-landing`) — shows **In Progress** for January to June 2025.
2. Continuing from the sub-landing redirects to the **Resubmission Task List** (`/report-data/resubmission-task-list`).
3. The **Resubmission Fee Calculations** page (`/report-data/resubmission-fee-calculations`) shows:
   - `pom.resubfees.pending` → **"Packaging data resubmission fee due"** — £2,500.00 outstanding
   - `pom.resubfees.viewed` → **"Packaging data resubmission fee paid"** — zero outstanding
   - `pom.resubfees.nomembers` → **"You have no additional fee to pay"** — memberCount=0 (CS with no chargeable members)

##### Nation-driven mock responses

The email address also controls the **UK nation** returned by the mock server. This affects the organisation's `nationId`, the compliance scheme's `NationId`, and the CS summary's `Nation` field, which in turn determines the environmental regulator displayed on the CS homepage.

| Email keyword | Nation | Regulator shown on CS homepage |
|---------------|--------|-------------------------------|
| `england` (default) | England | the Environment Agency |
| `scotland` | Scotland | the Scottish Environment Protection Agency |
| `wales` | Wales | Natural Resources Wales |
| `northernireland` | Northern Ireland | the Northern Ireland Environment Agency |

If no nation keyword is found in the email, the default nation is **England**. Nation keywords can be combined with registration task list keywords, e.g. `completed.scotland@test.com` returns a completed task list for a Scottish organisation.

##### Adding new tests

1. Add new page URLs to `Data/Pages.cs` if testing a new route
2. Add new WireMock stubs to `FrontendSchemeRegistration.MockServer/WebApi/` if the page calls APIs not yet mocked
3. Create a new `.feature` file in `Features/` using existing step definitions from `Steps/HttpSteps.cs` and `Steps/ContentSteps.cs`
4. Tag scenarios with `@WireMockServer` (required) and either:
   - Use an auth tag (`@AuthenticateComplianceSchemeUser`, `@AuthenticateDirectProducerUser`, or `@AuthenticateCsMemberProducerUser`) for default scenarios, or
   - Use `Given I am logged in with email <email>` for fine-grained control over user type, nation, and registration state

### Running against the epr-local-environment

The UI project can be run against the epr-local-environment Docker env.

See specific instructions for packaging https://github.com/DEFRA/epr-local-environment/?tab=readme-ov-file#packaging and follow the steps to run all dependent packaging services.

Once all are running, stop the epr-packaging-frontend instance running in Docker as they both share the same ports:

```
docker stop epr-local-environment-epr-packaging-frontend-1
```

Manage the secrets of the UI project in this solution. In Rider, right click the UI project > Tools > .NET User Secrets

Complete the following config and save it as your secrets.json:

Note:

- [to be completed] - grab the details from a fellow developer
- [complete full path to local env cert] - this needs to be the absolute path
  - the cert also needs to be trusted locally. see https://github.com/DEFRA/epr-local-environment/tree/main/compose/certs#trusting-the-certificate

```
{
  "AccountsFacadeAPI": {
    "BaseEndpoint": "https://localhost:7253/api/"
  },
  "EprAuthorizationConfig": {
    "FacadeBaseUrl": "https://localhost:7253/api/"
  },
  "PaymentFacadeApi": {
    "BaseUrl": "https://localhost:7166/api/v1/"
  },
  "HttpClient": {
    "AppServiceUrl": "https://localhost:7265/"
  },
  "WebAPI": {
    "BaseEndpoint": "https://localhost:7265/"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "AzureADB2C": {
    "ClientId": "[to be completed]",
    "ClientSecret": "[to be completed]",
    "Instance": "https://AZDCUSPOC2.b2clogin.com",
    "Tenant": "[to be completed]",
    "Domain": "AZDCUSPOC2.onmicrosoft.com",
  },
  "Kestrel": {
    "Certificates": {
      "Default": {
        "Path": "/[complete full path to local env cert]/epr-local-environment/compose/certs/https/aspnetapp.pfx",
        "Password": "password"
      }
    }
  }
}
```


## How To Debug
Use debugging tools in your chosen IDE.

## Environment Variables - deployed environments
The structure of the appsettings can be found in the repository. Example configurations for the different environments can be found in [epr-app-config-settings](https://dev.azure.com/defragovuk/RWD-CPR-EPR4P-ADO/_git/epr-app-config-settings).

| Variable Name                                              | Description                                                                                                      |
|------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------|
| Logging__LogLevel__Default                                 | Default logging level                                                                                            |
| FeatureManagement__ShowLanguageSwitcher                    | Show language switcher                                                                                           |
| FeatureManagement__ShowComplianceSchemeMemberManagement    | Show compliance scheme member management                                                                         |
| UseLocalSession                                            | Use local session                                                                                                |
| Session__IdleTimeoutMinutes                                | Number of minutes before the session should timeout if idle (in minutes)                                         |
| Cookie__SessionCookieName                                  | Name of the session cookie                                                                                       |
| Cookie__CookiePolicyCookieName                             | Name of the cookie policy cookie                                                                                 |
| Cookie__AntiForgeryCookieName                              | Name of the anti-forgery cookie                                                                                  |
| Cookie__AuthenticationCookieName                           | Name of the authentication cookie                                                                                |
| Cookie__TsCookieName                                       | Name of the Ts cookie                                                                                            |
| Cookie__TempDataCookie                                     | Name of the Temp Data cookie                                                                                     |
| Cookie__B2CCookieName                                      | Name of the B2C cookie                                                                                           |
| Cookie__CorrelationCookieName                              | Name fo the Correlation cookie                                                                                   |
| Cookie__OpenIdCookieName                                   | Name of the Open Id cookie                                                                                       |
| Cookie__AuthenticationExpiryInMinutes                      | Time before the Authentication expires (in minutes)                                                              |
| Cookie__CookiePolicyDurationInMonths                       | Time before cookies need to be reaccepted (in months)                                                            |
| BasePath                                                   | URL path of the application                                                                                      |
| HttpClient__RetryCount                                     | Number of retries should connection to HTTP client fail                                                          |
| HttpClient__RetryDelaySeconds                              | Time between each HTTP client retry (in seconds)                                                                 |
| HttpClient__UserAgent                                      | The user agent                                                                                                   |
| HttpClient__TimeoutSeconds                                 | Time until the connection to the HTTP client times out (in seconds)                                              |
| HttpClient__AppServiceUrl                                  | Application service URL                                                                                          |
| FileUploadLimitInBytes                                     | Maximum file upload size in bytes                                                                                |
| SchemeYear                                                 | The current scheme year                                                                                          |
| ForwardedHeaders__ForwardedHostHeaderName                  | Sets the header used to retrieve the forwarded value of the Host header field                                    |
| ForwardedHeaders__OriginalHostHeaderName                   | Sets the header used to retrieve the original value of the Host header field                                     |
| ForwardedHeaders__AllowedHosts                             | Base URL of the front-end to be included as a header for B2C requests                                            |
| AzureAdB2C__Instance                                       | B2C Instance Name                                                                                                |
| AzureAdB2C__Domain                                         | B2C Domain Name                                                                                                  |
| AzureAdB2C__ClientId                                       | B2C Client Id (for the front-end app registration)                                                               |
| AzureAdB2C__ClientSecret                                   | B2C Client Secret (generated against the front-end app registration)                                             |
| AzureAdB2C__SignUpSignInPolicyId                           | The User Flow for SignUp/SignIn created in B2C                                                                   |
| AzureAdB2C__SignedOutCallbackPath                          | Path in the application which handles sign-out                                                                   |
| AzureAdB2C__ResetPasswordPolicyId                          | Azure ADB2C id to reset password policy                                                                          |
| AzureAdB2C__EditProfilePolicyId                            | Azure ADB2C id to edit profile policy                                                                            |
| AzureAdB2C__CallbackPath                                   | Callback path for Azure ADB2C                                                                                    |
| AzureAdB2C__ExtensionsClientId                             | Azure ADB2C Extensions App client id used by Graph API                                                           |
| AzureAdB2C__TenantId                                       | Azure ADB2C tenant id                                                                                            |
| AccountsFacadeAPI__BaseEndpoint                            | URL of the account facade                                                                                        |
| AccountsFacadeAPI__DownstreamScope                         | Downstream scope for the account facade                                                                          |
| WebAPI__BaseEndpoint                                       | Base endpoint for the Web API                                                                                    |
| WebAPI__DownstreamScope                                    | Downstream scope for the Web API                                                                                 |
| Caching__CacheNotifications                                | Enable or disable caching notifications                                                                          |
| Caching__CacheComplianceSchemeSummaries                    | Enable or disable caching compliance scheme summaries                                                            |
| Caching__SlidingExpirationSeconds                          | Cache expiry if not accessed (in seconds)                                                                        |
| Caching__AbsoluteExpirationSeconds                         | Cache expiry (in seconds)                                                                                        |
| ComplianceSchemeMembersPagination__PageSize                | PageSize URL parameter for SchemeMembers endpoint                                                                |
| FrontEndAccountManagement__BaseUrl                         | Base URL for the Frontend Account Management                                                                     |
| FrontEndAccountCreation__BaseUrl                           | Base URL for the Frontend Account Creation                                                                       |
| PhaseBanner__ApplicationStatus                             | Status to be showed in the phase banner (ie__ Alpha)                                                             |
| PhaseBanner__SurveyUrl                                     | URL for the link in the phase banner to the survey                                                               |
| PhaseBanner__Enabled                                       | Display phase banner                                                                                             |
| EprAuthorizationConfig__FacadeBaseUrl                      | URL to the account facade (Don't rename variable name, it's used in EPR.Common)                                  |
| EprAuthorizationConfig__FacadeUserAccountEndpoint          | Endpoint in the account facade for users (Don't rename variable name, it's used in EPR.Common)                   |
| EprAuthorizationConfig__FacadeDownStreamScope              | B2C scope for account facade (Don't rename variable name, it's used in EPR.Common)                               |
| EmailAddresses__DataProtection                             | Email address for data protection queries                                                                        |
| EmailAddresses__DefraGroupProtectionOfficer                | Email address for the DEFRA group protection officer                                                             |
| SiteDates__PrivacyLastUpdated                              | Last time the privacy policy was updated                                                                         |
| SiteDates__DateFormat                                      | Data format for site dates                                                                                       |
| ExternalUrls__GovUkHome                                    | Link to Gov UK home                                                                                              |
| ExternalUrls__LandingPage                                  | Link to Privacy Scottish Environmental Protection Agency                                                         |
| ExternalUrls__PrivacyScottishEnvironmentalProtectionAgency | Link to Privacy National Resources Wales                                                                         |
| ExternalUrls__PrivacyNationalResourcesWales                | Link to Privacy Northern Ireland Environmental Agency                                                            |
| ExternalUrls__PrivacyNorthernIrelandEnvironmentAgency      | Link to Northern Ireland Enviornmental Agency                                                                    |
| ExternalUrls__PrivacyEnvironmentAgency                     | Link to Privacy Environmental Agency                                                                             |
| ExternalUrls__PrivacyDataProtectionPublicRegister          | Link to Privacy Data Protection Public Register                                                                  |
| ExternalUrls__PrivacyDefrasPersonalInformationCharter      | Link to Privacy DEFRA's Personal Information Charter                                                             |
| ExternalUrls__PrivacyInformationCommissioner               | Link to Privacy Information Commissioner                                                                         |
| GuidanceLinks__WhatPackagingDataYouNeedToCollect           | Guidance link used on file upload sub landing page                                                               |
| GuidanceLinks__HowToBuildCsvFileToReportYourPackagingData  | Guidance link used on file upload sub landing page                                                               |
| GuidanceLinks__HowToReportOrganisationDetails              | Guidance link for organisation details, brands and partnerships                                                  |
| SubmissionPeriods__0__DataPeriod                           | First submission period                                                                                          |
| SubmissionPeriods__0__StartMonth                           | First submission period’s start month                                                                            |
| SubmissionPeriods__0__EndMonth                             | First submission period’s end month                                                                              |
| SubmissionPeriods__0__Deadline                             | First submission period’s deadline                                                                               |
| SubmissionPeriods__0__ActiveFrom                           | First submission period’s active from                                                                            |
| SubmissionPeriods__1__DataPeriod                           | Second submission period                                                                                         |
| SubmissionPeriods__1__Deadline                             | Second submission period’s deadline                                                                              |
| SubmissionPeriods__1__StartMonth                           | Second submission period’s start month                                                                           |
| SubmissionPeriods__1__EndMonth                             | Second submission period’s end month                                                                             |
| SubmissionPeriods__1__ActiveFrom                           | Second submission period’s active from                                                                           |
| GoogleAnalytics__CookiePrefix                              | Google Analytics cookie prefix                                                                                   |
| GoogleAnalytics__MeasurementId                             | Google Analytics measurement id                                                                                  |
| GoogleAnalytics__TagManagerContainerId                     | Google Analytics tag manager container id                                                                        |
| MSAL__DisableL1Cache                                       | Microsoft Authentication Library: disable L1/InMemory cache (useful where multiple apps share the same L2 cache) |
| MSAL__L2SlidingExpiration                                  | Microsoft Authentication Library: how long a cache entry can be inactive before it will be removed               |
| Redis__ConnectionString                                    | Connection string to Redis                                                                                       |
| Redis__InstanceName                                        | Redis Cache instance name                                                                                        |

## Additional Information

### Monitoring and Health Check
A health check can be found at ```{BASE_URL}/admin/health```

### Source files

- `FrontendSchemeRegistration/FrontendSchemeRegistration.Application` - Application .NET source files
- `FrontendSchemeRegistration/FrontendSchemeRegistration.Application.UnitTests` - Application .NET unit test files
- `FrontendSchemeRegistration/FrontendSchemeRegistration.PactTests` - .NET Pact tests
- `FrontendSchemeRegistration/FrontendSchemeRegistration.UI` - UI .NET unit source files
- `FrontendSchemeRegistration/FrontendSchemeRegistration.UI.UnitTests` - UI .NET unit test files
- `FrontendSchemeRegistration/FrontendSchemeRegistration.UI.Component.UnitTests` - WireMock component/integration tests (Reqnroll BDD)
- `FrontendSchemeRegistration/FrontendSchemeRegistration.MockServer` - WireMock mock server for local development and testing

## Contributing to this project

Please read the [contribution guidelines](CONTRIBUTING.md) before submitting a pull request.

## Licence

[Licence information](LICENCE.md).

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

## Contributing to this project

Please read the [contribution guidelines](CONTRIBUTING.md) before submitting a pull request.

## Licence

[Licence information](LICENCE.md).
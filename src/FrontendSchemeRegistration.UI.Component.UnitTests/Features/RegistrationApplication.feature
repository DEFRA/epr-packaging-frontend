Feature: Registration Application
As an authenticated user
I want to access the registration application pages
So that I can complete my producer registration

@AuthenticateComplianceSchemeUser
@WireMockServer
Scenario: Navigate to registration guidance page redirects to task list when guidance already completed
    When I navigate to the Registration Guidance Page
    Then I am redirected to the: Registration Task List Page
    And the page redirect content includes the following: Submit registration data
    And the page redirect content includes the following: View registration fee
    And the page redirect content includes the following: Submit registration application

@AuthenticateComplianceSchemeUser
@WireMockServer
Scenario: Navigate to registration task list page shows task list
    When I navigate to the Registration Task List Page
    Then I am on the Registration Task List Page
    And the page content includes the following: Submit registration data
    And the page content includes the following: View registration fee
    And the page content includes the following: Submit registration application

@WireMockServer
Scenario: Navigate to registration guidance page as unauthenticated user redirected to login
    When I navigate to the Registration Guidance Page
    Then I am redirected to the: Login Page

@WireMockServer
Scenario Outline: Resubmitting for a Compliance Scheme - <expectedPageHeader>
    Given I am logged in with email notstarted@test.com
    And I use registration journey <registrationJourney>
    And I have completed company details upload for a compliance scheme
    When I browse to the following url following redirects: /report-data/<pagePath>
    Then the page is successfully returned
    And the page content includes the following: <expectedPageHeader>
    And the page content includes the following: <expectedRegistrationHeader>

    Examples:
      | registrationJourney | expectedPageHeader                      | expectedRegistrationHeader          | pagePath                                                                                               |
      | CsoLargeProducer   | Large producer 2026 registration         | COMPLIANCE SCHEME LTD               | registration-task-list?registrationjourney=CsoLargeProducer&registrationyear=2026                     |
      | CsoLargeProducer   | Large producer 2026 registration         | Check files and submit              | review-organisation-data?submissionId=__SUBMISSION_ID__&registrationyear=2026&registrationjourney=CsoLargeProducer |
      | CsoLargeProducer   | Large producer 2026 registration         | Declaration                         | declaration-enter-full-name?submissionId=__SUBMISSION_ID__&registrationyear=2026&registrationjourney=CsoLargeProducer |
    #   | CsoLargeProducer   | Large producer 2026 registration         | Large producer 2026 registration    | file-upload-company-details/confirm-upload?submissionId=__SUBMISSION_ID__&registrationyear=2026&registrationjourney=CsoLargeProducer |
      | CsoSmallProducer   | Small producer 2026 registration         | COMPLIANCE SCHEME LTD               | registration-task-list?registrationjourney=CsoSmallProducer&registrationyear=2026                     |
      | CsoSmallProducer   | Small producer 2026 registration         | Check files and submit              | review-organisation-data?submissionId=__SUBMISSION_ID__&registrationyear=2026&registrationjourney=CsoSmallProducer |
      | CsoSmallProducer   | Small producer 2026 registration         | Declaration                         | declaration-enter-full-name?submissionId=__SUBMISSION_ID__&registrationyear=2026&registrationjourney=CsoSmallProducer |
    #   | CsoSmallProducer   | Small producer 2026 registration         | Small producer 2026 registration    | file-upload-company-details/confirm-upload?submissionId=__SUBMISSION_ID__&registrationyear=2026&registrationjourney=CsoSmallProducer |


@WireMockServer
Scenario Outline: Resubmitting for a Compliance Scheme - Large 2026 - brand and partnership journey pages
    Given I am logged in with email notstarted@test.com
    And I use registration journey <registrationJourney>
    And I have completed company details upload for a compliance scheme
    When I browse to the following url following redirects: /report-data/registration-task-list?registrationjourney=<registrationJourney>&registrationyear=2026
    And I start the file upload step
    And I upload a valid CSV file
    Then I am on the Company Details Success Page
    And the page content includes the following: <expectedHeader>
    When I browse to the following url following redirects: /report-data/organisation-details-uploaded?submissionId=__SUBMISSION_ID__&registrationyear=2026&registrationjourney=<registrationJourney>
    Then the page is successfully returned
    And the page content includes the following: Organisation details uploaded
    And the page content includes the following: <expectedHeader>
    When I browse to the following url following redirects: /report-data/upload-brand-details?submissionId=__SUBMISSION_ID__&registrationyear=2026&registrationjourney=<registrationJourney>
    Then the page is successfully returned
    And the page content includes the following: Upload brand details
    And the page content includes the following: <expectedHeader>
    When I upload a valid brands CSV file
    And I browse to the following url following redirects: /report-data/file-upload-brands-success?submissionId=__SUBMISSION_ID__&registrationyear=2026
    Then the page is successfully returned
    And the page content includes the following: Brand details uploaded
    And the page content includes the following: <expectedHeader>
    When I browse to the following url following redirects: /report-data/upload-partner-details?submissionId=__SUBMISSION_ID__&registrationyear=2026
    Then the page is successfully returned
    And the page content includes the following: Upload partner details
    And the page content includes the following: <expectedHeader>
    When I upload a valid partnerships CSV file
    And I browse to the following url following redirects: /report-data/file-upload-partnerships-success?submissionId=__SUBMISSION_ID__&registrationyear=2026
    Then the page is successfully returned
    And the page content includes the following: Partner details uploaded
    And the page content includes the following: <expectedHeader>

    Examples:
      | registrationJourney | expectedHeader                  |
      | CsoLargeProducer   | Large producer 2026 registration |
      | CsoSmallProducer   | Small producer 2026 registration |

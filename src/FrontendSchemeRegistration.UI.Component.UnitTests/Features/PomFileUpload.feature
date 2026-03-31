Feature: POM File Upload
As an authenticated user
I want to upload a packaging data file
So that I can submit my packaging data for the relevant submission period

  @WireMockServer
  @AuthenticateDirectProducerUser
  @PomUploadSuccess
  Scenario: Successful POM file upload with no errors or warnings
    Given I have navigated to the File Upload Sub Landing Page
    When I select a submission period and start the POM file upload
    And I upload a valid POM CSV file
    Then I am on the POM File Upload Success Page
    And the page content includes the following: Packaging data uploaded – check and submit
    And the page content includes the following: Your file has saved and you can submit it to the environmental regulator.

  @WireMockServer
  @AuthenticateDirectProducerUser
  @PomUploadWarnings
  Scenario: Successful POM file upload with warnings
    Given I have navigated to the File Upload Sub Landing Page
    When I select a submission period and start the POM file upload
    And I upload a valid POM CSV file
    Then I am on the POM File Upload Warnings Page
    And the page content includes the following: Packaging data uploaded – check the warnings
    And the page content includes the following: There are some warnings.

  @WireMockServer
  @AuthenticateDirectProducerUser
  @PomUploadErrors
  Scenario: POM file upload fails validation
    Given I have navigated to the File Upload Sub Landing Page
    When I select a submission period and start the POM file upload
    And I upload a valid POM CSV file
    Then I am on the POM File Upload Errors Page
    And the page content includes the following: Packaging data not uploaded – fix the errors and try again
    And the page content includes the following: Your data file has not been uploaded to your organisation's account.

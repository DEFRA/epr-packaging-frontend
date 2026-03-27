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

  @WireMockServer
  @AuthenticateDirectProducerUser
  @PomUploadWarnings
  Scenario: Successful POM file upload with warnings
    Given I have navigated to the File Upload Sub Landing Page
    When I select a submission period and start the POM file upload
    And I upload a valid POM CSV file
    Then I am on the POM File Upload Warnings Page

  @WireMockServer
  @AuthenticateDirectProducerUser
  @PomUploadErrors
  Scenario: POM file upload fails validation
    Given I have navigated to the File Upload Sub Landing Page
    When I select a submission period and start the POM file upload
    And I upload a valid POM CSV file
    Then I am on the POM File Upload Errors Page

Feature: Submission Confirmations
As an authenticated user
I want to see a success confirmation when I submit a file
So that I know my submission has been received

  @WireMockServer
  @AuthenticateComplianceSchemeUser
  @PomUploadSuccess
  Scenario: Successful packaging data submission displays confirmation panel
    Given I have navigated to the File Upload Sub Landing Page
    When I select a submission period and start the POM file upload
    And I upload a valid POM CSV file
    And I confirm and submit the packaging data
    Then the page is successfully returned
    And the page content includes the following: Packaging data submitted to the environmental regulator

  @WireMockServer
  @AuthenticateDirectProducerNotStarted
  @RegistrationUploadSuccess
  Scenario: Successful registration file upload displays success notification banner
    Given I have navigated to the Registration Guidance Page
    When I continue to the Registration Task List
    And I start the file upload step
    And I upload a valid CSV file
    Then the page is successfully returned
    And the page content includes the following: Success
    And the page content includes the following: Organisation details uploaded

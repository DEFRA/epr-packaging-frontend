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
    Then I am on the POM Submission Confirmation Page
    And the page content includes the following: Packaging data submitted to the environmental regulator

  @WireMockServer
  @AuthenticateDirectProducerNotStarted
  @RegistrationUploadSuccess
  # Verifies the "Success" notification banner heading is shown after a successful upload.
  # The URL check and "Organisation details uploaded" content are covered by RegistrationFileUpload.feature.
  Scenario: Successful registration file upload displays success notification banner
    Given I have navigated to the Registration Guidance Page
    When I continue to the Registration Task List
    And I start the file upload step
    And I upload a valid CSV file
    Then I am on the Company Details Success Page
    And the page content includes the following: Success

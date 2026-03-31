Feature: Registration File Upload
As an authenticated user
I want to upload a registration organisation details file
So that I can complete my EPR registration

  # --- Direct Producer scenarios ---

  @WireMockServer
  @AuthenticateDirectProducerNotStarted
  @RegistrationUploadSuccess
  Scenario: Direct Producer - Successful file upload and validation
    Given I have navigated to the Registration Guidance Page
    When I continue to the Registration Task List
    And I start the file upload step
    And I upload a valid CSV file
    Then I am on the Company Details Success Page
    And the page content includes the following: Organisation details uploaded
    And the page content includes the following: Test Producer

  @WireMockServer
  @AuthenticateDirectProducerNotStarted
  @RegistrationUploadWarnings
  Scenario: Direct Producer - File uploaded with warnings
    Given I have navigated to the Registration Guidance Page
    When I continue to the Registration Task List
    And I start the file upload step
    And I upload a valid CSV file
    Then I am on the Company Details Warnings Page
    And the page content includes the following: check the warnings
    And the page content includes the following: Test Producer

  @WireMockServer
  @AuthenticateDirectProducerNotStarted
  @RegistrationUploadErrors
  Scenario: Direct Producer - File fails validation
    Given I have navigated to the Registration Guidance Page
    When I continue to the Registration Task List
    And I start the file upload step
    And I upload a valid CSV file
    Then I am on the Company Details Errors Page
    And the page content includes the following: Organisation details not uploaded
    And the page content includes the following: Test Producer

  @WireMockServer
  @AuthenticateDirectProducerNotStarted
  @RegistrationUploadClosedLoopError
  Scenario: Direct Producer - File upload rejected due to closed loop registration for year before threshold
    Given I have navigated to the Registration Guidance Page
    When I continue to the Registration Task List
    And I start the file upload step
    And I upload a valid CSV file
    Then I am on the Company Details Upload Page
    And the page content includes the following: Closed loop registration cannot be submitted through this service for registration years before 2027
    And the page content includes the following: Test Producer

  # --- Compliance Scheme scenarios ---
  # The CS scenarios mirror the Direct Producer flows above and verify that the same
  # upload journeys work correctly for compliance scheme users. The signed-in navbar
  # name differs by user type on these pages ("Test User" for CS vs "Test Producer"
  # for direct producer), which differentiates these scenarios.

  @WireMockServer
  @AuthenticateComplianceSchemeNotStarted
  @RegistrationUploadSuccess
  Scenario: Compliance Scheme - Successful file upload and validation
    Given I have navigated to the Registration Guidance Page
    When I continue to the Registration Task List
    And I start the file upload step
    And I upload a valid CSV file
    Then I am on the Company Details Success Page
    And the page content includes the following: Organisation details uploaded
    And the page content includes the following: Test User

  @WireMockServer
  @AuthenticateComplianceSchemeNotStarted
  @RegistrationUploadWarnings
  Scenario: Compliance Scheme - File uploaded with warnings
    Given I have navigated to the Registration Guidance Page
    When I continue to the Registration Task List
    And I start the file upload step
    And I upload a valid CSV file
    Then I am on the Company Details Warnings Page
    And the page content includes the following: check the warnings
    And the page content includes the following: Test User

  @WireMockServer
  @AuthenticateComplianceSchemeNotStarted
  @RegistrationUploadErrors
  Scenario: Compliance Scheme - File fails validation
    Given I have navigated to the Registration Guidance Page
    When I continue to the Registration Task List
    And I start the file upload step
    And I upload a valid CSV file
    Then I am on the Company Details Errors Page
    And the page content includes the following: Organisation details not uploaded
    And the page content includes the following: Test User

  @WireMockServer
  @AuthenticateComplianceSchemeNotStarted
  @RegistrationUploadClosedLoopError
  Scenario: Compliance Scheme - File upload rejected due to closed loop registration for year before threshold
    Given I have navigated to the Registration Guidance Page
    When I continue to the Registration Task List
    And I start the file upload step
    And I upload a valid CSV file
    Then I am on the Company Details Upload Page
    And the page content includes the following: Closed loop registration cannot be submitted through this service for registration years before 2027
    And the page content includes the following: Test User

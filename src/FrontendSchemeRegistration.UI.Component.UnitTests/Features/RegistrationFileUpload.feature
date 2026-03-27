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

  # --- Compliance Scheme scenarios ---

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

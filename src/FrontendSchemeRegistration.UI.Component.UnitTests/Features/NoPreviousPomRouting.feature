Feature: No previous POM routing
As a user starting packaging data submission for a period
I want routing to depend on whether an uploaded file already exists
So that I can continue from the correct step

  @WireMockServer
  Scenario: No previous POM submission with no upload routes to upload page
    Given I am logged in with email pom.noprevious.noupload@test.com
    And I have navigated to the File Upload Sub Landing Page
    When I select a submission period and start the POM file upload
    Then I am on the POM File Upload Page

  @WireMockServer
  Scenario: No previous POM submission with existing uploaded file routes to check and submit and shows file uploaded tile
    Given I am logged in with email pom.noprevious.uploaded@test.com
    And I have navigated to the File Upload Sub Landing Page
    Then the page content includes the following: File uploaded
    When I select a submission period and start the POM file upload
    Then I am on the POM File Upload Success Page

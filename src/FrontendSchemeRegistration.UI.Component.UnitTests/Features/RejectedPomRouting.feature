Feature: Rejected POM routing
As a user resubmitting rejected packaging data
I want the period selection to route based on whether a new upload exists
So that I can either upload a file or submit an already uploaded one

  @WireMockServer
  Scenario: Rejected POM submission with no newer upload routes to upload page
    Given I am logged in with email pom.rejected.noupload@test.com
    And I have navigated to the File Upload Sub Landing Page
    When I select a submission period and start the POM file upload
    Then I am on the POM File Upload Page

  @WireMockServer
  Scenario: Rejected POM submission with newer upload routes to check and submit page
    Given I am logged in with email pom.rejected.uploaded@test.com
    And I have navigated to the File Upload Sub Landing Page
    When I select a submission period and start the POM file upload
    Then I am on the POM File Upload Success Page

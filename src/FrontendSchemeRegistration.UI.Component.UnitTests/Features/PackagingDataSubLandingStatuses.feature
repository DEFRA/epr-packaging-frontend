Feature: Packaging Data Sub Landing Statuses
As an authenticated user
I want to see the correct packaging data status on the sub landing page
So that I can understand my current submission status at a glance

  @WireMockServer
  Scenario: Accepted - packaging data accepted by the regulator
    Given I am logged in with email pom.accepted@test.com
    When I navigate to the File Upload Sub Landing Page
    Then the page is successfully returned
    And the page content includes the following: Accepted by Regulator
    And the page content includes the following: You have accepted files.

  @WireMockServer
  Scenario: Rejected - packaging data rejected by the regulator
    Given I am logged in with email pom.rejected@test.com
    When I navigate to the File Upload Sub Landing Page
    Then the page is successfully returned
    And the page content includes the following: Rejected by Regulator
    And the page content includes the following: Check if you need to resubmit

  @WireMockServer
  Scenario: View resubmission fees - fees not yet viewed
    Given I am logged in with email pom.resubfees.pending@test.com
    When I navigate to the File Upload Sub Landing Page
    Then the page is successfully returned
    And the page content includes the following: In Progress
    And the page content includes the following: You need to view your updated fee and select a payment method.

  @WireMockServer
  Scenario: View resubmission fees - fees already viewed, awaiting submission
    Given I am logged in with email pom.resubfees.viewed@test.com
    When I navigate to the File Upload Sub Landing Page
    Then the page is successfully returned
    And the page content includes the following: In Progress
    And the page content includes the following: You now need to submit to the regulator

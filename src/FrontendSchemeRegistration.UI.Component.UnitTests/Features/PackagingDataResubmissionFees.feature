Feature: Packaging Data Resubmission Fees
As an authenticated user
I want to view my packaging data resubmission fee breakdown
So that I know whether a payment is required before I submit

  @WireMockServer
  Scenario: Resubmission fee is applicable
    Given I am logged in with email pom.resubfees.pending@test.com
    When I complete the resubmission fee journey
    Then the page is successfully returned
    And the page content includes the following: Packaging data resubmission fee due
    And the page content includes the following: £2,500.00

  @WireMockServer
  Scenario: Resubmission fee is zero
    Given I am logged in with email pom.resubfees.viewed@test.com
    When I complete the resubmission fee journey
    Then the page is successfully returned
    And the page content includes the following: Packaging data resubmission fee paid

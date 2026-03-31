Feature: Packaging Data Resubmission Fees
As an authenticated user
I want to view my packaging data resubmission fee breakdown
So that I know whether a payment is required before I submit

  @WireMockServer
  Scenario: Resubmission fee is applicable
    Given I am logged in with email pom.resubfees.pending@test.com
    When I complete the resubmission fee journey
    Then I am on the Resubmission Fee Calculations Page
    And the page content includes the following: Packaging data resubmission fee due
    And the page content includes the following: £2,500.00

  @WireMockServer
  Scenario: Resubmission fee is zero - fee already settled
    Given I am logged in with email pom.resubfees.viewed@test.com
    When I complete the resubmission fee journey
    Then I am on the Resubmission Fee Calculations Page
    And the page content includes the following: Packaging data resubmission fee paid
    And the page content includes the following: £0.00

  @WireMockServer
  Scenario: Resubmission fee is zero - compliance scheme with no members incurring a fee
    Given I am logged in with email pom.resubfees.nomembers@test.com
    When I complete the resubmission fee journey
    Then I am on the Resubmission Fee Calculations Page
    And the page content includes the following: You have no additional fee to pay

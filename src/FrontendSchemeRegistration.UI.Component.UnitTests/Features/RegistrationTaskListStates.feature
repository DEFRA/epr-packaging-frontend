Feature: Registration Task List States
As an authenticated compliance scheme user
I want to see different task list states based on my registration progress
So that I know what actions I need to take next

@WireMockServer
Scenario: Not started - task list shows all tasks locked
    Given I am logged in with email notstarted@test.com
    When I navigate to the Registration Task List Page
    Then the page is successfully returned
    And the page content includes the following: NOT STARTED
    And the page content includes the following: CANNOT START YET
    And the page content includes the following: Submit registration data

@WireMockServer
Scenario: Uploaded - task list shows file upload completed and payment not started
    Given I am logged in with email uploaded@test.com
    When I navigate to the Registration Task List Page
    Then the page is successfully returned
    And the page content includes the following: COMPLETED
    And the page content includes the following: You have submitted your registration details
    And the page content includes the following: View registration fee

@WireMockServer
Scenario: Fees - task list shows file upload completed with fees ready to view
    Given I am logged in with email fees@test.com
    When I navigate to the Registration Task List Page
    Then the page is successfully returned
    And the page content includes the following: COMPLETED
    And the page content includes the following: You have submitted your registration details
    And the page content includes the following: View registration fee

@WireMockServer
Scenario: Paid - task list shows payment completed and submit not started
    Given I am logged in with email paid@test.com
    When I navigate to the Registration Task List Page
    Then the page is successfully returned
    And the page content includes the following: COMPLETED
    And the page content includes the following: You have viewed the registration fee and selected a payment method
    And the page content includes the following: Submit registration application

@WireMockServer
Scenario: Completed - task list shows all tasks completed with submission confirmation
    Given I am logged in with email completed@test.com
    When I navigate to the Registration Task List Page
    Then the page is successfully returned
    And the page content includes the following: COMPLETED
    And the page content includes the following: Your application has been submitted

@WireMockServer
Scenario: Accepted - task list shows all tasks completed after regulator acceptance
    Given I am logged in with email accepted@test.com
    When I navigate to the Registration Task List Page
    Then the page is successfully returned
    And the page content includes the following: COMPLETED

@WireMockServer
Scenario: Rejected - task list resets file upload after regulator rejection
    Given I am logged in with email rejected@test.com
    When I navigate to the Registration Task List Page
    Then the page is successfully returned
    And the page content includes the following: NOT STARTED
    And the page content includes the following: CANNOT START YET
    And the page content includes the following: Submit registration data

@WireMockServer
Scenario: Queried - task list resets file upload after regulator query
    Given I am logged in with email queried@test.com
    When I navigate to the Registration Task List Page
    Then the page is successfully returned
    And the page content includes the following: NOT STARTED
    And the page content includes the following: CANNOT START YET
    And the page content includes the following: Submit registration data

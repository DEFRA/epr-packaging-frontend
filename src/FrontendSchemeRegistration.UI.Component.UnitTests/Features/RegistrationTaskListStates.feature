Feature: Registration Task List States
As an authenticated compliance scheme user
I want to see different task list states based on my registration progress
So that I know what actions I need to take next

@WireMockServer
Scenario: Not started - task list shows all tasks locked
    Given I am logged in with email notstarted@test.com
    When I navigate to the Registration Task List Page
    Then I am on the Registration Task List Page
    And the page content includes the following: NOT STARTED
    And the page content includes the following: CANNOT START YET
    And the page content includes the following: Submit registration data

@WireMockServer
Scenario: Uploaded - task list shows file upload submitted, awaiting fee calculation
    Given I am logged in with email uploaded@test.com
    When I navigate to the Registration Task List Page
    Then I am on the Registration Task List Page
    And the page content includes the following: Submit registration data
    And the page content includes the following: PENDING
    And the page content includes the following: View registration fee
    And the page content includes the following: CANNOT START YET

@WireMockServer
Scenario: Fees - task list shows file upload completed with registration fee ready to view
    Given I am logged in with email fees@test.com
    When I navigate to the Registration Task List Page
    Then I am on the Registration Task List Page
    And the page content includes the following: COMPLETED
    And the page content includes the following: You have submitted your registration details
    And the page content includes the following: View registration fee
    And the page content includes the following: NOT STARTED

@WireMockServer
Scenario: Paid - task list shows payment completed and submit not started
    Given I am logged in with email paid@test.com
    When I navigate to the Registration Task List Page
    Then I am on the Registration Task List Page
    And the page content includes the following: COMPLETED
    And the page content includes the following: You have viewed the registration fee and selected a payment method
    And the page content includes the following: Submit registration application

@WireMockServer
# Verifies that once the application is submitted (all tasks completed), the task list
# shows all three tasks as COMPLETED with the submission confirmation message.
# "Your application has been submitted" distinguishes this from the Paid state (which
# still shows NOT STARTED for the submit task).
Scenario: Completed - task list shows all tasks completed with submission confirmation
    Given I am logged in with email completed@test.com
    When I navigate to the Registration Task List Page
    Then I am on the Registration Task List Page
    And the page content includes the following: COMPLETED
    And the page content includes the following: Your application has been submitted
    And the page content includes the following: You have submitted your registration details

@WireMockServer
# Verifies that after regulator acceptance, the task list continues to show all tasks
# as COMPLETED. The task list renders identically to the Completed state; the unique
# regulator-acceptance content is on the CSO registration tile (RegistrationTileStates.feature).
Scenario: Accepted - task list shows all tasks completed after regulator acceptance
    Given I am logged in with email accepted@test.com
    When I navigate to the Registration Task List Page
    Then I am on the Registration Task List Page
    And the page content includes the following: COMPLETED
    And the page content includes the following: Your application has been submitted
    And the page content includes the following: You have submitted your registration details

@WireMockServer
# Verifies that after regulator rejection, the task list correctly resets the file
# upload step back to NOT STARTED, allowing the user to resubmit.
Scenario: Rejected - task list resets file upload after regulator rejection
    Given I am logged in with email rejected@test.com
    When I navigate to the Registration Task List Page
    Then I am on the Registration Task List Page
    And the page content includes the following: NOT STARTED
    And the page content includes the following: CANNOT START YET
    And the page content includes the following: Submit registration data

@WireMockServer
# Verifies that after a regulator query, the task list correctly resets the file
# upload step back to NOT STARTED, allowing the user to resubmit a corrected file.
Scenario: Queried - task list resets file upload after regulator query
    Given I am logged in with email queried@test.com
    When I navigate to the Registration Task List Page
    Then I am on the Registration Task List Page
    And the page content includes the following: NOT STARTED
    And the page content includes the following: CANNOT START YET
    And the page content includes the following: Submit registration data

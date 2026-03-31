Feature: Registration Application
As an authenticated user
I want to access the registration application pages
So that I can complete my producer registration

@AuthenticateComplianceSchemeUser
@WireMockServer
Scenario: Navigate to registration guidance page redirects to task list when guidance already completed
    When I navigate to the Registration Guidance Page
    Then I am redirected to the: Registration Task List Page
    And the page redirect content includes the following: Submit registration data
    And the page redirect content includes the following: View registration fee
    And the page redirect content includes the following: Submit registration application

@AuthenticateComplianceSchemeUser
@WireMockServer
Scenario: Navigate to registration task list page shows task list
    When I navigate to the Registration Task List Page
    Then I am on the Registration Task List Page
    And the page content includes the following: Submit registration data
    And the page content includes the following: View registration fee
    And the page content includes the following: Submit registration application

@WireMockServer
Scenario: Navigate to registration guidance page as unauthenticated user redirected to login
    When I navigate to the Registration Guidance Page
    Then I am redirected to the: Login Page

Feature: Static Pages
As a user
I want to access static information pages
So that I can view privacy and cookie policies

@AuthenticateComplianceSchemeUser
@WireMockServer
Scenario: Navigate to privacy page shows privacy notice
    When I navigate to the Privacy Page
    Then the page is successfully returned
    And the page content includes the following: Privacy notice

@AuthenticateComplianceSchemeUser
@WireMockServer
Scenario: Navigate to cookies page shows cookies information
    When I navigate to the Cookies Page
    Then the page is successfully returned
    And the page content includes the following: Cookies

Feature: Static Pages
As a user
I want to access static information pages
So that I can view privacy and cookie policies

@AuthenticateComplianceSchemeUser
@WireMockServer
Scenario: Navigate to privacy page shows privacy notice
    When I navigate to the Privacy Page
    Then I am on the Privacy Page
    And the page content includes the following: Privacy notice - 'Report packaging data' service

@AuthenticateComplianceSchemeUser
@WireMockServer
Scenario: Navigate to cookies page shows cookies information
    When I navigate to the Cookies Page
    Then I am on the Cookies Page
    And the page content includes the following: Cookies are small files saved on your phone, tablet or computer when you visit a website.

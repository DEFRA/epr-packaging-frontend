Feature: File Upload Pages
As an authenticated user
I want to access the file upload pages
So that I can upload packaging data and organisation details

@AuthenticateComplianceSchemeUser
@WireMockServer
Scenario: Navigate to file upload sub landing page shows packaging data page
    When I navigate to the File Upload Sub Landing Page
    Then the page is successfully returned
    And the page content includes the following: Report packaging data

@AuthenticateComplianceSchemeUser
@WireMockServer
Scenario: Navigate to company details sub landing page shows organisation details page
    When I navigate to the Company Details Sub Landing Page
    Then the page is successfully returned
    And the page content includes the following: organisation details

@WireMockServer
Scenario: Navigate to file upload sub landing page as unauthenticated user redirected to login
    When I navigate to the File Upload Sub Landing Page
    Then I am redirected to the: Login Page

@WireMockServer
Scenario: Navigate to company details sub landing page as unauthenticated user redirected to login
    When I navigate to the Company Details Sub Landing Page
    Then I am redirected to the: Login Page

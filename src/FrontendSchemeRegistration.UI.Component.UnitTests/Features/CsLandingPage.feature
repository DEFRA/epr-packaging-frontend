Feature: Landing Page
As a user
I want to see the landing page        

@AuthenticateComplianceSchemeUser
@WireMockServer
Scenario: Navigate to home page as authenticated user shows landing page
    When I navigate to the Compliance Scheme Landing Page
    Then the page is successfully returned
    And the page content includes the following: Account home - COMPLIANCE SCHEME LTD

@AuthenticateComplianceSchemeUser
@WireMockServer
Scenario: Compliance scheme home page displays correct tiles with links
    When I navigate to the Compliance Scheme Landing Page
    Then the page is successfully returned
    And the page content includes the following: Register your members
    And the page content includes the following: Members' organisation details
    And the page content includes the following: Members' packaging data
    And the page content includes the following: Subsidiaries
    And the page content contains a link titled Register your members to /report-data/cso-registration
    And the page content contains a link titled Members' organisation details to /report-data/report-organisation-details
    And the page content contains a link titled Members' packaging data to /report-data/file-upload-sub-landing
    And the page content contains a link titled Subsidiaries to /report-data/subsidiaries-list
    
@AuthenticateComplianceSchemeUser
@WireMockServer
Scenario: Navigate to home page as authenticated user redirected to landing page
    When I navigate to the Report Data Page
    Then I am redirected to the: Compliance Scheme Landing Page
    And the page redirect content includes the following: Account home - COMPLIANCE SCHEME LTD
    
@WireMockServer
Scenario: Navigate to home page as unauthenticated user redirected to login page
    When I navigate to the Report Data Page
    Then I am redirected to the: Login Page
    
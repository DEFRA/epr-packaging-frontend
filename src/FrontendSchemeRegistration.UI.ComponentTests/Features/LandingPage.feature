Feature: Landing Page
As a user
I want to see the landing page        

@AuthenticateUser
@WireMockServer
Scenario: Navigate to home page as authenticated user shows landing page
    When I navigate to the Compliance Scheme Landing Page
    Then the page is successfully returned
    And the page content includes the following: Account home - SUPER TEST LTD
    
@AuthenticateUser
@WireMockServer
Scenario: Navigate to home page as authenticated user redirected to landing page
    When I navigate to the Report Data Page
    Then I am redirected to the: Compliance Scheme Landing Page
    And the page redirect content includes the following: Account home - SUPER TEST LTD
    
@WireMockServer
Scenario: Navigate to home page as unauthenticated user redirected to login page
    When I navigate to the Report Data Page
    Then I am redirected to the: Login Page
    
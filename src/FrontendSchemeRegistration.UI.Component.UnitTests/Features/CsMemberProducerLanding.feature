Feature: CS Member Producer Landing Page
As a direct producer managed by a compliance scheme
I want to be directed to the compliance scheme member landing page
So that I can manage my packaging data through my compliance scheme

@AuthenticateCsMemberProducerUser
@WireMockServer
Scenario: Navigate to report data as CS-managed producer redirects to manage compliance scheme page
    When I navigate to the Report Data Page
    Then I am redirected to the: Compliance Scheme Member Landing Page
    And the page redirect content includes the following: Account home - CS MEMBER PRODUCER LTD

@AuthenticateCsMemberProducerUser
@WireMockServer
Scenario: Navigate directly to manage compliance scheme page as CS-managed producer
    When I navigate to the Compliance Scheme Member Landing Page
    Then the page is successfully returned
    And the page content includes the following: Account home - CS MEMBER PRODUCER LTD

@WireMockServer
Scenario: Navigate to manage compliance scheme page as unauthenticated user redirected to login page
    When I navigate to the Compliance Scheme Member Landing Page
    Then I am redirected to the: Login Page

Feature: Direct Producer Landing Page
As a direct producer
I want to see the producer landing page
So that I can access my account home

@AuthenticateDirectProducerUser
@WireMockServer
Scenario: Navigate to home page as authenticated direct producer shows self-managed landing page
    When I navigate to the Report Data Page
    Then I am redirected to the: Producer Landing Page
    And the page redirect content includes the following: Account home - DIRECT PRODUCER LTD

@AuthenticateDirectProducerUser
@WireMockServer
Scenario: Navigate directly to producer landing page as authenticated direct producer
    When I navigate to the Producer Landing Page
    Then the page is successfully returned
    And the page content includes the following: Account home - DIRECT PRODUCER LTD

@AuthenticateDirectProducerUser
@WireMockServer
Scenario: Direct producer home page displays correct tiles with links
    When I navigate to the Producer Landing Page
    Then the page is successfully returned
    And the page content includes the following: Report your organisation details
    And the page content includes the following: Report packaging data
    And the page content includes the following: Subsidiaries
    And the page content includes the following: Add and manage your subsidiary companies
    And the page content contains a link titled Report your organisation details to /report-data/report-organisation-details
    And the page content contains a link titled Report packaging data to /report-data/file-upload-sub-landing
    And the page content contains a link titled Subsidiaries to /report-data/subsidiaries-list

@WireMockServer
Scenario: Navigate to producer landing page as unauthenticated user redirected to login page
    When I navigate to the Producer Landing Page
    Then I am redirected to the: Login Page

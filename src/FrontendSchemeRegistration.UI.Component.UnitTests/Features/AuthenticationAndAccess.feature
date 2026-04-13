Feature: Authentication and Access Control
As a security measure
Unauthenticated users should be redirected to the login page
When they attempt to access protected pages

@WireMockServer
Scenario: Unauthenticated user accessing report data page is redirected to login
    When I navigate to the Report Data Page
    Then I am redirected to the: Login Page

@WireMockServer
Scenario: Unauthenticated user accessing file upload sub landing is redirected to login
    When I navigate to the File Upload Sub Landing Page
    Then I am redirected to the: Login Page

@WireMockServer
Scenario: Unauthenticated user accessing company details sub landing is redirected to login
    When I navigate to the Company Details Sub Landing Page
    Then I am redirected to the: Login Page

@WireMockServer
Scenario: Unauthenticated user accessing registration guidance is redirected to login
    When I navigate to the Registration Guidance Page
    Then I am redirected to the: Login Page

@WireMockServer
Scenario: Unauthenticated user accessing registration task list is redirected to login
    When I navigate to the Registration Task List Page
    Then I am redirected to the: Login Page

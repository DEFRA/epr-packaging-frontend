Feature: Nation-specific Login
As an authenticated user from any UK nation
I want to log in and see my nation-specific content
So that I interact with the correct environmental regulator

# ── Compliance Scheme users ────────────────────────────────────────────

@WireMockServer
Scenario: Compliance scheme user in England sees Environment Agency
    Given I am logged in with email cs.england@test.com
    When I navigate to the Report Data Page
    Then I am redirected to the: Compliance Scheme Landing Page
    And the page redirect content includes the following: the Environment Agency

@WireMockServer
Scenario: Compliance scheme user in Scotland sees SEPA
    Given I am logged in with email cs.scotland@test.com
    When I navigate to the Report Data Page
    Then I am redirected to the: Compliance Scheme Landing Page
    And the page redirect content includes the following: the Scottish Environment Protection Agency

@WireMockServer
Scenario: Compliance scheme user in Wales sees NRW
    Given I am logged in with email cs.wales@test.com
    When I navigate to the Report Data Page
    Then I am redirected to the: Compliance Scheme Landing Page
    And the page redirect content includes the following: Natural Resources Wales

@WireMockServer
Scenario: Compliance scheme user in Northern Ireland sees NIEA
    Given I am logged in with email cs.northernireland@test.com
    When I navigate to the Report Data Page
    Then I am redirected to the: Compliance Scheme Landing Page
    And the page redirect content includes the following: the Northern Ireland Environment Agency

# ── Direct Producer users ──────────────────────────────────────────────
# For direct producers, the landing page does not display nation-specific regulator text
# so there are no nation-differentiated content assertions here. These scenarios are
# smoke tests that verify nation-keyword login does not break routing for direct producers.

@WireMockServer
Scenario Outline: Direct producer in <nation> logs in successfully and lands on producer page
    Given I am logged in with email producer.<nation>@test.com
    When I navigate to the Report Data Page
    Then I am redirected to the: Producer Landing Page

    Examples:
      | nation          |
      | england         |
      | scotland        |
      | wales           |
      | northernireland |

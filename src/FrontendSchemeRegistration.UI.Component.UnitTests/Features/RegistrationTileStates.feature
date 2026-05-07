Feature: Registration Tile States
As an authenticated compliance scheme user
I want to see the correct registration tile text on the CSO registration page
So that I can understand my current registration status at a glance

@WireMockServer
Scenario: Not started - registration tile shows deadline instructions
    Given I am logged in with email notstarted@test.com
    When I navigate to the CSO Registration Page
    Then the page is successfully returned
    And the page content includes the following: Large producer 2026 registration
    And the page content includes the following: Applications made after this date could incur a late fee

@WireMockServer
Scenario: Uploaded - registration tile shows data submitted, awaiting fee calculation
    Given I am logged in with email uploaded@test.com
    When I navigate to the CSO Registration Page
    Then the page is successfully returned
    And the page content includes the following: You have submitted your registration data
    And the page content includes the following: You will then need to pay your registration fee

@WireMockServer
Scenario: Fees - registration tile shows data submitted with fees ready to view
    Given I am logged in with email fees@test.com
    When I navigate to the CSO Registration Page
    Then the page is successfully returned
    And the page content includes the following: You have submitted your registration data for 2026
    And the page content includes the following: View your registration fees and payment methods

@WireMockServer
Scenario: Paid - registration tile shows fees viewed and payment required
    Given I am logged in with email paid@test.com
    When I navigate to the CSO Registration Page
    Then the page is successfully returned
    And the page content includes the following: You have viewed your registration fees
    And the page content includes the following: Payment must be made before submitting your 2026 application

@WireMockServer
Scenario: Completed - registration tile shows application submitted
    Given I am logged in with email completed@test.com
    When I navigate to the CSO Registration Page
    Then the page is successfully returned
    And the page content includes the following: Your registration application for 2026 has been submitted

@WireMockServer
Scenario: Accepted - registration tile shows registration granted by regulator
    Given I am logged in with email accepted@test.com
    When I navigate to the CSO Registration Page
    Then the page is successfully returned
    And the page content includes the following: Your registration for 2026 has been granted by the regulator
    And the page content includes the following: Resubmit your data files

@WireMockServer
Scenario: Rejected - registration tile shows application refused
    Given I am logged in with email rejected@test.com
    When I navigate to the CSO Registration Page
    Then the page is successfully returned
    And the page content includes the following: Your registration application for 2026 has been refused
    And the page content includes the following: Contact the regulator for further advice

@WireMockServer
Scenario: Queried - registration tile shows regulator query
    Given I am logged in with email queried@test.com
    When I navigate to the CSO Registration Page
    Then the page is successfully returned
    And the page content includes the following: The regulator has identified a potential issue
    And the page content includes the following: You will need to submit a corrected registration data file

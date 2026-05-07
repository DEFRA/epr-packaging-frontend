Feature: Registration application payment pages
  As a compliance scheme user whose mock registration state matches the page
  I want fee and payment registration routes to render
  So that payment and post-payment steps can be verified in component tests

  WireMock serves different registration-application JSON based on the email keyword in the stub bearer token
  (see MockServer WebApi EmailKeywords: "fees" → *Fees.json, "paid" → *Paid.json).
  Use compact addresses (fees@test.com, paid@test.com) so keywords match unambiguously.
  For Northern Ireland, combine the fees keyword with "northernireland" in the local part
  (e.g. fees.northernireland@test.com) so the mock serves *Fees.json and sets the compliance scheme nation to NI.

@WireMockServer
Scenario: Northern Ireland — select payment redirects to bank transfer only
  Given I am logged in with email fees.northernireland@test.com
  And I use registration journey CsoLargeProducer
  And I have completed company details upload for a compliance scheme
  And I browse to the following url following redirects: /report-data/registration-task-list?registrationjourney=CsoLargeProducer&registrationyear=2026
  When I browse to the following url following redirects: /report-data/select-payment-options?registrationyear=2026&registrationjourney=CsoLargeProducer
  Then the page is successfully returned
  And the page content includes the following: Pay by bank transfer
  And the page content does not include the following: Choose how to pay your registration fee
  And the page content does not include the following: Pay online through GOV.UK

@WireMockServer
Scenario Outline: Compliance scheme registration payment pages (fee outstanding)
  Given I am logged in with email fees@test.com
  And I use registration journey <registrationJourney>
  And I have completed company details upload for a compliance scheme
  And I browse to the following url following redirects: /report-data/registration-task-list?registrationjourney=<registrationJourney>&registrationyear=2026
  When I browse to the following url following redirects: /report-data/<pagePath>
  Then the page is successfully returned
  And the page content includes the following: <expectedContent>

  Examples:
    | registrationJourney | expectedContent                         | pagePath                                                                                 |
    | CsoLargeProducer    | Registration fee due                    | registration-fee-calculations?registrationyear=2026&registrationjourney=CsoLargeProducer |
    | CsoLargeProducer    | Choose how to pay your registration fee | select-payment-options?registrationyear=2026&registrationjourney=CsoLargeProducer          |
    | CsoLargeProducer    | Pay by bank transfer                    | pay-by-banktransfer?registrationyear=2026&registrationjourney=CsoLargeProducer            |
    | CsoLargeProducer    | Pay online through GOV.UK               | pay-online?registrationyear=2026&registrationjourney=CsoLargeProducer                     |
    | CsoLargeProducer    | Pay by phone                            | pay-by-phone?registrationyear=2026&registrationjourney=CsoLargeProducer                 |
    | CsoSmallProducer    | Registration fee due                    | registration-fee-calculations?registrationyear=2026&registrationjourney=CsoSmallProducer  |
    | CsoSmallProducer    | Choose how to pay your registration fee | select-payment-options?registrationyear=2026&registrationjourney=CsoSmallProducer          |
    | CsoSmallProducer    | Pay by bank transfer                    | pay-by-banktransfer?registrationyear=2026&registrationjourney=CsoSmallProducer             |
    | CsoSmallProducer    | Pay online through GOV.UK               | pay-online?registrationyear=2026&registrationjourney=CsoSmallProducer                    |
    | CsoSmallProducer    | Pay by phone                            | pay-by-phone?registrationyear=2026&registrationjourney=CsoSmallProducer                 |

@WireMockServer
Scenario Outline: Compliance scheme registration additional information (registration fee paid)
  Given I am logged in with email paid@test.com
  And I use registration journey <registrationJourney>
  And I have completed company details upload for a compliance scheme
  And I browse to the following url following redirects: /report-data/registration-task-list?registrationjourney=<registrationJourney>&registrationyear=2026
  When I browse to the following url following redirects: /report-data/<pagePath>
  Then the page is successfully returned
  And the page content includes the following: <expectedContent>

  Examples:
    | registrationJourney | expectedContent               | pagePath                                                                                 |
    | CsoLargeProducer    | Members' organisation details | additional-information?registrationyear=2026&registrationjourney=CsoLargeProducer         |
    | CsoSmallProducer    | Members' organisation details | additional-information?registrationyear=2026&registrationjourney=CsoSmallProducer         |

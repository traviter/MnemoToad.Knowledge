@ignore
Feature: Seed test data (reusable setup helper)

  Background:
    * url baseUrl

  Scenario:
    * def data = karate.get('data')
    Given path 'testdata', 'seed'
    And request data
    When method post
    Then status 204

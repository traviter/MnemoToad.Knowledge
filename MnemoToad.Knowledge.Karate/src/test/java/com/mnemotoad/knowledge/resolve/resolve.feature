@Regression @Resolve
Feature: Resolve Property Path DSL expressions against KnowledgeNodes

  Background:
    * url baseUrl
    * json graph = read('graph.json')
    * def seedResult = karate.callSingle('classpath:com/mnemotoad/knowledge/testdata/build-test-data.feature', { data: graph })
    * configure afterFeature =
      """
      function(){
        karate.call('classpath:com/mnemotoad/knowledge/testdata/cleanup-test-data.feature', { data: graph });
      }
      """

  Scenario Outline: <case>
    * def testCase = read('cases/<case>.json')
    Given path 'nodes', 'resolve'
    And request testCase.request
    When method post
    Then status 200
    And match response == testCase.response

    Examples:
      | case                     |
      | mixed-terminals          |
      | media-with-metadata      |
      | one-edge-relation        |
      | duplicate-node-id        |
      | missing-attribute-error  |
      | missing-relation-error   |
      | node-not-found           |

  Scenario Outline: <case>
    * def testCase = read('cases/<case>.json')
    Given path 'nodes', 'resolve'
    And request testCase.request
    When method post
    Then status 400

    Examples:
      | case                   |
      | empty-batch             |
      | entry-empty-paths       |
      | entry-missing-node-id   |
      | entry-missing-paths     |
      | invalid-path-syntax     |

  Scenario Outline: <case>
    * def testCase = read('cases/<case>.json')
    Given path 'nodes', 'resolve', 'type'
    And request testCase.request
    When method post
    Then status 200
    And match response == testCase.response

    Examples:
      | case                              |
      | resolve-by-type                   |
      | resolve-by-type-multi-hop         |
      | resolve-by-type-partial-results   |

  Scenario: Resolve by type with an unknown NodeType name returns 404
    * def testCase = read('cases/resolve-by-type-unknown-name.json')
    Given path 'nodes', 'resolve', 'type'
    And request testCase.request
    When method post
    Then status 404

  Scenario: Resolve by type with a missing NodeType name returns 400
    * def testCase = read('cases/resolve-by-type-missing-name.json')
    Given path 'nodes', 'resolve', 'type'
    And request testCase.request
    When method post
    Then status 400

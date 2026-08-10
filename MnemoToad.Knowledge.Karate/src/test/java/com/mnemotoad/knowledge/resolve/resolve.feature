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
      | one-hop-relation         |
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

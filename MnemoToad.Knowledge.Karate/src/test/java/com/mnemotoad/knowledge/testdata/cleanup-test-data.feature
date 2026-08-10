@ignore
Feature: Cleanup test data (reusable teardown helper)

  Background:
    * url baseUrl

  Scenario:
    * def data = karate.get('data')
    * def knowledgeRelationIds = get data.knowledgeRelations[*].id
    * def knowledgeNodeIds = get data.knowledgeNodes[*].id
    * def nodeTypeIds = get data.nodeTypes[*].id
    * def relationshipTypeIds = get data.relationshipTypes[*].id
    * def mediaAssetIds = get data.mediaAssets[*].id
    Given path 'testdata', 'cleanup'
    And request { knowledgeRelationIds: '#(knowledgeRelationIds)', knowledgeNodeIds: '#(knowledgeNodeIds)', nodeTypeIds: '#(nodeTypeIds)', relationshipTypeIds: '#(relationshipTypeIds)', mediaAssetIds: '#(mediaAssetIds)' }
    When method post
    Then status 204

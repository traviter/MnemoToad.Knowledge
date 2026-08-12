@Regression @RelationshipType
Feature: RelationshipType API

  Background:
    * url baseUrl
    * def uniqueName = read('classpath:com/mnemotoad/knowledge/common/util.js')
    * def uniqueAlphaName = read('classpath:com/mnemotoad/knowledge/common/uniqueAlphaName.js')
    * def relationshipTypeFixtures = call read('fixtures.js')
    * def nodeTypeFixtures = call read('classpath:com/mnemotoad/knowledge/nodetype/fixtures.js')
    * def knowledgeNodeFixtures = call read('classpath:com/mnemotoad/knowledge/knowledgenode/fixtures.js')
    * def knowledgeRelationFixtures = call read('classpath:com/mnemotoad/knowledge/knowledgerelation/fixtures.js')
    * def createRelationshipType = relationshipTypeFixtures.create
    * def createNodeType = nodeTypeFixtures.create
    * def createKnowledgeNode = knowledgeNodeFixtures.create
    * def createKnowledgeRelation = knowledgeRelationFixtures.create
    * configure afterScenario =
      """
      function(){
        // Clean up dependents before dependencies: the relation references the nodes and the
        // relationship type, and the nodes reference the node type.
        knowledgeRelationFixtures.cleanup();
        knowledgeNodeFixtures.cleanup();
        nodeTypeFixtures.cleanup();
        relationshipTypeFixtures.cleanup();
      }
      """

  Scenario: Create a relationship type successfully
    * def name = uniqueAlphaName('RelationshipType')
    * def inverseName = 'inverseOf' + name
    Given path 'relationshipTypes'
    And request { name: '#(name)', inverseName: '#(inverseName)', description: 'Created by Karate test' }
    When method post
    Then status 201
    And match response.name == name
    And match response.inverseName == inverseName
    And match response.description == 'Created by Karate test'
    And match response.id == '#uuid'
    * eval relationshipTypeFixtures.stageForCleanup(response.id)

  Scenario: Reject creation with missing name
    Given path 'relationshipTypes'
    And request { name: '', description: 'should fail' }
    When method post
    Then status 400

  Scenario: Reject creation with a duplicate name
    * def name = uniqueAlphaName('RelationshipType')
    * def created = createRelationshipType({ name: name })

    Given path 'relationshipTypes'
    And request { name: '#(name)' }
    When method post
    Then status 400

  Scenario: Reject creation with a non-alpha name
    Given path 'relationshipTypes'
    And request { name: 'hasCapital1', description: 'should fail' }
    When method post
    Then status 400

  Scenario: Reject creation with a non-alpha inverseName
    * def name = uniqueAlphaName('RelationshipType')
    Given path 'relationshipTypes'
    And request { name: '#(name)', inverseName: 'capital_of' }
    When method post
    Then status 400

  Scenario: Get a relationship type by id
    * def created = createRelationshipType()

    Given path 'relationshipTypes', created.response.id
    When method get
    Then status 200
    And match response.name == created.response.name

  Scenario: List relationship types includes the newly created one
    * def created = createRelationshipType()

    Given path 'relationshipTypes'
    When method get
    Then status 200
    * def found = karate.filter(response, function(x){ return x.name == created.response.name })
    And match found[0].name == created.response.name

  Scenario: Update a relationship type
    * def created = createRelationshipType()

    * def updatedName = created.response.name + 'Updated'
    * def updatedInverseName = 'inverseOf' + updatedName
    Given path 'relationshipTypes', created.response.id
    And request { name: '#(updatedName)', inverseName: '#(updatedInverseName)', description: 'Updated by test' }
    When method put
    Then status 200
    And match response.name == updatedName
    And match response.inverseName == updatedInverseName
    And match response.description == 'Updated by test'

  Scenario: Reject update with a duplicate name
    * def name1 = uniqueAlphaName('RelationshipType')
    * def name2 = uniqueAlphaName('RelationshipType')
    * def created1 = createRelationshipType({ name: name1 })
    * def created2 = createRelationshipType({ name: name2 })

    Given path 'relationshipTypes', created2.response.id
    And request { name: '#(name1)' }
    When method put
    Then status 400

  Scenario: Delete a relationship type
    * def created = createRelationshipType()

    Given path 'relationshipTypes', created.response.id
    When method delete
    Then status 204

    Given path 'relationshipTypes', created.response.id
    When method get
    Then status 404

  Scenario: Reject deleting a relationship type that is referenced by a relation
    * def relationshipType = createRelationshipType()
    * def nodeType = createNodeType()
    * def sourceNode = createKnowledgeNode({ nodeTypeId: nodeType.response.id })
    * def targetNode = createKnowledgeNode({ nodeTypeId: nodeType.response.id })
    * def created = createKnowledgeRelation({ sourceNodeId: sourceNode.response.id, relationshipTypeId: relationshipType.response.id, targetNodeId: targetNode.response.id })

    Given path 'relationshipTypes', relationshipType.response.id
    When method delete
    Then status 400

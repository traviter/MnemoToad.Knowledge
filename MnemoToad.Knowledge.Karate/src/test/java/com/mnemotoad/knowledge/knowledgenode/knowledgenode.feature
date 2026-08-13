@Regression @KnowledgeNode
Feature: KnowledgeNode API

  Background:
    * url baseUrl
    * def uniqueName = read('classpath:com/mnemotoad/knowledge/common/util.js')
    * def nodeTypeFixtures = call read('classpath:com/mnemotoad/knowledge/nodetype/fixtures.js')
    * def mediaAssetFixtures = call read('classpath:com/mnemotoad/knowledge/mediaasset/fixtures.js')
    * def knowledgeNodeFixtures = call read('fixtures.js')
    * def createNodeType = nodeTypeFixtures.create
    * def createMediaAsset = mediaAssetFixtures.create
    * def createKnowledgeNode = knowledgeNodeFixtures.create
    * configure afterScenario =
      """
      function(){
        // KnowledgeNodes must be deleted before their referenced NodeTypes -- the FK on
        // knowledge_node.node_type_id would otherwise reject the NodeType delete.
        knowledgeNodeFixtures.cleanup();
        nodeTypeFixtures.cleanup();
        mediaAssetFixtures.cleanup();
      }
      """

  Scenario: Create a knowledge node successfully
    * def nodeType = createNodeType()
    * def name = uniqueName('KnowledgeNode')
    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(name)', description: 'Created by Karate test' }
    When method post
    Then status 201
    And match response.nodeTypeId == nodeType.response.id
    And match response.canonicalName == name
    And match response.description == 'Created by Karate test'
    And match response.id == '#uuid'
    * eval knowledgeNodeFixtures.stageForCleanup(response.id)

  Scenario: Create a knowledge node with attributes
    * def nodeType = createNodeType()
    * def name = uniqueName('KnowledgeNode')
    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(name)', attributes: { isoCode: 'FR', population: 68000000, isEuMember: true } }
    When method post
    Then status 201
    And match response.attributes == { isoCode: 'FR', population: 68000000, isEuMember: true }
    * eval knowledgeNodeFixtures.stageForCleanup(response.id)

  Scenario: Reject creation with missing canonical name
    * def nodeType = createNodeType()
    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '' }
    When method post
    Then status 400

  Scenario: Reject creation with an invalid node type id
    Given path 'nodes'
    And request { nodeTypeId: 'not-a-guid', canonicalName: '#(uniqueName("KnowledgeNode"))' }
    When method post
    Then status 400

  Scenario: Reject creation with a missing node type id
    Given path 'nodes'
    And request { canonicalName: '#(uniqueName("KnowledgeNode"))' }
    When method post
    Then status 400

  Scenario: Reject creation referencing a node type that does not exist
    * def randomId = '' + java.util.UUID.randomUUID()
    Given path 'nodes'
    And request { nodeTypeId: '#(randomId)', canonicalName: '#(uniqueName("KnowledgeNode"))' }
    When method post
    Then status 400

  Scenario: Reject creation with an array-valued attribute
    * def nodeType = createNodeType()
    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(uniqueName("KnowledgeNode"))', attributes: { tags: ['a', 'b'] } }
    When method post
    Then status 400

  Scenario: Reject creation with an object-valued attribute
    * def nodeType = createNodeType()
    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(uniqueName("KnowledgeNode"))', attributes: { nested: { a: 1 } } }
    When method post
    Then status 400

  Scenario: Reject creation with a null-valued attribute
    * def nodeType = createNodeType()
    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(uniqueName("KnowledgeNode"))', attributes: { population: null } }
    When method post
    Then status 400

  Scenario: Reject creation with a non-alpha attribute key
    * def nodeType = createNodeType()
    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(uniqueName("KnowledgeNode"))', attributes: { 'iso-code': 'FR' } }
    When method post
    Then status 400

  Scenario: Reject creation with a duplicate name for the same node type
    * def nodeType = createNodeType()
    * def name = uniqueName('KnowledgeNode')
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id, canonicalName: name })

    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(name)' }
    When method post
    Then status 400

  Scenario: Allow the same canonical name under different node types
    * def nodeType1 = createNodeType()
    * def nodeType2 = createNodeType()
    * def name = uniqueName('KnowledgeNode')
    * def created1 = createKnowledgeNode({ nodeTypeId: nodeType1.response.id, canonicalName: name })

    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType2.response.id)', canonicalName: '#(name)' }
    When method post
    Then status 201
    * eval knowledgeNodeFixtures.stageForCleanup(response.id)

  Scenario: Get a knowledge node by id
    * def nodeType = createNodeType()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id })

    Given path 'nodes', created.response.id
    When method get
    Then status 200
    And match response.canonicalName == created.response.canonicalName

  Scenario: Get a knowledge node by id returns its attributes
    * def nodeType = createNodeType()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id, attributes: { isoCode: 'FR' } })

    Given path 'nodes', created.response.id
    When method get
    Then status 200
    And match response.attributes == { isoCode: 'FR' }

  Scenario: List knowledge nodes includes the newly created one
    * def nodeType = createNodeType()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id })

    Given path 'nodes'
    And param nodeTypeId = nodeType.response.id
    When method get
    Then status 200
    * def found = karate.filter(response, function(x){ return x.id == created.response.id })
    And match found[0].canonicalName == created.response.canonicalName

  Scenario: List knowledge nodes does not include attributes
    * def nodeType = createNodeType()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id, attributes: { isoCode: 'FR' } })

    Given path 'nodes'
    And param nodeTypeId = nodeType.response.id
    When method get
    Then status 200
    * def found = karate.filter(response, function(x){ return x.id == created.response.id })
    And match found[0].attributes == '#notpresent'

  Scenario: List knowledge nodes filtered by node type
    * def nodeType1 = createNodeType()
    * def nodeType2 = createNodeType()
    * def created1 = createKnowledgeNode({ nodeTypeId: nodeType1.response.id })
    * def created2 = createKnowledgeNode({ nodeTypeId: nodeType2.response.id })

    Given path 'nodes'
    And param nodeTypeId = nodeType1.response.id
    When method get
    Then status 200
    * def foundIds = karate.map(response, function(x){ return x.id })
    And match foundIds contains created1.response.id
    And match foundIds !contains created2.response.id

  Scenario: Reject listing knowledge nodes with a missing node type id
    Given path 'nodes'
    When method get
    Then status 400

  Scenario: Reject listing knowledge nodes with an invalid node type id
    Given path 'nodes'
    And param nodeTypeId = 'not-a-guid'
    When method get
    Then status 400

  Scenario: List knowledge nodes with an all-zero node type id returns an empty list
    Given path 'nodes'
    And param nodeTypeId = '00000000-0000-0000-0000-000000000000'
    When method get
    Then status 200
    And match response == []

  Scenario: Update a knowledge node
    * def nodeType = createNodeType()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id })

    * def updatedName = created.response.canonicalName + '_Updated'
    Given path 'nodes', created.response.id
    And request { nodeTypeId: '#(created.response.nodeTypeId)', canonicalName: '#(updatedName)', description: 'Updated by test' }
    When method put
    Then status 200
    And match response.canonicalName == updatedName
    And match response.description == 'Updated by test'

  Scenario: Update a knowledge node fully replaces its attributes
    * def nodeType = createNodeType()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id, attributes: { isoCode: 'FR', population: 68000000 } })

    Given path 'nodes', created.response.id
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(created.response.canonicalName)', attributes: { isoCode: 'FR', isEuMember: true } }
    When method put
    Then status 200
    And match response.attributes == { isoCode: 'FR', isEuMember: true }

  Scenario: Update a knowledge node with no attributes clears the existing ones
    * def nodeType = createNodeType()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id, attributes: { isoCode: 'FR' } })

    Given path 'nodes', created.response.id
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(created.response.canonicalName)' }
    When method put
    Then status 200
    And match response.attributes == {}

  Scenario: Delete a knowledge node that still has attributes cascades and succeeds
    * def nodeType = createNodeType()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id, attributes: { isoCode: 'FR' } })

    Given path 'nodes', created.response.id
    When method delete
    Then status 204

    Given path 'nodes', created.response.id
    When method get
    Then status 404

  Scenario: Reject update with a duplicate name for the same node type
    * def nodeType = createNodeType()
    * def name1 = uniqueName('KnowledgeNode')
    * def name2 = uniqueName('KnowledgeNode')
    * def created1 = createKnowledgeNode({ nodeTypeId: nodeType.response.id, canonicalName: name1 })
    * def created2 = createKnowledgeNode({ nodeTypeId: nodeType.response.id, canonicalName: name2 })

    Given path 'nodes', created2.response.id
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(name1)' }
    When method put
    Then status 400

  Scenario: Reject update referencing a node type that does not exist
    * def nodeType = createNodeType()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id })
    * def randomId = '' + java.util.UUID.randomUUID()

    Given path 'nodes', created.response.id
    And request { nodeTypeId: '#(randomId)', canonicalName: '#(created.response.canonicalName)' }
    When method put
    Then status 400

  Scenario: Delete a knowledge node
    * def nodeType = createNodeType()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id })

    Given path 'nodes', created.response.id
    When method delete
    Then status 204

    Given path 'nodes', created.response.id
    When method get
    Then status 404

  Scenario: Reject deleting a node type that is referenced by a knowledge node
    * def nodeType = createNodeType()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id })

    Given path 'nodeTypes', nodeType.response.id
    When method delete
    Then status 400

  Scenario: Create a knowledge node with media embeds the stored stanza
    * def nodeType = createNodeType()
    * def mediaAsset = createMediaAsset()
    * def name = uniqueName('KnowledgeNode')

    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(name)', media: { flag: { id: '#(mediaAsset.response.id)', alt_text: 'Flag of France' } } }
    When method post
    Then status 201
    And match response.media.flag.id == mediaAsset.response.id
    And match response.media.flag.alt_text == 'Flag of France'
    * eval knowledgeNodeFixtures.stageForCleanup(response.id)

  Scenario: Create a knowledge node with media preserves arbitrary extra fields
    * def nodeType = createNodeType()
    * def mediaAsset = createMediaAsset()
    * def name = uniqueName('KnowledgeNode')

    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(name)', media: { flag: { id: '#(mediaAsset.response.id)', alt_text: 'Flag of France', other_metadata: 2323 } } }
    When method post
    Then status 201
    And match response.media.flag.other_metadata == 2323
    * eval knowledgeNodeFixtures.stageForCleanup(response.id)

  Scenario: Get a knowledge node by id returns its media
    * def nodeType = createNodeType()
    * def mediaAsset = createMediaAsset()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id, media: { flag: { id: mediaAsset.response.id, alt_text: 'Flag of France' } } })

    Given path 'nodes', created.response.id
    When method get
    Then status 200
    And match response.media.flag.id == mediaAsset.response.id
    And match response.media.flag.alt_text == 'Flag of France'

  Scenario: Reject creation with a media entry missing id
    * def nodeType = createNodeType()

    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(uniqueName("KnowledgeNode"))', media: { flag: { alt_text: 'Flag of France' } } }
    When method post
    Then status 400

  Scenario: Reject creation with a media entry missing alt_text
    * def nodeType = createNodeType()
    * def mediaAsset = createMediaAsset()

    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(uniqueName("KnowledgeNode"))', media: { flag: { id: '#(mediaAsset.response.id)' } } }
    When method post
    Then status 400

  Scenario: Reject creation with a non-alpha media key
    * def nodeType = createNodeType()
    * def mediaAsset = createMediaAsset()

    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(uniqueName("KnowledgeNode"))', media: { flag2: { id: '#(mediaAsset.response.id)', alt_text: 'x' } } }
    When method post
    Then status 400

  Scenario: Reject creation reusing a media asset already linked to another node
    * def nodeType = createNodeType()
    * def mediaAsset = createMediaAsset()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id, media: { flag: { id: mediaAsset.response.id, alt_text: 'Flag of France' } } })

    Given path 'nodes'
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(uniqueName("KnowledgeNode"))', media: { flag: { id: '#(mediaAsset.response.id)', alt_text: 'Reused' } } }
    When method post
    Then status 400

  Scenario: Update a knowledge node fully replaces its media
    * def nodeType = createNodeType()
    * def flagAsset = createMediaAsset()
    * def photoAsset = createMediaAsset()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id, media: { flag: { id: flagAsset.response.id, alt_text: 'Flag of France' } } })

    Given path 'nodes', created.response.id
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(created.response.canonicalName)', media: { photo: { id: '#(photoAsset.response.id)', alt_text: 'Eiffel Tower' } } }
    When method put
    Then status 200
    And match response.media == { photo: { id: '#(photoAsset.response.id)', alt_text: 'Eiffel Tower' } }

  Scenario: Update a knowledge node with no media clears the existing media
    * def nodeType = createNodeType()
    * def mediaAsset = createMediaAsset()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id, media: { flag: { id: mediaAsset.response.id, alt_text: 'Flag of France' } } })

    Given path 'nodes', created.response.id
    And request { nodeTypeId: '#(nodeType.response.id)', canonicalName: '#(created.response.canonicalName)' }
    When method put
    Then status 200
    And match response.media == {}

  Scenario: Delete a knowledge node that still has media cascades and succeeds
    * def nodeType = createNodeType()
    * def mediaAsset = createMediaAsset()
    * def created = createKnowledgeNode({ nodeTypeId: nodeType.response.id, media: { flag: { id: mediaAsset.response.id, alt_text: 'Flag of France' } } })

    Given path 'nodes', created.response.id
    When method delete
    Then status 204

    Given path 'nodes', created.response.id
    When method get
    Then status 404

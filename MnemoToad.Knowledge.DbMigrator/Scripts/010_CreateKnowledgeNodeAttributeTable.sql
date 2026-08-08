CREATE TABLE knowledge_node_attribute (
    knowledge_node_id UUID NOT NULL,
    key VARCHAR(100) NOT NULL,
    value JSONB NOT NULL,
    CONSTRAINT pk_knowledge_node_attribute PRIMARY KEY (knowledge_node_id, key),
    CONSTRAINT fk_knowledge_node_attribute_knowledge_node_id FOREIGN KEY (knowledge_node_id) REFERENCES knowledge_node(id) ON DELETE CASCADE
);

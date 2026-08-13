ALTER TABLE relationship_type
    ADD CONSTRAINT ck_relationship_type_name CHECK (name ~ '^[A-Za-z]+$');

ALTER TABLE knowledge_node_attribute
    ADD CONSTRAINT ck_knowledge_node_attribute_key CHECK (key ~ '^[A-Za-z]+$');

ALTER TABLE knowledge_node_media
    ADD CONSTRAINT ck_knowledge_node_media_key CHECK (key ~ '^[A-Za-z]+$');

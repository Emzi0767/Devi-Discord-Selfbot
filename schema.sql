CREATE SEQUENCE devi_message_log_id_seq;
CREATE TABLE devi_message_log(
    id BIGINT NOT NULL DEFAULT NEXTVAL('devi_message_log_id_seq'),
    message_id BIGINT NOT NULL,  -- snowflake
    author_id BIGINT NOT NULL,   -- snowflake
    channel_id BIGINT NOT NULL,  -- snowflake
    created TIMESTAMP WITH TIME ZONE NOT NULL,
    edits TIMESTAMP WITH TIME ZONE[] NOT NULL,
    contents TEXT[],
    embeds JSONB[],
    attachment_urls TEXT[],
    deleted BOOL NOT NULL,
    edited BOOL NOT NULL,
    UNIQUE(message_id, channel_id)
);
ALTER SEQUENCE devi_message_log_id_seq OWNED BY devi_message_log.id;

CREATE SEQUENCE devi_reaction_log_id_seq;
CREATE TABLE devi_reaction_log(
    id BIGINT NOT NULL DEFAULT NEXTVAL('devi_reaction_log_id_seq'),
    message_id BIGINT NOT NULL,  -- snowflake
    channel_id BIGINT NOT NULL,  -- snowflake
    user_id BIGINT NOT NULL,     -- snowflake
    reaction TEXT NOT NULL,
    action BOOL NOT NULL,        -- true = add, false = remove
    action_timestamp TIMESTAMP WITH TIME ZONE NOT NULL
);
ALTER SEQUENCE devi_reaction_log_id_seq OWNED BY devi_reaction_log.id;
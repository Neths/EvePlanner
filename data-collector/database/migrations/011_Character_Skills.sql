-- Character Skills
-- Stores character skill levels and training information

CREATE TABLE IF NOT EXISTS character_skills (
    character_id BIGINT NOT NULL REFERENCES characters(character_id) ON DELETE CASCADE,
    skill_id INTEGER NOT NULL,
    active_skill_level INTEGER NOT NULL CHECK (active_skill_level BETWEEN 0 AND 5),
    trained_skill_level INTEGER NOT NULL CHECK (trained_skill_level BETWEEN 0 AND 5),
    skillpoints_in_skill BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    PRIMARY KEY (character_id, skill_id)
);

-- Character skill queue
CREATE TABLE IF NOT EXISTS character_skill_queue (
    id SERIAL PRIMARY KEY,
    character_id BIGINT NOT NULL REFERENCES characters(character_id) ON DELETE CASCADE,
    skill_id INTEGER NOT NULL,
    queue_position INTEGER NOT NULL,
    finished_level INTEGER NOT NULL CHECK (finished_level BETWEEN 1 AND 5),
    start_date TIMESTAMP WITH TIME ZONE,
    finish_date TIMESTAMP WITH TIME ZONE,
    training_start_sp BIGINT,
    level_start_sp BIGINT,
    level_end_sp BIGINT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE(character_id, queue_position)
);

-- Indexes
CREATE INDEX idx_character_skills_character ON character_skills(character_id);
CREATE INDEX idx_character_skill_queue_character ON character_skill_queue(character_id);
CREATE INDEX idx_character_skill_queue_finish ON character_skill_queue(finish_date) WHERE finish_date IS NOT NULL;

-- Comments
COMMENT ON TABLE character_skills IS 'Character trained skills from ESI';
COMMENT ON TABLE character_skill_queue IS 'Character skill training queue';
COMMENT ON COLUMN character_skills.active_skill_level IS 'Currently active skill level';
COMMENT ON COLUMN character_skills.trained_skill_level IS 'Highest trained level';

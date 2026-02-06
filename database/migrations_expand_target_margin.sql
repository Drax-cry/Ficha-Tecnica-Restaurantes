-- Expands the recipes.target_margin column so very high markup percentages
-- (above 500%) can be stored without causing an out-of-range error.
ALTER TABLE recipes
    MODIFY target_margin DECIMAL(18,4) NOT NULL DEFAULT 0.5500;

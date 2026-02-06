-- SQL script to create the application's MySQL database and login table
CREATE DATABASE IF NOT EXISTS ficha_tecnica_restaurantes CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE ficha_tecnica_restaurantes;

CREATE TABLE IF NOT EXISTS login (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    username VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    salt VARBINARY(128) NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_login_username (username),
    UNIQUE KEY ux_login_email (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS categories (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    user_id INT UNSIGNED NOT NULL,
    name VARCHAR(150) NOT NULL,
    icon_key VARCHAR(50) NOT NULL DEFAULT 'category',
    description TEXT NULL,
    color VARCHAR(20) NULL,
    display_order INT UNSIGNED NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_categories_user_name (user_id, name),
    KEY ix_categories_user_id (user_id),
    CONSTRAINT fk_categories_user FOREIGN KEY (user_id)
        REFERENCES login (id) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS recipe_categories (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    user_id INT UNSIGNED NOT NULL,
    name VARCHAR(150) NOT NULL,
    description TEXT NULL,
    icon_key VARCHAR(50) NOT NULL DEFAULT 'chef-hat',
    color VARCHAR(20) NULL,
    display_order INT UNSIGNED NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_recipe_categories_user_name (user_id, name),
    KEY ix_recipe_categories_user_id (user_id),
    CONSTRAINT fk_recipe_categories_user FOREIGN KEY (user_id)
        REFERENCES login (id) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ingredients (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    user_id INT UNSIGNED NOT NULL,
    name VARCHAR(150) NOT NULL,
    description TEXT NULL,
    category_id INT UNSIGNED NULL,
    unit VARCHAR(50) NOT NULL,
    cost_per_unit DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    currency CHAR(3) NOT NULL DEFAULT 'EUR',
    reorder_level DECIMAL(10,2) NULL,
    supplier VARCHAR(150) NULL,
    package_quantity DECIMAL(10,2) NULL,
    package_size VARCHAR(100) NULL,
    total_cost DECIMAL(10,2) NULL,
    icon_key VARCHAR(50) NULL,
    last_price_update DATETIME NULL,
    notes TEXT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_ingredients_user_name (user_id, name),
    KEY ix_ingredients_user_id (user_id),
    KEY ix_ingredients_category_id (category_id),
    CONSTRAINT fk_ingredients_user FOREIGN KEY (user_id)
        REFERENCES login (id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_ingredients_category FOREIGN KEY (category_id)
        REFERENCES categories (id) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ingredient_price_movements (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    user_id INT UNSIGNED NOT NULL,
    ingredient_id INT UNSIGNED NOT NULL,
    previous_price DECIMAL(18,4) NOT NULL,
    new_price DECIMAL(18,4) NOT NULL,
    change_amount DECIMAL(18,4) NOT NULL,
    change_percentage DECIMAL(9,4) NULL,
    effective_date DATETIME NOT NULL,
    recorded_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    notes TEXT NULL,
    PRIMARY KEY (id),
    KEY ix_price_movements_user_id (user_id),
    KEY ix_price_movements_ingredient_id (ingredient_id),
    CONSTRAINT fk_price_movements_user FOREIGN KEY (user_id)
        REFERENCES login (id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_price_movements_ingredient FOREIGN KEY (ingredient_id)
        REFERENCES ingredients (id) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS recipes (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    user_id INT UNSIGNED NOT NULL,
    category_id INT UNSIGNED NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT NULL,
    chef_notes TEXT NULL,
    preparation_time VARCHAR(80) NULL,
    yield VARCHAR(80) NULL,
    target_margin DECIMAL(18,4) NOT NULL DEFAULT 0.5500,
    ingredient_cost DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    suggested_price DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    image_path VARCHAR(255) NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_recipes_user_name (user_id, name),
    KEY ix_recipes_user_id (user_id),
    KEY ix_recipes_category_id (category_id),
    CONSTRAINT fk_recipes_user FOREIGN KEY (user_id)
        REFERENCES login (id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_recipes_category FOREIGN KEY (category_id)
        REFERENCES recipe_categories (id) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS recipe_ingredients (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    recipe_id INT UNSIGNED NOT NULL,
    ingredient_id INT UNSIGNED NOT NULL,
    ingredient_name VARCHAR(200) NOT NULL,
    quantity DECIMAL(10,4) NOT NULL DEFAULT 0.0000,
    unit VARCHAR(50) NOT NULL,
    cost_per_unit DECIMAL(10,4) NOT NULL DEFAULT 0.0000,
    total_cost DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_recipe_ingredients_recipe_id (recipe_id),
    KEY ix_recipe_ingredients_ingredient_id (ingredient_id),
    CONSTRAINT fk_recipe_ingredients_recipe FOREIGN KEY (recipe_id)
        REFERENCES recipes (id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_recipe_ingredients_ingredient FOREIGN KEY (ingredient_id)
        REFERENCES ingredients (id) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS suppliers (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    user_id INT UNSIGNED NOT NULL,
    name VARCHAR(150) NOT NULL,
    contact_name VARCHAR(150) NULL,
    email VARCHAR(255) NULL,
    phone VARCHAR(50) NULL,
    whatsapp VARCHAR(50) NULL,
    website VARCHAR(255) NULL,
    tax_id VARCHAR(50) NULL,
    payment_terms VARCHAR(100) NULL,
    address_line1 VARCHAR(255) NULL,
    address_line2 VARCHAR(255) NULL,
    city VARCHAR(150) NULL,
    state VARCHAR(150) NULL,
    postal_code VARCHAR(20) NULL,
    country VARCHAR(100) NULL,
    notes TEXT NULL,
    is_preferred TINYINT(1) NOT NULL DEFAULT 0,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY ux_suppliers_user_name (user_id, name),
    KEY ix_suppliers_user_id (user_id),
    KEY ix_suppliers_is_active (is_active),
    CONSTRAINT fk_suppliers_user FOREIGN KEY (user_id)
        REFERENCES login (id) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

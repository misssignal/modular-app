-- CRATE Workbench Permission Schema
-- Reference implementation for the database-backed permission system.
-- Currently stubbed with in-memory store in PermissionService.cs.

-- Core identity
CREATE TABLE users (
    user_id       TEXT PRIMARY KEY,       -- AD/LDAP username (GMID)
    display_name  TEXT NOT NULL,
    email         TEXT,
    is_active     BOOLEAN DEFAULT TRUE,
    created_at    TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE labs (
    lab_id        TEXT PRIMARY KEY,       -- "vscl", "emissions", etc.
    display_name  TEXT NOT NULL,
    module_source TEXT NOT NULL,          -- Artifactory URI or local path
    created_at    TIMESTAMPTZ DEFAULT now()
);

-- Lab membership (many-to-many)
CREATE TABLE lab_memberships (
    user_id       TEXT REFERENCES users(user_id),
    lab_id        TEXT REFERENCES labs(lab_id),
    joined_at     TIMESTAMPTZ DEFAULT now(),
    PRIMARY KEY (user_id, lab_id)
);

-- Roles are lab-scoped
CREATE TABLE lab_roles (
    role_id       TEXT,                   -- "viewer", "operator", "admin"
    lab_id        TEXT REFERENCES labs(lab_id),
    display_name  TEXT NOT NULL,
    description   TEXT,
    permission_level INTEGER NOT NULL DEFAULT 1,  -- maps to PermissionLevel enum
    PRIMARY KEY (role_id, lab_id)
);

-- User → Role assignment (lab-scoped)
CREATE TABLE user_lab_roles (
    user_id       TEXT REFERENCES users(user_id),
    lab_id        TEXT REFERENCES labs(lab_id),
    role_id       TEXT,
    assigned_by   TEXT REFERENCES users(user_id),
    assigned_at   TIMESTAMPTZ DEFAULT now(),
    PRIMARY KEY (user_id, lab_id, role_id),
    FOREIGN KEY (role_id, lab_id) REFERENCES lab_roles(role_id, lab_id)
);

-- Module registry
CREATE TABLE modules (
    module_id     TEXT PRIMARY KEY,       -- "modularapp.module.testrequest"
    display_name  TEXT NOT NULL,
    description   TEXT,
    registered_at TIMESTAMPTZ DEFAULT now()
);

-- Which labs have access to which modules
CREATE TABLE lab_modules (
    lab_id        TEXT REFERENCES labs(lab_id),
    module_id     TEXT REFERENCES modules(module_id),
    is_enabled    BOOLEAN DEFAULT TRUE,
    PRIMARY KEY (lab_id, module_id)
);

-- Permissions are module-scoped capabilities
CREATE TABLE permissions (
    permission_key TEXT PRIMARY KEY,      -- "test-request.edit"
    module_id      TEXT REFERENCES modules(module_id),
    description    TEXT NOT NULL,
    minimum_level  INTEGER DEFAULT 1      -- maps to PermissionLevel enum
);

-- Role → Permission grants (lab-scoped)
CREATE TABLE role_permissions (
    role_id        TEXT,
    lab_id         TEXT,
    permission_key TEXT REFERENCES permissions(permission_key),
    granted_by     TEXT REFERENCES users(user_id),
    granted_at     TIMESTAMPTZ DEFAULT now(),
    PRIMARY KEY (role_id, lab_id, permission_key),
    FOREIGN KEY (role_id, lab_id) REFERENCES lab_roles(role_id, lab_id)
);

-- Bench / workstation registry
CREATE TABLE benches (
    bench_name    TEXT NOT NULL,
    lab_id        TEXT REFERENCES labs(lab_id),
    hostname      TEXT NOT NULL,
    registered_by TEXT REFERENCES users(user_id),
    registered_at TIMESTAMPTZ DEFAULT now(),
    PRIMARY KEY (bench_name, lab_id)
);

-- ============================================
-- Query: resolve a user's effective permissions
-- ============================================
-- SELECT DISTINCT p.permission_key, p.module_id, p.minimum_level
-- FROM user_lab_roles ulr
-- JOIN role_permissions rp ON ulr.role_id = rp.role_id AND ulr.lab_id = rp.lab_id
-- JOIN permissions p ON rp.permission_key = p.permission_key
-- WHERE ulr.user_id = :userId
--   AND ulr.lab_id = ANY(:selectedLabIds);

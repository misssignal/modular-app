# Database Schema

## Status

The permission system currently uses an **in-memory stub** (`PermissionService.cs`). The SQL schema in `docs/schema/permissions.sql` is a reference implementation for the future Postgres-backed version.

## Tables

### Identity

| Table | Purpose | Primary Key |
|-------|---------|-------------|
| `users` | AD/LDAP user accounts | `user_id` (GMID username) |
| `labs` | Lab/work-area definitions with module source URIs | `lab_id` |

### Membership

| Table | Purpose | Primary Key |
|-------|---------|-------------|
| `lab_memberships` | Many-to-many user↔lab membership | `(user_id, lab_id)` |

### Roles & Permissions

| Table | Purpose | Primary Key |
|-------|---------|-------------|
| `lab_roles` | Role definitions scoped per lab | `(role_id, lab_id)` |
| `user_lab_roles` | User→role assignments within a lab | `(user_id, lab_id, role_id)` |
| `permissions` | Module-scoped capability definitions | `permission_key` |
| `role_permissions` | Role→permission grants scoped per lab | `(role_id, lab_id, permission_key)` |

### Modules

| Table | Purpose | Primary Key |
|-------|---------|-------------|
| `modules` | Registered module identities | `module_id` |
| `lab_modules` | Which labs have access to which modules | `(lab_id, module_id)` |

### Infrastructure

| Table | Purpose | Primary Key |
|-------|---------|-------------|
| `benches` | Workstation/bench registry per lab | `(bench_name, lab_id)` |

## Key Relationships

```
users ──┬── lab_memberships ──── labs
        │                         │
        └── user_lab_roles ───── lab_roles
                                  │
                            role_permissions ──── permissions ──── modules
                                                                    │
                                                              lab_modules ──── labs
```

## Effective Permission Query

To resolve all permissions for a user across their selected labs:

```sql
SELECT DISTINCT p.permission_key, p.module_id, p.minimum_level
FROM user_lab_roles ulr
JOIN role_permissions rp
  ON ulr.role_id = rp.role_id AND ulr.lab_id = rp.lab_id
JOIN permissions p
  ON rp.permission_key = p.permission_key
WHERE ulr.user_id = :userId
  AND ulr.lab_id = ANY(:selectedLabIds);
```

This query is the database equivalent of what `PermissionService.CreateContextForModule` does in memory today.

## Column Notes

- `permission_level` in `lab_roles` maps to the `PermissionLevel` enum (0–5)
- `minimum_level` in `permissions` represents the floor — users below this level cannot hold the capability regardless of role
- `assigned_by` and `granted_by` columns in role/permission tables track audit provenance
- All timestamps are `TIMESTAMPTZ` defaulting to `now()`

## Migration Path

When moving from in-memory to database-backed:

1. Deploy the schema from `docs/schema/permissions.sql`
2. Implement a new `DatabasePermissionService : IPermissionService`
3. Swap the DI registration in `App.ConfigureServices()`
4. Seed the database with the same default roles currently in `PermissionService.SeedDefaults()`
5. No module changes required — modules use `IPermissionContext`, which is unaffected

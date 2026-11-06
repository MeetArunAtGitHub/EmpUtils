-- Review with the Automation PostgreSQL DBA. This file is NOT executed by EmpUtils.
-- Verify existing indexes and EXPLAIN (ANALYZE, BUFFERS) for a representative
-- parameterized single-domain lookup before/after any approved change.
-- Run CREATE INDEX CONCURRENTLY outside a transaction and during an approved window.
-- The verified app query now filters lower(te.domainid) on the smaller employee
-- table, then joins mappings on the original domainid equality. The existing
-- apps.idx_employeeresourcemapping_domainid supports that join; do not duplicate it.
-- A read-only representative plan on 2026-11-06 returned 93 rows: server execution
-- fell from 3150 ms to 121 ms without schema changes. Connection/network costs vary.
-- The employee table was still scanned, but that was much cheaper than filtering
-- approximately seven million mapping rows. This optional expression index could
-- remove that remaining employee scan if the DBA finds its benefit worthwhile.
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_employee_resource_lower_domain
    ON efo.employee (lower(domainid));

-- Starts-with case-insensitive searches may need a separate text_pattern_ops
-- expression index. Do not add one unless actual usage/plans justify its cost.

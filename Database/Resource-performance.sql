-- Review with the Automation PostgreSQL DBA. This file is NOT executed by EmpUtils.
-- Verify existing indexes and EXPLAIN (ANALYZE, BUFFERS) for a representative
-- parameterized single-domain lookup before/after any approved change.
-- Run CREATE INDEX CONCURRENTLY outside a transaction and during an approved window.
-- Proposed indexes match the case-insensitive exact lookup and active mapping join.
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_employeeresource_active_domain_resource
    ON apps.employeeresourcemapping (lower(domainid), resourceid)
    WHERE activeind = true;

-- Only consider this if the plan shows a scan on the employee join and there is
-- no suitable existing domainid index. It preserves the existing case-sensitive join.
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_employee_resource_domain
    ON efo.employee (domainid);

-- Starts-with case-insensitive searches may need a separate text_pattern_ops
-- expression index. Do not add one unless actual usage/plans justify its cost.

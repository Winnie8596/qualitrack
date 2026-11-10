# SQL indexes for the NCR list

Phase 4, slice S2. The NCR list (D1) filters on who may see a row, on status and on overdue, then sorts and takes one page of 20 (21 rows are fetched to tell whether another page follows). This note measures the list's queries on 10,000 NCRs before and after the indexes in the `AddNcrListIndexes` migration.

## Setup

- PostgreSQL 17 (`postgres:17-alpine`, the version in `docker-compose.yml`), schema from the EF Core migrations.
- 10,000 NCRs raised over two years by 30 inspectors, with 12 investigators. 92 % are finished (8,740 closed, 460 cancelled). 800 are still in progress: 100 open, 200 investigating, 300 action in progress, 200 pending verification. This is roughly how a plant looks after using the system for a while.
- The SQL is what EF Core generates for `NcrRepository.ListAsync`, captured from the Testcontainers tests. It is run with literal values in place of the parameters.
- `EXPLAIN (ANALYZE, BUFFERS)`, run once to warm the cache and then measured. *Buffers* counts the 8 kB pages read. It is the steadier number, because timings this small vary from run to run.

## Results

| Query | Before | After |
| --- | --- | --- |
| Q1 inspector: own NCRs, newest first (`Raised_By = @id`) | Seq Scan, 456 buffers, 3.2 ms | Bitmap Index Scan on `IX_NonConformances_Raised_By`, 336 buffers, 0.9 ms |
| Q2 investigator: raised by or assigned to them (`Raised_By = @id OR InvestigatorId = @id`) | Seq Scan, 456 buffers, 2.0 ms | BitmapOr of both indexes, 457 buffers, 1.1 ms |
| Q3 QA manager: overdue, soonest due first | Seq Scan, 456 buffers, 2.1 ms | Seq Scan (unchanged), 456 buffers, 1.5 ms |
| Q4 QA manager: pending verification, newest first (`Status = @status`) | Seq Scan, 456 buffers, 1.2 ms | Index Scan on `IX_NonConformances_Status`, 12 buffers, 0.1 ms |

Before the indexes, every query reads the whole table (all 456 pages) and throws away 92–98 % of the rows (`Rows Removed by Filter: 9666`). The sort is never the cost: it is a *top-N heapsort*, which keeps only the best 21 rows in memory.

## What the plans show

- **An index pays off when the filter is selective.** Q4 matches 200 of 10,000 rows, and the index finds them by reading 12 pages instead of 456. Q1 matches 334 rows. Those rows are spread across the table, so PostgreSQL uses a *bitmap* scan: it collects the matching row locations from the index, then reads each page that holds them once, in order.
- **`OR` across two columns needs an index on each column.** In Q2, PostgreSQL ORs the two bitmaps together. It is faster than before but still touches almost every page, because investigator 3 is on 825 NCRs spread over the whole two years.
- **The overdue query cannot use these indexes.** The filter is `Status NOT IN ('Closed', 'Cancelled') AND @now > DueDate`. A btree index can't search a "not in" condition. Nearly every closed NCR also has a due date in the past, so the `DueDate` index would still lead to about 9,000 rows. The planner correctly decides a sequential scan is cheaper.
- **The planner bases its choice on statistics.** After bulk loading, run `ANALYZE` (autovacuum does it eventually). Without fresh statistics the planner may not know the indexes are worth using.

## Not done: a partial index for overdue

An index that only holds unfinished NCRs fixes Q3:

```sql
CREATE INDEX ... ON "NonConformances" ("DueDate") WHERE "Status" NOT IN ('Closed', 'Cancelled');
```

On the same data, this turns Q3 into an Index Scan (0.6 ms instead of 1.5 ms). It also stays small as closed NCRs pile up, because they are never in it. It is **not** in the migration: it would write the "finished" rule (BR-8) a second time, in SQL, outside the domain. The owner should decide whether that is worth it before the dashboard (S6), which counts overdue NCRs too.

## Takeaways

- At 10,000 rows, every query is far under the 500 ms target (PRD §8) with or without indexes. The indexes matter for growth: a sequential scan's cost grows with the table, while an index lookup's cost grows with the number of rows matched.
- Index the columns that filter rows away, measure with `EXPLAIN ANALYZE` on realistic data, and read *buffers*, not just time.
- Every index slows writes slightly and takes space. The NCR table is read far more often than written, so four indexes are cheap here.

# Repositories

## Paging

`Repository<T>.GetPagedAsync` normalises its inputs with `PageRequest.Normalize`
(`Application/Common/Results/PageRequest.cs`): page ≥ 1, 1 ≤ pageSize ≤ 100, and
a page small enough that `(page - 1) * pageSize` cannot overflow. Unclamped
input used to produce 500s for negative values and `int.MaxValue`, and let one
request read a whole table.

Pages are ordered by `InStableOrder`. When the entity maps `createdAt`, that
comes first, ascending (the closest match to the old implicit heap order).
The primary key comes after it as a tiebreaker. Without an `ORDER BY`,
PostgreSQL can return rows in any order, so consecutive pages could repeat or
skip rows. Repositories that need a different order (for example
`ProjectRepository`, newest first) page their own query and should still end
the order with the key.

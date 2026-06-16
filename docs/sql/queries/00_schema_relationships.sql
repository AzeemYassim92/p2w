/*
P2W Cards - Schema Relationships

Use this first in SSMS to understand what tables exist, how large they are,
and how the foreign keys connect the schema.
*/

/* 1. Row counts by table */
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    SUM(p.rows) AS RowCount
FROM sys.tables AS t
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
JOIN sys.partitions AS p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
GROUP BY s.name, t.name
ORDER BY RowCount DESC, t.name;

/* 2. Foreign-key map */
SELECT
    fk.name AS ForeignKeyName,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ChildSchema,
    OBJECT_NAME(fk.parent_object_id) AS ChildTable,
    pc.name AS ChildColumn,
    OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ParentSchema,
    OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
    rc.name AS ParentColumn
FROM sys.foreign_keys AS fk
JOIN sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns AS pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
JOIN sys.columns AS rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
ORDER BY ChildTable, ForeignKeyName, fkc.constraint_column_id;

/* 3. Index inventory for catalog and market tables */
SELECT
    t.name AS TableName,
    i.name AS IndexName,
    i.is_unique AS IsUnique,
    i.type_desc AS IndexType,
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS KeyColumns
FROM sys.indexes AS i
JOIN sys.tables AS t ON t.object_id = i.object_id
JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE t.name IN
(
    'Games',
    'CardSets',
    'ProductCategories',
    'CatalogProducts',
    'ProductVariants',
    'ExternalProductMappings',
    'CatalogImportRuns',
    'CatalogImportErrors',
    'CatalogImportCheckpoints',
    'MarketplaceSources',
    'CatalogMarketplaceListings',
    'CatalogMarketplaceSales',
    'CatalogMarketPriceSnapshots',
    'CatalogMarketMetrics'
)
GROUP BY t.name, i.name, i.is_unique, i.type_desc
ORDER BY t.name, i.is_unique DESC, i.name;

/* 4. Column catalog, useful when SSMS diagram view feels too dense */
SELECT
    t.name AS TableName,
    c.column_id AS ColumnOrder,
    c.name AS ColumnName,
    TYPE_NAME(c.user_type_id) AS SqlType,
    c.max_length AS MaxLength,
    c.precision AS PrecisionValue,
    c.scale AS ScaleValue,
    c.is_nullable AS IsNullable
FROM sys.tables AS t
JOIN sys.columns AS c ON c.object_id = t.object_id
WHERE t.name LIKE 'Catalog%'
   OR t.name IN ('Games', 'CardSets', 'ProductCategories', 'ProductVariants', 'MarketplaceSources', 'ExternalProductMappings')
ORDER BY t.name, c.column_id;

/* 5. Tables without foreign keys, good for spotting roots or legacy tables */
SELECT
    t.name AS TableName
FROM sys.tables AS t
WHERE NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys AS fk
    WHERE fk.parent_object_id = t.object_id
)
ORDER BY t.name;

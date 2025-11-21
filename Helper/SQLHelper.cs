using DataFileReader.Class;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using Newtonsoft.Json;

namespace DataFileReader.Helper;

public static class SQLHelper
{
    /// <summary>
    /// Creates the standardized HealthMetrics table if it doesn't exist
    /// </summary>
    public static void EnsureHealthMetricsTableExists()
    {
        var connectionString = ConfigurationManager.AppSettings["HealthDB"];
        var createTableQuery = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetrics]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [dbo].[HealthMetrics](
                    [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                    [Provider] NVARCHAR(50) NULL,
                    [MetricType] NVARCHAR(100) NULL,
                    [MetricSubtype] NVARCHAR(200) NULL,
                    [SourceFile] NVARCHAR(500) NULL,
                    [NormalizedTimestamp] DATETIME2 NULL,
                    [RawTimestamp] NVARCHAR(100) NULL,
                    [Value] DECIMAL(18,4) NULL,
                    [Unit] NVARCHAR(50) NULL,
                    [Metadata] NVARCHAR(MAX) NULL,
                    [CreatedDate] DATETIME2 DEFAULT GETDATE()
                )
                
                -- Optimized composite indexes for common query patterns
                -- Index for queries filtering by MetricType and date range (most common pattern)
                CREATE NONCLUSTERED INDEX IX_HealthMetrics_MetricType_Timestamp 
                    ON HealthMetrics(MetricType, NormalizedTimestamp) 
                    INCLUDE (Value, Unit, Provider)
                    WHERE NormalizedTimestamp IS NOT NULL AND Value IS NOT NULL
                
                -- Index for queries filtering by MetricType, MetricSubtype, and date range
                CREATE NONCLUSTERED INDEX IX_HealthMetrics_MetricType_Subtype_Timestamp 
                    ON HealthMetrics(MetricType, MetricSubtype, NormalizedTimestamp) 
                    INCLUDE (Value, Unit, Provider)
                    WHERE NormalizedTimestamp IS NOT NULL AND Value IS NOT NULL
                
                -- Index for timestamp-only queries (if needed)
                CREATE NONCLUSTERED INDEX IX_HealthMetrics_Timestamp 
                    ON HealthMetrics(NormalizedTimestamp)
                    WHERE NormalizedTimestamp IS NOT NULL
                
                -- Index for SourceFile lookups (for duplicate file detection)
                CREATE NONCLUSTERED INDEX IX_HealthMetrics_SourceFile 
                    ON HealthMetrics(SourceFile)
                    WHERE SourceFile IS NOT NULL
            END";

        try
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = new SqlCommand(createTableQuery, sqlConnection))
                {
                    sqlCommand.ExecuteNonQuery();
                }
            }

            // Ensure MetricSubtype column exists in existing tables
            if (!string.IsNullOrEmpty(connectionString))
            {
                EnsureMetricSubtypeColumnExists(connectionString);
                // Create smaller-resolution HealthMetrics tables if they don't exist
                EnsureHealthMetricsResolutionTablesExist(connectionString);
                // Optimize indexes for existing tables (will skip if table was just created with optimized indexes)
                OptimizeHealthMetricsIndexes();
                // Ensure the summary counts table exists
                EnsureHealthMetricsCountsTableExists(connectionString);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating HealthMetrics table: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Ensures the per-resolution HealthMetrics tables exist (Second, Minute, Hour, Day, Week, Month, Year)
    /// These tables are lightweight versions of HealthMetrics containing only:
    /// Id, MetricType, MetricSubtype, NormalizedTimestamp, Value, Unit
    /// Column names are consistent with the main HealthMetrics table.
    /// </summary>
    private static void EnsureHealthMetricsResolutionTablesExist(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)) return;

        var tableNames = new[]
        {
            "HealthMetricsSecond",
            "HealthMetricsMinute",
            "HealthMetricsHour",
            "HealthMetricsDay",
            "HealthMetricsWeek",
            "HealthMetricsMonth",
            "HealthMetricsYear"
        };

        var sb = new StringBuilder();

        foreach (var table in tableNames)
        {
            sb.Append($@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{table}]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[{table}](
                        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
                        [MetricType] NVARCHAR(100) NULL,
                        [MetricSubtype] NVARCHAR(200) NULL,
                        [NormalizedTimestamp] DATETIME2 NULL,
                        [Value] DECIMAL(18,4) NULL,
                        [Unit] NVARCHAR(50) NULL
                    );

                    -- Useful index for MetricType + timestamp queries
                    CREATE NONCLUSTERED INDEX IX_{table}_MetricType_Timestamp
                        ON [dbo].[{table}](MetricType, NormalizedTimestamp)
                        INCLUDE (Value, Unit)
                        WHERE NormalizedTimestamp IS NOT NULL AND Value IS NOT NULL;

                    -- Index on timestamp only for time-range queries
                    CREATE NONCLUSTERED INDEX IX_{table}_Timestamp
                        ON [dbo].[{table}](NormalizedTimestamp)
                        WHERE NormalizedTimestamp IS NOT NULL;
                END
            ");
        }

        try
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = new SqlCommand(sb.ToString(), sqlConnection))
                {
                    sqlCommand.CommandTimeout = 120;
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            // Log but do not throw - these helper tables should not block main operations
            Console.WriteLine($"Warning: Error creating resolution HealthMetrics tables: {ex.Message}");
        }
    }

    /// <summary>
    /// Optimizes existing HealthMetrics table by replacing old indexes with optimized composite indexes
    /// This should be called after table creation or when optimizing an existing large table
    /// </summary>
    public static void OptimizeHealthMetricsIndexes()
    {
        var connectionString = ConfigurationManager.AppSettings["HealthDB"];
        if (string.IsNullOrEmpty(connectionString)) return;

        var optimizeIndexesQuery = @"
            -- Check if table exists
            IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetrics]') AND type in (N'U'))
            BEGIN
                -- Drop old inefficient indexes if they exist
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HealthMetrics_Provider_Metric' AND object_id = OBJECT_ID(N'[dbo].[HealthMetrics]'))
                    DROP INDEX IX_HealthMetrics_Provider_Metric ON [dbo].[HealthMetrics];
                
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HealthMetrics_MetricSubtype' AND object_id = OBJECT_ID(N'[dbo].[HealthMetrics]'))
                    DROP INDEX IX_HealthMetrics_MetricSubtype ON [dbo].[HealthMetrics];
                
                -- Drop old timestamp index if it doesn't have the filtered WHERE clause
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HealthMetrics_Timestamp' AND object_id = OBJECT_ID(N'[dbo].[HealthMetrics]'))
                BEGIN
                    -- Check if it's the old unfiltered version (has_filter = 0 means no filter)
                    IF EXISTS (
                        SELECT * FROM sys.indexes 
                        WHERE name = 'IX_HealthMetrics_Timestamp' 
                        AND object_id = OBJECT_ID(N'[dbo].[HealthMetrics]')
                        AND has_filter = 0
                    )
                    BEGIN
                        DROP INDEX IX_HealthMetrics_Timestamp ON [dbo].[HealthMetrics];
                    END
                END
                
                -- Create optimized composite index for MetricType + NormalizedTimestamp queries (if it doesn't exist)
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HealthMetrics_MetricType_Timestamp' AND object_id = OBJECT_ID(N'[dbo].[HealthMetrics]'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_HealthMetrics_MetricType_Timestamp 
                        ON [dbo].[HealthMetrics](MetricType, NormalizedTimestamp) 
                        INCLUDE (Value, Unit, Provider)
                        WHERE NormalizedTimestamp IS NOT NULL AND Value IS NOT NULL;
                END
                
                -- Create optimized composite index for MetricType + MetricSubtype + NormalizedTimestamp queries (if it doesn't exist)
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HealthMetrics_MetricType_Subtype_Timestamp' AND object_id = OBJECT_ID(N'[dbo].[HealthMetrics]'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_HealthMetrics_MetricType_Subtype_Timestamp 
                        ON [dbo].[HealthMetrics](MetricType, MetricSubtype, NormalizedTimestamp) 
                        INCLUDE (Value, Unit, Provider)
                        WHERE NormalizedTimestamp IS NOT NULL AND Value IS NOT NULL;
                END
                
                -- Create filtered timestamp index (if it doesn't exist)
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HealthMetrics_Timestamp' AND object_id = OBJECT_ID(N'[dbo].[HealthMetrics]'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_HealthMetrics_Timestamp 
                        ON [dbo].[HealthMetrics](NormalizedTimestamp)
                        WHERE NormalizedTimestamp IS NOT NULL;
                END
                
                -- Ensure SourceFile index exists (if it doesn't exist)
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HealthMetrics_SourceFile' AND object_id = OBJECT_ID(N'[dbo].[HealthMetrics]'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_HealthMetrics_SourceFile 
                        ON [dbo].[HealthMetrics](SourceFile)
                        WHERE SourceFile IS NOT NULL;
                END
            END";

        try
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = new SqlCommand(optimizeIndexesQuery, sqlConnection))
                {
                    sqlCommand.CommandTimeout = 300; // 5 minutes timeout for large tables
                    sqlCommand.ExecuteNonQuery();
                    Console.WriteLine("HealthMetrics table indexes optimized successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error optimizing HealthMetrics indexes: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates the HealthMetricsCounts summary table if it doesn't exist
    /// This table stores the count of records for each MetricType/MetricSubtype combination
    /// </summary>
    private static void EnsureHealthMetricsCountsTableExists(string connectionString)
    {
        try
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                var createTableQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[HealthMetricsCounts](
                            [MetricType] NVARCHAR(100) NOT NULL,
                            [MetricSubtype] NVARCHAR(200) NOT NULL DEFAULT '',
                            [RecordCount] BIGINT NOT NULL DEFAULT 0,
                            [EarliestDateTime] DATETIME2 NULL,
                            [MostRecentDateTime] DATETIME2 NULL,
                            [DaysBetween] AS CAST(DATEDIFF(day, [EarliestDateTime], [MostRecentDateTime]) AS DECIMAL(18,2)) PERSISTED,
                            [DaysPerRecord] AS CAST(CAST(DATEDIFF(day, [EarliestDateTime], [MostRecentDateTime]) AS DECIMAL(18,5)) / NULLIF([RecordCount], 0) AS DECIMAL(18,5)) PERSISTED,
                            [LastUpdated] DATETIME2 NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT PK_HealthMetricsCounts PRIMARY KEY CLUSTERED (MetricType, MetricSubtype)
                        )
                        
                        -- Index for fast lookups
                        CREATE NONCLUSTERED INDEX IX_HealthMetricsCounts_RecordCount 
                            ON [dbo].[HealthMetricsCounts](RecordCount DESC)
                    END
                    ELSE
                    BEGIN
                        -- Add new columns if they don't exist (migration for existing tables)
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND name = 'EarliestDateTime')
                        BEGIN
                            ALTER TABLE [dbo].[HealthMetricsCounts]
                            ADD [EarliestDateTime] DATETIME2 NULL;
                        END
                        
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND name = 'MostRecentDateTime')
                        BEGIN
                            ALTER TABLE [dbo].[HealthMetricsCounts]
                            ADD [MostRecentDateTime] DATETIME2 NULL;
                        END
                        
                        -- Add computed columns if they don't exist
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND name = 'DaysBetween')
                        BEGIN
                            ALTER TABLE [dbo].[HealthMetricsCounts]
                            ADD [DaysBetween] AS CAST(DATEDIFF(day, [EarliestDateTime], [MostRecentDateTime]) AS DECIMAL(18,2)) PERSISTED;
                        END
                        
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND name = 'DaysPerRecord')
                        BEGIN
                            ALTER TABLE [dbo].[HealthMetricsCounts]
                            ADD [DaysPerRecord] AS CAST(CAST(DATEDIFF(day, [EarliestDateTime], [MostRecentDateTime]) AS DECIMAL(18,5)) / NULLIF([RecordCount], 0) AS DECIMAL(18,5)) PERSISTED;
                        END
                    END";

                using (var sqlCommand = new SqlCommand(createTableQuery, sqlConnection))
                {
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating HealthMetricsCounts table: {ex.Message}");
            // Don't throw - this is a helper table, shouldn't block main operations
        }
    }

    /// <summary>
    /// Initializes the HealthMetricsCounts table by calculating counts from existing HealthMetrics data
    /// This should be called once to populate the summary table from existing data
    /// </summary>
    public static void InitializeHealthMetricsCounts()
    {
        var connectionString = ConfigurationManager.AppSettings["HealthDB"];
        if (string.IsNullOrEmpty(connectionString)) return;

        try
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                var initializeQuery = @"
                    -- Ensure table exists first
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[HealthMetricsCounts](
                            [MetricType] NVARCHAR(100) NOT NULL,
                            [MetricSubtype] NVARCHAR(200) NOT NULL DEFAULT '',
                            [RecordCount] BIGINT NOT NULL DEFAULT 0,
                            [EarliestDateTime] DATETIME2 NULL,
                            [MostRecentDateTime] DATETIME2 NULL,
                            [DaysBetween] AS CAST(DATEDIFF(day, [EarliestDateTime], [MostRecentDateTime]) AS DECIMAL(18,2)) PERSISTED,
                            [DaysPerRecord] AS CAST(CAST(DATEDIFF(day, [EarliestDateTime], [MostRecentDateTime]) AS DECIMAL(18,5)) / NULLIF([RecordCount], 0) AS DECIMAL(18,5)) PERSISTED,
                            [LastUpdated] DATETIME2 NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT PK_HealthMetricsCounts PRIMARY KEY CLUSTERED (MetricType, MetricSubtype)
                        )
                        
                        CREATE NONCLUSTERED INDEX IX_HealthMetricsCounts_RecordCount 
                            ON [dbo].[HealthMetricsCounts](RecordCount DESC)
                    END
                    ELSE
                    BEGIN
                        -- Add new columns if they don't exist (migration for existing tables)
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND name = 'EarliestDateTime')
                        BEGIN
                            ALTER TABLE [dbo].[HealthMetricsCounts]
                            ADD [EarliestDateTime] DATETIME2 NULL;
                        END
                        
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND name = 'MostRecentDateTime')
                        BEGIN
                            ALTER TABLE [dbo].[HealthMetricsCounts]
                            ADD [MostRecentDateTime] DATETIME2 NULL;
                        END
                        
                        -- Add computed columns if they don't exist
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND name = 'DaysBetween')
                        BEGIN
                            ALTER TABLE [dbo].[HealthMetricsCounts]
                            ADD [DaysBetween] AS CAST(DATEDIFF(day, [EarliestDateTime], [MostRecentDateTime]) AS DECIMAL(18,2)) PERSISTED;
                        END
                        
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND name = 'DaysPerRecord')
                        BEGIN
                            ALTER TABLE [dbo].[HealthMetricsCounts]
                            ADD [DaysPerRecord] AS CAST(CAST(DATEDIFF(day, [EarliestDateTime], [MostRecentDateTime]) AS DECIMAL(18,5)) / NULLIF([RecordCount], 0) AS DECIMAL(18,5)) PERSISTED;
                        END
                    END
                    
                    -- Clear existing counts and recalculate from HealthMetrics
                    TRUNCATE TABLE [dbo].[HealthMetricsCounts];
                    
                    -- Insert counts grouped by MetricType and MetricSubtype
                    -- Only count records with valid timestamps (NormalizedTimestamp is not null, OR RawTimestamp is not null and not empty)
                    -- Also calculate min and max timestamps for each combination
                    INSERT INTO [dbo].[HealthMetricsCounts] (MetricType, MetricSubtype, RecordCount, EarliestDateTime, MostRecentDateTime, LastUpdated)
                    SELECT 
                        ISNULL(MetricType, '') AS MetricType,
                        ISNULL(MetricSubtype, '') AS MetricSubtype,
                        COUNT(*) AS RecordCount,
                        MIN(NormalizedTimestamp) AS EarliestDateTime,
                        MAX(NormalizedTimestamp) AS MostRecentDateTime,
                        GETDATE() AS LastUpdated
                    FROM [dbo].[HealthMetrics]
                    WHERE MetricType IS NOT NULL
                      AND (NormalizedTimestamp IS NOT NULL 
                           OR (RawTimestamp IS NOT NULL AND RawTimestamp != ''))
                    GROUP BY MetricType, MetricSubtype";

                using (var sqlCommand = new SqlCommand(initializeQuery, sqlConnection))
                {
                    sqlCommand.CommandTimeout = 300; // 5 minutes timeout for large tables
                    sqlCommand.ExecuteNonQuery();
                    Console.WriteLine("HealthMetricsCounts table initialized successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing HealthMetricsCounts: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds MetricSubtype column to existing HealthMetrics table if it doesn't exist
    /// </summary>
    private static void EnsureMetricSubtypeColumnExists(string connectionString)
    {
        try
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                // Check if column exists
                var checkColumnQuery = @"
                    IF NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetrics]') 
                        AND name = 'MetricSubtype'
                    )
                    BEGIN
                        ALTER TABLE [dbo].[HealthMetrics]
                        ADD [MetricSubtype] NVARCHAR(200) NULL;
                        
                        CREATE INDEX IX_HealthMetrics_MetricSubtype ON HealthMetrics(MetricSubtype);
                    END";

                using (var sqlCommand = new SqlCommand(checkColumnQuery, sqlConnection))
                {
                    sqlCommand.ExecuteNonQuery();
                }

                // More comprehensive update using MetricTypeParser logic
                // This will parse all non-alphanumeric delimiters and update existing records
                UpdateExistingMetricSubtypes(connectionString);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error ensuring MetricSubtype column exists: {ex.Message}");
            // Don't throw - this is a migration step that might fail if column already exists
        }
    }

    /// <summary>
    /// Updates existing records to populate MetricSubtype by parsing MetricType
    /// </summary>
    private static void UpdateExistingMetricSubtypes(string connectionString)
    {
        try
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                // Get all distinct MetricTypes that need updating
                // We need to update records where MetricType contains a subtype
                // This includes records where MetricSubtype is not set, or where MetricType still contains the subtype suffix
                var getMetricTypesQuery = @"
                    SELECT DISTINCT [MetricType], [MetricSubtype]
                    FROM [dbo].[HealthMetrics]
                    WHERE [MetricType] IS NOT NULL";

                var recordsToUpdate = new List<(string OriginalMetricType, string BaseType, string Subtype)>();
                using (var sqlCommand = new SqlCommand(getMetricTypesQuery, sqlConnection))
                {
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var metricType = reader["MetricType"]?.ToString();
                            var existingSubtype = reader["MetricSubtype"]?.ToString();

                            if (!string.IsNullOrEmpty(metricType))
                            {
                                var parsedSubtype = MetricTypeParser.GetSubtypeString(metricType);
                                var baseType = MetricTypeParser.GetBaseType(metricType);

                                // If MetricType contains a subtype (baseType != metricType), we need to update
                                if (!string.IsNullOrEmpty(parsedSubtype) && baseType != metricType)
                                {
                                    // Use existing subtype if it's already set, otherwise use parsed subtype
                                    var subtypeToUse = !string.IsNullOrEmpty(existingSubtype) ? existingSubtype : parsedSubtype;
                                    recordsToUpdate.Add((metricType, baseType, subtypeToUse));
                                }
                            }
                        }
                    }
                }

                // Update each record: set MetricSubtype (if not already set) and update MetricType to be just the base type
                foreach (var record in recordsToUpdate)
                {
                    var updateQuery = @"
                        UPDATE [dbo].[HealthMetrics]
                        SET [MetricType] = @BaseType,
                            [MetricSubtype] = CASE 
                                WHEN ([MetricSubtype] IS NULL OR [MetricSubtype] = '') THEN @Subtype
                                ELSE [MetricSubtype]
                            END
                        WHERE [MetricType] = @OriginalMetricType";

                    using (var updateCommand = new SqlCommand(updateQuery, sqlConnection))
                    {
                        updateCommand.Parameters.AddWithValue("@Subtype", record.Subtype);
                        updateCommand.Parameters.AddWithValue("@BaseType", record.BaseType);
                        updateCommand.Parameters.AddWithValue("@OriginalMetricType", record.OriginalMetricType);
                        updateCommand.ExecuteNonQuery();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating existing MetricSubtypes: {ex.Message}");
            // Don't throw - this is a migration step
        }
    }

    /// <summary>
    /// Gets distinct SourceFile values from HealthMetrics table (files that have already been processed)
    /// </summary>
    public static HashSet<string> GetProcessedFiles()
    {
        var connectionString = ConfigurationManager.AppSettings["HealthDB"];
        var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                var sql = "SELECT DISTINCT SourceFile FROM HealthMetrics WHERE SourceFile IS NOT NULL AND SourceFile != ''";

                using (var sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var sourceFile = reader["SourceFile"]?.ToString();
                            if (!string.IsNullOrEmpty(sourceFile))
                            {
                                processedFiles.Add(sourceFile);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // If table doesn't exist yet, return empty set (no files processed yet)
            Console.WriteLine($"Note: Could not retrieve processed files list: {ex.Message}");
        }

        return processedFiles;
    }

    /// <summary>
    /// Marks a file as processed by inserting a minimal record (used for empty files)
    /// </summary>
    public static void MarkFileAsProcessed(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;

        var connectionString = ConfigurationManager.AppSettings["HealthDB"];

        try
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                // Check if file is already marked as processed
                var checkQuery = "SELECT COUNT(*) FROM HealthMetrics WHERE SourceFile = @SourceFile";
                using (var checkCommand = new SqlCommand(checkQuery, sqlConnection))
                {
                    checkCommand.Parameters.AddWithValue("@SourceFile", filePath);
                    var count = (int)checkCommand.ExecuteScalar();
                    if (count > 0)
                    {
                        // File already marked as processed
                        return;
                    }
                }

                // Insert minimal record to mark file as processed
                var insertQuery = @"
                    INSERT INTO HealthMetrics 
                    (Provider, MetricType, MetricSubtype, SourceFile, NormalizedTimestamp, RawTimestamp, Value, Unit, Metadata)
                    VALUES 
                    (@Provider, @MetricType, @MetricSubtype, @SourceFile, @NormalizedTimestamp, @RawTimestamp, @Value, @Unit, @Metadata)";

                using (var sqlCommand = new SqlCommand(insertQuery, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@Provider", DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@MetricType", DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@MetricSubtype", DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@SourceFile", filePath);
                    sqlCommand.Parameters.AddWithValue("@NormalizedTimestamp", DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@RawTimestamp", DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@Value", DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@Unit", DBNull.Value);
                    sqlCommand.Parameters.AddWithValue("@Metadata", DBNull.Value);

                    sqlCommand.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error marking file as processed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Inserts health metrics into the database using bulk insert for performance
    /// Also updates the HealthMetricsCounts summary table incrementally
    /// </summary>
    public static void InsertHealthMetrics(List<HealthMetric> metrics)
    {
        if (metrics == null || metrics.Count == 0) return;

        var connectionString = ConfigurationManager.AppSettings["HealthDB"];

        try
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                // Use parameterized query for safety and performance
                var insertQuery = @"
                    INSERT INTO HealthMetrics 
                    (Provider, MetricType, MetricSubtype, SourceFile, NormalizedTimestamp, RawTimestamp, Value, Unit, Metadata)
                    VALUES 
                    (@Provider, @MetricType, @MetricSubtype, @SourceFile, @NormalizedTimestamp, @RawTimestamp, @Value, @Unit, @Metadata)";

                // Track MetricType/Subtype combinations for summary table update
                // Store count, min timestamp, and max timestamp for each combination
                var typeSubtypeData = new Dictionary<(string MetricType, string MetricSubtype), (int Count, DateTime? MinTimestamp, DateTime? MaxTimestamp)>();

                foreach (var metric in metrics)
                {
                    // Parse MetricType to extract base type and subtype
                    string baseType = metric.MetricType ?? string.Empty;
                    string subtype = metric.MetricSubtype ?? string.Empty;

                    if (!string.IsNullOrEmpty(metric.MetricType))
                    {
                        // If subtype not already set, parse it from MetricType
                        if (string.IsNullOrEmpty(subtype))
                        {
                            subtype = MetricTypeParser.GetSubtypeString(metric.MetricType) ?? string.Empty;
                        }

                        // If a subtype exists, update MetricType to be just the base type
                        if (!string.IsNullOrEmpty(subtype))
                        {
                            baseType = MetricTypeParser.GetBaseType(metric.MetricType);
                        }
                    }

                    // Only track counts for records with MetricType AND a valid timestamp
                    // Valid timestamp means: NormalizedTimestamp is not null, OR RawTimestamp is not null and not empty
                    if (!string.IsNullOrEmpty(baseType))
                    {
                        bool hasValidTimestamp = (metric.NormalizedTimestamp.HasValue) ||
                                                 (!string.IsNullOrEmpty(metric.RawTimestamp));

                        if (hasValidTimestamp && metric.NormalizedTimestamp.HasValue)
                        {
                            var key = (MetricType: baseType, MetricSubtype: subtype ?? string.Empty);
                            var timestamp = metric.NormalizedTimestamp.Value;

                            if (!typeSubtypeData.ContainsKey(key))
                            {
                                typeSubtypeData[key] = (Count: 0, MinTimestamp: timestamp, MaxTimestamp: timestamp);
                            }

                            var current = typeSubtypeData[key];
                            typeSubtypeData[key] = (
                                Count: current.Count + 1,
                                MinTimestamp: timestamp < current.MinTimestamp ? timestamp : current.MinTimestamp,
                                MaxTimestamp: timestamp > current.MaxTimestamp ? timestamp : current.MaxTimestamp
                            );
                        }
                        else if (hasValidTimestamp)
                        {
                            // Has RawTimestamp but no NormalizedTimestamp - count it but don't track timestamp
                            var key = (MetricType: baseType, MetricSubtype: subtype ?? string.Empty);
                            if (!typeSubtypeData.ContainsKey(key))
                            {
                                typeSubtypeData[key] = (Count: 0, MinTimestamp: null, MaxTimestamp: null);
                            }
                            var current = typeSubtypeData[key];
                            typeSubtypeData[key] = (Count: current.Count + 1, MinTimestamp: current.MinTimestamp, MaxTimestamp: current.MaxTimestamp);
                        }
                    }

                    using (var sqlCommand = new SqlCommand(insertQuery, sqlConnection))
                    {
                        sqlCommand.Parameters.AddWithValue("@Provider", (object)metric.Provider ?? DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("@MetricType", string.IsNullOrEmpty(baseType) ? DBNull.Value : (object)baseType);
                        sqlCommand.Parameters.AddWithValue("@MetricSubtype", string.IsNullOrEmpty(subtype) ? DBNull.Value : (object)subtype);
                        sqlCommand.Parameters.AddWithValue("@SourceFile", (object)metric.SourceFile ?? DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("@NormalizedTimestamp", (object)metric.NormalizedTimestamp ?? DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("@RawTimestamp", (object)metric.RawTimestamp ?? DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("@Value", (object)metric.Value ?? DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("@Unit", (object)metric.Unit ?? DBNull.Value);

                        // Serialize additional fields to JSON
                        var metadataJson = metric.AdditionalFields.Count > 0
                            ? JsonConvert.SerializeObject(metric.AdditionalFields)
                            : (object)DBNull.Value;
                        sqlCommand.Parameters.AddWithValue("@Metadata", metadataJson);

                        sqlCommand.ExecuteNonQuery();
                    }
                }

                // Update the summary counts table for all MetricType/Subtype combinations in this batch
                if (typeSubtypeData.Count > 0)
                {
                    UpdateHealthMetricsCounts(sqlConnection, typeSubtypeData);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting health metrics: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates the HealthMetricsCounts summary table with new record counts and timestamp ranges
    /// Uses MERGE to efficiently handle inserts and updates in a single operation
    /// </summary>
    private static void UpdateHealthMetricsCounts(SqlConnection connection, Dictionary<(string MetricType, string MetricSubtype), (int Count, DateTime? MinTimestamp, DateTime? MaxTimestamp)> typeSubtypeData)
    {
        try
        {
            // Ensure the table exists before trying to update it
            EnsureHealthMetricsCountsTableExists(connection);

            // Use MERGE to efficiently update or insert counts and timestamps
            var mergeQuery = @"
                MERGE [dbo].[HealthMetricsCounts] AS target
                USING (VALUES {0}) AS source (MetricType, MetricSubtype, IncrementCount, MinTimestamp, MaxTimestamp)
                ON target.MetricType = source.MetricType 
                   AND target.MetricSubtype = source.MetricSubtype
                WHEN MATCHED THEN
                    UPDATE SET 
                        RecordCount = target.RecordCount + source.IncrementCount,
                        EarliestDateTime = CASE 
                            WHEN source.MinTimestamp IS NOT NULL AND (target.EarliestDateTime IS NULL OR source.MinTimestamp < target.EarliestDateTime)
                            THEN source.MinTimestamp
                            ELSE target.EarliestDateTime
                        END,
                        MostRecentDateTime = CASE
                            WHEN source.MaxTimestamp IS NOT NULL AND (target.MostRecentDateTime IS NULL OR source.MaxTimestamp > target.MostRecentDateTime)
                            THEN source.MaxTimestamp
                            ELSE target.MostRecentDateTime
                        END,
                        LastUpdated = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (MetricType, MetricSubtype, RecordCount, EarliestDateTime, MostRecentDateTime, LastUpdated)
                    VALUES (source.MetricType, source.MetricSubtype, source.IncrementCount, source.MinTimestamp, source.MaxTimestamp, GETDATE());";

            // Build the VALUES clause for all combinations
            var valuesList = new List<string>();
            var parameters = new List<SqlParameter>();
            int paramIndex = 0;

            foreach (var kvp in typeSubtypeData)
            {
                var metricTypeParam = $"@MetricType{paramIndex}";
                var metricSubtypeParam = $"@MetricSubtype{paramIndex}";
                var countParam = $"@Count{paramIndex}";
                var minTimestampParam = $"@MinTimestamp{paramIndex}";
                var maxTimestampParam = $"@MaxTimestamp{paramIndex}";

                valuesList.Add($"({metricTypeParam}, {metricSubtypeParam}, {countParam}, {minTimestampParam}, {maxTimestampParam})");

                parameters.Add(new SqlParameter(metricTypeParam, kvp.Key.MetricType));
                parameters.Add(new SqlParameter(metricSubtypeParam, kvp.Key.MetricSubtype ?? string.Empty));
                parameters.Add(new SqlParameter(countParam, kvp.Value.Count));
                parameters.Add(new SqlParameter(minTimestampParam, (object)kvp.Value.MinTimestamp ?? DBNull.Value));
                parameters.Add(new SqlParameter(maxTimestampParam, (object)kvp.Value.MaxTimestamp ?? DBNull.Value));

                paramIndex++;
            }

            var finalQuery = string.Format(mergeQuery, string.Join(", ", valuesList));

            using (var sqlCommand = new SqlCommand(finalQuery, connection))
            {
                sqlCommand.Parameters.AddRange(parameters.ToArray());
                sqlCommand.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - summary table updates shouldn't block main inserts
            Console.WriteLine($"Warning: Error updating HealthMetricsCounts: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates the HealthMetricsCounts summary table if it doesn't exist (overload that uses existing connection)
    /// </summary>
    private static void EnsureHealthMetricsCountsTableExists(SqlConnection connection)
    {
        try
        {
            var createTableQuery = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[HealthMetricsCounts](
                        [MetricType] NVARCHAR(100) NOT NULL,
                        [MetricSubtype] NVARCHAR(200) NOT NULL DEFAULT '',
                        [RecordCount] BIGINT NOT NULL DEFAULT 0,
                        [EarliestDateTime] DATETIME2 NULL,
                        [MostRecentDateTime] DATETIME2 NULL,
                        [DaysBetween] AS CAST(DATEDIFF(day, [EarliestDateTime], [MostRecentDateTime]) AS DECIMAL(18,2)) PERSISTED,
                        [DaysPerRecord] AS CAST(CAST(DATEDIFF(day, [EarliestDateTime], [MostRecentDateTime]) AS DECIMAL(18,5)) / NULLIF([RecordCount], 0) AS DECIMAL(18,5)) PERSISTED,
                        [LastUpdated] DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT PK_HealthMetricsCounts PRIMARY KEY CLUSTERED (MetricType, MetricSubtype)
                    )
                    
                    -- Index for fast lookups
                    CREATE NONCLUSTERED INDEX IX_HealthMetricsCounts_RecordCount 
                        ON [dbo].[HealthMetricsCounts](RecordCount DESC)
                END
                ELSE
                BEGIN
                    -- Add new columns if they don't exist (migration for existing tables)
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND name = 'EarliestDateTime')
                    BEGIN
                        ALTER TABLE [dbo].[HealthMetricsCounts]
                        ADD [EarliestDateTime] DATETIME2 NULL;
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND name = 'MostRecentDateTime')
                    BEGIN
                        ALTER TABLE [dbo].[HealthMetricsCounts]
                        ADD [MostRecentDateTime] DATETIME2 NULL;
                    END
                    
                    -- Add computed columns if they don't exist
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND name = 'DaysBetween')
                    BEGIN
                        ALTER TABLE [dbo].[HealthMetricsCounts]
                        ADD [DaysBetween] AS CAST(DATEDIFF(day, [EarliestDateTime], [MostRecentDateTime]) AS DECIMAL(18,2)) PERSISTED;
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HealthMetricsCounts]') AND name = 'DaysPerRecord')
                    BEGIN
                        ALTER TABLE [dbo].[HealthMetricsCounts]
                        ADD [DaysPerRecord] AS CAST(CAST(DATEDIFF(day, [EarliestDateTime], [MostRecentDateTime]) AS DECIMAL(18,5)) / NULLIF([RecordCount], 0) AS DECIMAL(18,5)) PERSISTED;
                    END
                END";

            using (var sqlCommand = new SqlCommand(createTableQuery, connection))
            {
                sqlCommand.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating HealthMetricsCounts table: {ex.Message}");
            // Don't throw - this is a helper table, shouldn't block main operations
        }
    }
    public static void CreateSQLTable(MetaData metaData)
    {
        var connectionString = ConfigurationManager.AppSettings["HealthDB"];
        var tableName = metaData.Name ?? $"ID_{metaData.ID}";

        DeleteSQLTable(tableName);

        var createTableQuery = $"CREATE TABLE [{tableName}]({string.Join(", ", metaData.Fields.Keys.Select(field => $"[{field}] VARCHAR(MAX) NULL"))})";

        using (var sqlConnection = new SqlConnection(connectionString))
        {
            sqlConnection.Open();
            using (var sqlCommand = new SqlCommand(createTableQuery, sqlConnection))
            {
                sqlCommand.ExecuteNonQuery();
            }
        }
    }

    public static void UpdateSQLTable(MetaData metaData, string fileContent)
    {
        var connectionString = ConfigurationManager.AppSettings["HealthDB"];
        var sqlQuery = string.Empty;

        try
        {
            var contentLines = fileContent.Split('\n');
            var tableName = metaData.Name ?? $"ID_{Math.Abs(metaData.ID)}";

            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                foreach (var contentLine in contentLines)
                {
                    var line = contentLine.Trim();
                    var fieldData = line.Split(',');

                    if (metaData != null && fieldData.Length >= metaData.Fields.Count)
                    {
                        sqlQuery = $"INSERT INTO {tableName} ({string.Join(", ", metaData.Fields.Keys)}) VALUES ({string.Join(", ", fieldData.Select(fd => $"'{fd}'"))})";

                        using (var sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                        {
                            sqlCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public static void DeleteSQLTable(string tableName)
    {
        var connectionString = ConfigurationManager.AppSettings["HealthDB"];
        var sqlConnection = new SqlConnection(connectionString);
        var sqlQuery = $"DROP TABLE {tableName}";

        try
        {
            sqlConnection.Open();
            using var sqlCommand = new SqlCommand(sqlQuery, sqlConnection);
            sqlCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw; // Re-throw the exception instead of just logging it
        }
        finally
        {
            sqlConnection.Close();
        }
    }

    /// <summary>
    /// Retrieves health metrics data from the HealthMetricsWeek table
    /// </summary>
    /// <param name="metricType">Optional filter by MetricType</param>
    /// <param name="metricSubtype">Optional filter by MetricSubtype</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <returns>List of weekly aggregated health metrics</returns>
    public static List<HealthMetric> GetHealthMetricsWeek(
        string? metricType = null,
        string? metricSubtype = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var connectionString = ConfigurationManager.AppSettings["HealthDB"];
        var results = new List<HealthMetric>();

        try
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                var sql = new StringBuilder(@"
                    SELECT 
                        MetricType,
                        MetricSubtype,
                        NormalizedTimestamp,
                        Value,
                        Unit
                    FROM [dbo].[HealthMetricsWeek]
                    WHERE 1=1");

                var parameters = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(metricType))
                {
                    sql.Append(" AND MetricType = @MetricType");
                    parameters.Add(new SqlParameter("@MetricType", metricType));
                }

                if (!string.IsNullOrEmpty(metricSubtype))
                {
                    sql.Append(" AND MetricSubtype = @MetricSubtype");
                    parameters.Add(new SqlParameter("@MetricSubtype", metricSubtype));
                }

                if (fromDate.HasValue)
                {
                    sql.Append(" AND NormalizedTimestamp >= @FromDate");
                    parameters.Add(new SqlParameter("@FromDate", fromDate.Value));
                }

                if (toDate.HasValue)
                {
                    sql.Append(" AND NormalizedTimestamp <= @ToDate");
                    parameters.Add(new SqlParameter("@ToDate", toDate.Value));
                }

                sql.Append(" ORDER BY NormalizedTimestamp, MetricType, MetricSubtype");

                using (var sqlCommand = new SqlCommand(sql.ToString(), sqlConnection))
                {
                    sqlCommand.Parameters.AddRange(parameters.ToArray());

                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var metric = new HealthMetric
                            {
                                MetricType = reader["MetricType"]?.ToString() ?? string.Empty,
                                MetricSubtype = reader["MetricSubtype"]?.ToString() ?? string.Empty,
                                Value = reader["Value"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Value"]) : null,
                                Unit = reader["Unit"]?.ToString() ?? string.Empty
                            };

                            if (reader["NormalizedTimestamp"] != DBNull.Value)
                            {
                                metric.NormalizedTimestamp = Convert.ToDateTime(reader["NormalizedTimestamp"]);
                            }

                            results.Add(metric);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving HealthMetricsWeek data: {ex.Message}");
            throw;
        }

        return results;
    }

    public static void InsertHealthMetricsWeek(
        string? metricType = null,
        string? metricSubtype = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool overwriteExisting = false)
    {
        ExecuteHealthMetricsAggregation(
            targetTable: "[dbo].[HealthMetricsWeek]",
            timeGroupingExpression: @"
            DATEADD(day, -(DATEPART(weekday, CAST(CAST(NormalizedTimestamp AS DATE) AS DATETIME2)) 
            + @@DATEFIRST - 2) % 7, CAST(CAST(NormalizedTimestamp AS DATE) AS DATETIME2))",
            timestampColumnAlias: "WeekStart",
            metricType, metricSubtype,
            fromDate, toDate,
            overwriteExisting);
    }

    public static void InsertHealthMetricsMonth(
        string? metricType = null,
        string? metricSubtype = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool overwriteExisting = false)
    {
        ExecuteHealthMetricsAggregation(
            targetTable: "[dbo].[HealthMetricsMonth]",
            timeGroupingExpression: "DATEADD(MONTH, DATEDIFF(MONTH, 0, NormalizedTimestamp), 0)",
            timestampColumnAlias: "MonthStart",
            metricType, metricSubtype,
            fromDate, toDate,
            overwriteExisting);
    }

    private static void ExecuteHealthMetricsAggregation(
    string targetTable,
    string timeGroupingExpression,
    string timestampColumnAlias,
    string? metricType,
    string? metricSubtype,
    DateTime? fromDate,
    DateTime? toDate,
    bool overwriteExisting)
    {
        var connectionString = ConfigurationManager.AppSettings["HealthDB"];

        try
        {
            using var sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();

            // -------------------------------------------------------
            // 1. Build INSERT Query
            // -------------------------------------------------------

            var sql = new StringBuilder($@"
            INSERT INTO {targetTable}
            (MetricType, MetricSubtype, NormalizedTimestamp, Value, Unit)
            SELECT 
                MetricType,
                ISNULL(MetricSubtype, '') AS MetricSubtype,
                {timeGroupingExpression} AS {timestampColumnAlias},
                AVG(Value) AS AvgValue,
                MAX(Unit) AS Unit
            FROM [dbo].[HealthMetrics]
            WHERE NormalizedTimestamp IS NOT NULL
              AND Value IS NOT NULL");

            var parameters = new List<SqlParameter>();

            AppendOptionalFilter(sql, parameters, "MetricType", metricType, "MetricType");
            AppendOptionalFilter(sql, parameters, "ISNULL(MetricSubtype, '')", metricSubtype, "MetricSubtype");
            AppendOptionalDateRange(sql, parameters, fromDate, toDate);

            sql.Append($@"
            GROUP BY 
                MetricType,
                ISNULL(MetricSubtype, ''),
                {timeGroupingExpression}");

            // -------------------------------------------------------
            // 2. Delete existing records if required
            // -------------------------------------------------------
            if (overwriteExisting)
            {
                var deleteSql = new StringBuilder($@"
                DELETE FROM {targetTable}
                WHERE 1=1");

                var deleteParams = new List<SqlParameter>();

                AppendOptionalFilter(deleteSql, deleteParams, "MetricType", metricType, "DeleteMetricType");
                AppendOptionalFilter(deleteSql, deleteParams, "ISNULL(MetricSubtype, '')", metricSubtype, "DeleteMetricSubtype");
                AppendOptionalDateRange(deleteSql, deleteParams, fromDate, toDate, prefix: "Delete");

                using var deleteCmd = new SqlCommand(deleteSql.ToString(), sqlConnection);
                deleteCmd.Parameters.AddRange(deleteParams.ToArray());
                deleteCmd.ExecuteNonQuery();
            }

            // -------------------------------------------------------
            // 3. Execute INSERT
            // -------------------------------------------------------
            using var insertCmd = new SqlCommand(sql.ToString(), sqlConnection);
            insertCmd.Parameters.AddRange(parameters.ToArray());
            insertCmd.CommandTimeout = 300;

            var rows = insertCmd.ExecuteNonQuery();
            Console.WriteLine($"Inserted {rows} aggregated records into {targetTable}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting aggregated health metrics into {targetTable}: {ex.Message}");
            throw;
        }
    }
    private static void AppendOptionalFilter(
    StringBuilder sql,
    List<SqlParameter> parameters,
    string columnExpression,
    string? value,
    string parameterName)
    {
        if (!string.IsNullOrEmpty(value))
        {
            sql.Append($" AND {columnExpression} = @{parameterName}");
            parameters.Add(new SqlParameter($"@{parameterName}", value));
        }
    }

    private static void AppendOptionalDateRange(
        StringBuilder sql,
        List<SqlParameter> parameters,
        DateTime? fromDate,
        DateTime? toDate,
        string prefix = "")
    {
        if (fromDate.HasValue && toDate.HasValue)
        {
            sql.Append($" AND NormalizedTimestamp >= @{prefix}FromDate AND NormalizedTimestamp <= @{prefix}ToDate");
            parameters.Add(new SqlParameter($"@{prefix}FromDate", fromDate.Value));
            parameters.Add(new SqlParameter($"@{prefix}ToDate", toDate.Value));
        }
        else if (fromDate.HasValue)
        {
            sql.Append($" AND NormalizedTimestamp >= @{prefix}FromDate");
            parameters.Add(new SqlParameter($"@{prefix}FromDate", fromDate.Value));
        }
        else if (toDate.HasValue)
        {
            sql.Append($" AND NormalizedTimestamp <= @{prefix}ToDate");
            parameters.Add(new SqlParameter($"@{prefix}ToDate", toDate.Value));
        }
    }







    /// <summary>
    /// Gets the date range (min and max timestamps) for a specific MetricType and MetricSubtype combination
    /// </summary>
    /// <param name="metricType">The MetricType to query</param>
    /// <param name="metricSubtype">The MetricSubtype to query (null or empty string for records without subtype)</param>
    /// <returns>Tuple with MinDate and MaxDate, or null if no records found</returns>
    public static (DateTime MinDate, DateTime MaxDate)? GetDateRangeForMetric(string metricType, string? metricSubtype = null)
    {
        var connectionString = ConfigurationManager.AppSettings["HealthDB"];

        try
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                var sql = new StringBuilder(@"
                    SELECT 
                        MIN(NormalizedTimestamp) AS MinDate,
                        MAX(NormalizedTimestamp) AS MaxDate
                    FROM [dbo].[HealthMetrics]
                    WHERE MetricType = @MetricType
                        AND NormalizedTimestamp IS NOT NULL");

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@MetricType", metricType)
                };

                if (!string.IsNullOrEmpty(metricSubtype))
                {
                    sql.Append(" AND ISNULL(MetricSubtype, '') = @MetricSubtype");
                    parameters.Add(new SqlParameter("@MetricSubtype", metricSubtype));
                }
                else
                {
                    sql.Append(" AND (MetricSubtype IS NULL OR MetricSubtype = '')");
                }

                using (var sqlCommand = new SqlCommand(sql.ToString(), sqlConnection))
                {
                    sqlCommand.Parameters.AddRange(parameters.ToArray());

                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (reader["MinDate"] != DBNull.Value && reader["MaxDate"] != DBNull.Value)
                            {
                                var minDate = Convert.ToDateTime(reader["MinDate"]);
                                var maxDate = Convert.ToDateTime(reader["MaxDate"]);
                                return (minDate, maxDate);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting date range for {metricType}/{metricSubtype ?? "null"}: {ex.Message}");
            throw;
        }

        return null;
    }
}













    //public static void InsertHealthMetricsMonth(
    //    string? metricType = null,
    //    string? metricSubtype = null,
    //    DateTime? fromDate = null,
    //    DateTime? toDate = null,
    //    bool overwriteExisting = false)
    //{
    //    var connectionString = ConfigurationManager.AppSettings["HealthDB"];

    //    try
    //    {
    //        using (var sqlConnection = new SqlConnection(connectionString))
    //        {
    //            sqlConnection.Open();

    //            // Build the aggregation query
    //            // Calculate month start (Monday) - works regardless of DATEFIRST setting
    //            // Formula: DATEADD(day, -(DATEPART(monthday, date) + @@DATEFIRST - 2) % 7, date)
    //            var sql = new StringBuilder(@"
    //                INSERT INTO [dbo].[HealthMetricsMonth]
    //                (MetricType, MetricSubtype, NormalizedTimestamp, Value, Unit)
    //                SELECT 
    //                    MetricType,
    //                    ISNULL(MetricSubtype, '') AS MetricSubtype,
    //                    -- Calculate the start of the month (Monday)
    //                    DATEADD(day, -(DATEPART(monthday, CAST(CAST(NormalizedTimestamp AS DATE) AS DATETIME2)) + @@DATEFIRST - 2) % 7, 
    //                            CAST(CAST(NormalizedTimestamp AS DATE) AS DATETIME2)) AS MonthStart,
    //                    AVG(Value) AS AvgValue,
    //                    MAX(Unit) AS Unit
    //                FROM [dbo].[HealthMetrics]
    //                WHERE NormalizedTimestamp IS NOT NULL
    //                    AND Value IS NOT NULL");

    //            var parameters = new List<SqlParameter>();

    //            if (!string.IsNullOrEmpty(metricType))
    //            {
    //                sql.Append(" AND MetricType = @MetricType");
    //                parameters.Add(new SqlParameter("@MetricType", metricType));
    //            }

    //            if (!string.IsNullOrEmpty(metricSubtype))
    //            {
    //                sql.Append(" AND ISNULL(MetricSubtype, '') = @MetricSubtype");
    //                parameters.Add(new SqlParameter("@MetricSubtype", metricSubtype));
    //            }

    //            if (fromDate.HasValue)
    //            {
    //                sql.Append(" AND NormalizedTimestamp >= @FromDate");
    //                parameters.Add(new SqlParameter("@FromDate", fromDate.Value));
    //            }

    //            if (toDate.HasValue)
    //            {
    //                sql.Append(" AND NormalizedTimestamp <= @ToDate");
    //                parameters.Add(new SqlParameter("@ToDate", toDate.Value));
    //            }

    //            sql.Append(@"
    //                GROUP BY 
    //                    MetricType,
    //                    ISNULL(MetricSubtype, ''),
    //                    DATEADD(day, -(DATEPART(monthday, CAST(CAST(NormalizedTimestamp AS DATE) AS DATETIME2)) + @@DATEFIRST - 2) % 7, 
    //                            CAST(CAST(NormalizedTimestamp AS DATE) AS DATETIME2))");

    //            // If overwriteExisting is true, delete existing records first
    //            if (overwriteExisting)
    //            {
    //                var deleteSql = new StringBuilder(@"
    //                    DELETE FROM [dbo].[HealthMetricsMonth]
    //                    WHERE 1=1");

    //                var deleteParameters = new List<SqlParameter>();

    //                if (!string.IsNullOrEmpty(metricType))
    //                {
    //                    deleteSql.Append(" AND MetricType = @DeleteMetricType");
    //                    deleteParameters.Add(new SqlParameter("@DeleteMetricType", metricType));
    //                }

    //                if (!string.IsNullOrEmpty(metricSubtype))
    //                {
    //                    deleteSql.Append(" AND ISNULL(MetricSubtype, '') = @DeleteMetricSubtype");
    //                    deleteParameters.Add(new SqlParameter("@DeleteMetricSubtype", metricSubtype));
    //                }

    //                if (fromDate.HasValue || toDate.HasValue)
    //                {
    //                    // Delete records in the date range
    //                    if (fromDate.HasValue && toDate.HasValue)
    //                    {
    //                        deleteSql.Append(" AND NormalizedTimestamp >= @DeleteFromDate AND NormalizedTimestamp <= @DeleteToDate");
    //                        deleteParameters.Add(new SqlParameter("@DeleteFromDate", fromDate.Value));
    //                        deleteParameters.Add(new SqlParameter("@DeleteToDate", toDate.Value));
    //                    }
    //                    else if (fromDate.HasValue)
    //                    {
    //                        deleteSql.Append(" AND NormalizedTimestamp >= @DeleteFromDate");
    //                        deleteParameters.Add(new SqlParameter("@DeleteFromDate", fromDate.Value));
    //                    }
    //                    else if (toDate.HasValue)
    //                    {
    //                        deleteSql.Append(" AND NormalizedTimestamp <= @DeleteToDate");
    //                        deleteParameters.Add(new SqlParameter("@DeleteToDate", toDate.Value));
    //                    }
    //                }

    //                using (var deleteCommand = new SqlCommand(deleteSql.ToString(), sqlConnection))
    //                {
    //                    deleteCommand.Parameters.AddRange(deleteParameters.ToArray());
    //                    deleteCommand.ExecuteNonQuery();
    //                }
    //            }

    //            // Execute the insert query
    //            using (var sqlCommand = new SqlCommand(sql.ToString(), sqlConnection))
    //            {
    //                sqlCommand.Parameters.AddRange(parameters.ToArray());
    //                sqlCommand.CommandTimeout = 300; // 5 minutes timeout for large aggregations
    //                var rowsAffected = sqlCommand.ExecuteNonQuery();
    //                Console.WriteLine($"Inserted {rowsAffected} monthly aggregated records into HealthMetricsMonth.");
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error inserting HealthMetricsMonth data: {ex.Message}");
    //        throw;
    //    }
    //}

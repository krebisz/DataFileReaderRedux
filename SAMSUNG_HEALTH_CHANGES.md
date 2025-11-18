# Changes Made for Samsung Health JSON Processing

## Summary

The application has been updated to properly process Samsung Health JSON files and store them in a standardized SQL Server database. Here's what changed:

## Key Changes

### 1. **Removed Problematic Character Removal** ✅

- **Issue**: The original code removed escape characters and whitespace from JSON, which broke parsing
- **Fix**: Removed `RemoveEscapeCharacters()` and `RemoveFaultyCharacterSequences()` calls for JSON files
- **Location**: `Program.cs` - JSON files are now read directly without modification

### 2. **Created Time Normalization Helper** ✅

- **New File**: `Helper/TimeNormalizationHelper.cs`
- **Purpose**: Converts Unix timestamps (milliseconds) to standardized DateTime UTC
- **Features**:
  - Handles Unix timestamps in milliseconds and seconds
  - Supports ISO 8601 date strings
  - Automatically detects timestamp format
  - Extracts timestamps from common field names (`start_time`, `end_time`, `timestamp`, etc.)

### 3. **Created Samsung Health Parser** ✅

- **New File**: `Helper/SamsungHealthParser.cs`
- **Purpose**: Extracts health metrics from Samsung Health JSON files
- **Features**:
  - Detects Samsung Health files by path
  - Extracts metric type from file path (e.g., `com.samsung.shealth.tracker.heart_rate` → `TrackerHeartRate`)
  - Parses arrays of measurement objects
  - Extracts all numeric values as separate metrics
  - Handles timestamps (start_time, end_time)
  - Determines units automatically (bpm, steps, kcal, etc.)
  - Stores additional fields as metadata

### 4. **Enhanced SQL Helper** ✅

- **File**: `Helper/SQLHelper.cs`
- **New Methods**:
  - `EnsureHealthMetricsTableExists()`: Creates standardized table schema
  - `InsertHealthMetrics()`: Inserts health metrics using parameterized queries
- **Schema**:
  ```sql
  HealthMetrics (
      Id, Provider, MetricType, SourceFile,
      NormalizedTimestamp, RawTimestamp, Value, Unit,
      Metadata (JSON), CreatedDate
  )
  ```
- **Indexes**: Created on Timestamp, Provider+MetricType, and SourceFile for performance

### 5. **Updated Main Program** ✅

- **File**: `Program.cs`
- **Changes**:
  - Detects Samsung Health files automatically
  - Routes Samsung files to specialized parser
  - Inserts metrics into database
  - Shows progress and statistics
  - Handles errors gracefully
  - Supports `MaxFilesToProcess` config for testing

### 6. **Updated Configuration** ✅

- **File**: `App.config`
- **Change**: RootDirectory now points to `C:\Documents\Personal\Health\Health Data\Samsung Health\jsons`
- **Note**: You can change this path to point to your actual data location

## How It Works

1. **File Discovery**: Recursively scans the configured directory for JSON files
2. **Provider Detection**: Checks if file path contains "Samsung Health" or "com.samsung"
3. **Parsing**:
   - For Samsung files: Uses `SamsungHealthParser` to extract metrics
   - For other files: Uses legacy processing (original implementation)
4. **Time Normalization**: Converts Unix timestamps (milliseconds) to DateTime UTC
5. **Database Storage**: Inserts metrics into standardized `HealthMetrics` table

## Example: Heart Rate File Processing

**Input JSON** (Samsung Health):

```json
[
  {
    "heart_rate": 67.0,
    "heart_rate_max": 70.0,
    "heart_rate_min": 65.0,
    "start_time": 1724032740000,
    "end_time": 1724032799000
  },
  ...
]
```

**Output** (Database Records):

- `Provider`: "Samsung"
- `MetricType`: "TrackerHeartRate_heart_rate"
- `NormalizedTimestamp`: 2024-08-19 12:59:00 (UTC)
- `Value`: 67.0
- `Unit`: "bpm"
- Plus separate records for `heart_rate_max` and `heart_rate_min`

## Database Schema

The `HealthMetrics` table structure:

| Column              | Type          | Description                                          |
| ------------------- | ------------- | ---------------------------------------------------- |
| Id                  | BIGINT        | Primary key (auto-increment)                         |
| Provider            | NVARCHAR(50)  | "Samsung", "Google", "Huawei", etc.                  |
| MetricType          | NVARCHAR(100) | Type of metric (e.g., "TrackerHeartRate_heart_rate") |
| SourceFile          | NVARCHAR(500) | Original file path                                   |
| NormalizedTimestamp | DATETIME2     | Standardized UTC datetime                            |
| RawTimestamp        | NVARCHAR(100) | Original timestamp value                             |
| Value               | DECIMAL(18,4) | Numeric measurement value                            |
| Unit                | NVARCHAR(50)  | Unit of measurement (bpm, steps, etc.)               |
| Metadata            | NVARCHAR(MAX) | Additional fields as JSON                            |
| CreatedDate         | DATETIME2     | Record creation timestamp                            |

## Configuration Options

In `App.config`, you can configure:

- **RootDirectory**: Path to your Samsung Health jsons folder
- **HealthDB**: SQL Server connection string
- **MaxFilesToProcess**: Limit files for testing (0 = all files)
- **VerboseLogging**: Enable detailed console output

## Next Steps

1. **Verify Database Connection**: Ensure your SQL Server is running and the connection string is correct
2. **Test with Small Dataset**: Set `MaxFilesToProcess` to 10-20 files first
3. **Run the Application**: Execute and monitor the console output
4. **Query Database**: Use SQL to verify data was inserted:
   ```sql
   SELECT TOP 100 * FROM HealthMetrics
   WHERE Provider = 'Samsung'
   ORDER BY NormalizedTimestamp DESC
   ```

## Notes

- The application now handles arrays of measurements properly (one row per measurement)
- Timestamps are normalized to UTC for consistent querying
- Each numeric field becomes a separate metric record (e.g., heart_rate, heart_rate_max, heart_rate_min)
- Additional fields are stored as JSON in the Metadata column
- The legacy processing is still available for non-Samsung files

## Troubleshooting

- **Database Connection Error**: Check your connection string in `App.config`
- **No Metrics Extracted**: Check console output for parsing errors
- **Performance**: For large datasets, consider batch inserts (future enhancement)

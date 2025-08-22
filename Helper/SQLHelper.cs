using DataFileReader.Class;
using System.Configuration;
using System.Data.SqlClient;

namespace DataFileReader.Helper;

public static class SQLHelper
{
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
}
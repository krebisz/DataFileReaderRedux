using DataFileReader.Class;
using System.Collections;
using System.Configuration;
using System.Data.SqlClient;

namespace DataFileReader.Helper;

public static class SQLHelper
{
    public static void CreateSQLTable(MetaData metaData)
    {
        var sqlConnection = new SqlConnection(ConfigurationManager.AppSettings["HealthDB"]);
        var sqlCommand = sqlConnection.CreateCommand();

        var sqlQuery = string.Empty;
        var tableName = string.Empty;

        if (!string.IsNullOrEmpty(metaData.Name))
            tableName = metaData.Name;
        else
            tableName = "ID_" + metaData.ID;

        DeleteSQLTable(tableName);

        sqlQuery = "CREATE TABLE [" + tableName + "](";

        try
        {
            var metaDataArray = new ArrayList();

            for (var i = 0; i < metaData.Fields.Count; i++)
            {
                metaDataArray.Add(metaData.Fields.ToArray()[i]);

                if (i > 0) sqlQuery += ",";

                sqlQuery += "[" + metaData.Fields.ToArray()[i].Key + "] VARCHAR(MAX) NULL";
            }

            sqlQuery += ")";

            sqlConnection.Open();
            sqlCommand = new SqlCommand(sqlQuery, sqlConnection);
            sqlCommand.ExecuteNonQuery();
            sqlConnection.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public static void UpdateSQLTable(MetaData metaData, string fileContent)
    {
        var sqlConnection = new SqlConnection(ConfigurationManager.AppSettings["HealthDB"]);
        var sqlCommand = sqlConnection.CreateCommand();

        var sqlQuery = string.Empty;

        try
        {
            string[] contentLines = fileContent.Split('\n');

            foreach (var contentLine in contentLines)
            {
                var line = contentLine.Replace("\r", string.Empty);
                //line = contentLine.Replace("\n", string.Empty);

                string[] fieldData = line.Split(',');

                if (metaData != null && fieldData.Length >= metaData.Fields.Count)
                {
                    var tableName = string.Empty;

                    if (!string.IsNullOrEmpty(metaData.Name))
                        tableName = metaData.Name;
                    else
                        tableName = "ID_" + Math.Abs(metaData.ID);

                    sqlQuery = "INSERT INTO " + tableName + "(";

                    for (var i = 0; i < metaData.Fields.Count; i++)
                    {
                        if (i > 0) sqlQuery += ",";

                        sqlQuery += "[" + metaData.Fields.ToArray()[i].Key + "]";
                    }

                    sqlQuery += ") VALUES (";

                    for (var i = 0; i < fieldData.Length; i++)
                    {
                        if (i > 0) sqlQuery += ",";

                        sqlQuery += "'" + fieldData[i] + "'";
                    }

                    sqlQuery += ")";

                    sqlConnection.Open();
                    sqlCommand = new SqlCommand(sqlQuery, sqlConnection);
                    sqlCommand.ExecuteNonQuery();
                    sqlConnection.Close();
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
        var sqlConnection = new SqlConnection(ConfigurationManager.AppSettings["HealthDB"]);
        var sqlCommand = sqlConnection.CreateCommand();

        var sqlQuery = string.Empty;

        sqlQuery = "DROP TABLE " + tableName;

        try
        {
            sqlConnection.Open();
            sqlCommand = new SqlCommand(sqlQuery, sqlConnection);
            sqlCommand.ExecuteNonQuery();
            sqlConnection.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error DNE: {ex.Message}");
        }
    }
}
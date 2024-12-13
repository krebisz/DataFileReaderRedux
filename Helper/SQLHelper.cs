using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataFileReader.Class;

namespace DataFileReader.Helper
{
    public static class SQLHelper
    {
        public static void CreateSQLTable(MetaData metaData)
        {
            SqlConnection sqlConnection = new SqlConnection("Data Source=(local);Initial Catalog=Health;Integrated Security=SSPI");
            SqlCommand sqlCommand = sqlConnection.CreateCommand();

            string sqlQuery = string.Empty;
            string tableName = string.Empty;

            if (!String.IsNullOrEmpty(metaData.Name))
            {
                tableName = metaData.Name;
            }
            else
            {
                tableName = "ID_" + metaData.ID.ToString();
            }

            DeleteSQLTable(tableName);

            sqlQuery = "CREATE TABLE [" + tableName + "](";

            try
            {
                ArrayList metaDataArray = new ArrayList();

                for (int i = 0; i < metaData.Fields.Count; i++)
                {
                    metaDataArray.Add(metaData.Fields.ToArray()[i]);

                    if (i > 0)
                    {
                        sqlQuery += ",";
                    }

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
            SqlConnection sqlConnection = new SqlConnection("Data Source=(local);Initial Catalog=Health;Integrated Security=SSPI");
            SqlCommand sqlCommand = sqlConnection.CreateCommand();

            string sqlQuery = string.Empty;

            try
            {
                string[] contentLines = fileContent.Split('\n');

                foreach (string contentLine in contentLines)
                {
                    string line = contentLine.Replace("\r", string.Empty);
                    //line = contentLine.Replace("\n", string.Empty);

                    string[] fieldData = line.Split(',');

                    if ((metaData != null) && (fieldData.Length >= metaData.Fields.Count))
                    {
                        string tableName = string.Empty;

                        if (!String.IsNullOrEmpty(metaData.Name))
                        {
                            tableName = metaData.Name;wr
                        }
                        else
                        {
                            tableName = "ID_" + Math.Abs(metaData.ID).ToString();
                        }

                        sqlQuery = "INSERT INTO " + tableName + "(";

                        for (int i = 0; i < metaData.Fields.Count; i++)
                        {
                            if (i > 0)
                            {
                                sqlQuery += ",";
                            }

                            sqlQuery += "[" + metaData.Fields.ToArray()[i].Key + "]";
                        }

                        sqlQuery += ") VALUES ("; 


                        for (int i = 0; i < fieldData.Length; i++)
                        {
                            if (i > 0)
                            {
                                sqlQuery += ",";
                            }

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
            SqlConnection sqlConnection = new SqlConnection("Data Source=(local);Initial Catalog=Health;Integrated Security=SSPI");
            SqlCommand sqlCommand = sqlConnection.CreateCommand();

            string sqlQuery = string.Empty;

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
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace SQLMethodsNameSpace
{ 
    public class SQLMethods
    {
        static string ConnectionString = "Data Source=pearltestdb.database.windows.net;" + "Initial Catalog=CheckItOutdb;" + "User id=ntahiri;" + "Password=Faisal123;";

        //Sends scraped data to database - yet to find a way to include product category
        public static void InsertProductRecord(string ProductName, string ProductCategory, string ProductPrice, string Store)
        {
            SqlConnection sqlConnection1 =
            new SqlConnection(ConnectionString);

            SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.Text;
            //cmd.CommandText = "INSERT INTO dbo.TestProductTable (ProductName, ProductCategory, ProductPrice, Store) VALUES ('Human', 'Homosapien', $4.00, 'earth.com/');";

            string stmt = "INSERT INTO dbo.TestProductTable(ProductName, ProductCategory, ProductPrice, Source, TimeGenerated) VALUES(@ProductName, @ProductCategory, @ProductPrice, @Source, @TimeGenerated)";

            //Using parameterized query to avoid injection hacking
            cmd = new SqlCommand(stmt, sqlConnection1);
            cmd.Parameters.Add("@ProductName", SqlDbType.VarChar, 250);
            cmd.Parameters.Add("@ProductCategory", SqlDbType.VarChar,100);
            cmd.Parameters.Add("@ProductPrice", SqlDbType.Money);
            cmd.Parameters.Add("@Source", SqlDbType.VarChar, 100);
            cmd.Parameters.Add("@TimeGenerated", SqlDbType.DateTime);

            cmd.Parameters["@ProductName"].Value = ProductName;
            cmd.Parameters["@ProductCategory"].Value = ProductCategory;
            cmd.Parameters["@ProductPrice"].Value = ProductPrice;
            cmd.Parameters["@Source"].Value = Store;
            cmd.Parameters["@TimeGenerated"].Value = DateTime.Now;

            cmd.Connection = sqlConnection1;
            sqlConnection1.Open();
            cmd.ExecuteNonQuery();
            sqlConnection1.Close();
        }

        //Drops data in the product table (before a new crawl, for instance)
        public static void DropProductTable()
        {
            SqlConnection sqlConnection1 =
            new SqlConnection(ConnectionString);

            string sqlTrunc = "TRUNCATE TABLE " + "TestProductTable"; 
            SqlCommand cmd = new SqlCommand(sqlTrunc, sqlConnection1);
            sqlConnection1.Open();
            cmd.ExecuteNonQuery();
            sqlConnection1.Close();
        }

        //GenericGroceryItems holds common grocery items one may search for, this function gets these items
        public static List<string> GetGenericProductNames()
        {
            List<string> columnData = new List<string>();

            SqlConnection sqlConnection1 =
            new SqlConnection(ConnectionString);

            SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.Text;

            sqlConnection1.Open();
            string query = "SELECT ProductCategory FROM GenericGroceryItems";
            using (SqlCommand command = new SqlCommand(query, sqlConnection1))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columnData.Add(reader.GetString(0));
                    }
                }
            }
            return columnData;
        }

     }
 }

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

            Console.WriteLine();
            Console.WriteLine("Inserted " + ProductName + ": " + ProductPrice + " as a record in database.");
            Console.WriteLine();

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

        public static List<Product> GetProductListings(string productCategory)
        {
            //List<string> columnData = new List<string>();
            string query = "SELECT * FROM dbo.TestProductTable WHERE ProductCategory = @ProductCategory";

            SqlConnection sqlConnection1 =
            new SqlConnection(ConnectionString);

            SqlCommand cmd = new SqlCommand();

            cmd = new SqlCommand(query, sqlConnection1);
            cmd.Parameters.Add("@ProductCategory", SqlDbType.VarChar, 100);
            cmd.Parameters["@ProductCategory"].Value = productCategory;

            cmd.CommandType = CommandType.Text;
            sqlConnection1.Open();

            List<Product> products = new List<Product>();

            using (cmd)
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Product prdct = new Product();

                        prdct.ProductName = reader.GetValue(0).ToString();
                        prdct.ProductCategory = reader.GetValue(1).ToString();
                        prdct.ProductPrice = double.Parse((reader.GetValue(2).ToString()));
                        prdct.Source = reader.GetValue(3).ToString();
                        prdct.TimeGenerated = DateTime.Parse(reader.GetValue(4).ToString());

                        products.Add(prdct);
                    }
                }
            }
            return products;
        }
    }
 }

public class Product
{

    public Product()
    {
        ProductName = "";
        ProductCategory = "";
        ProductPrice = 0;
        Source = "";
        TimeGenerated = DateTime.MinValue;
    }

    // Constructor that takes  arguments:
    public Product(string productName, string productCategory, double productPrice, string source, DateTime timeGenerated)
    {
        ProductName = productName;
        ProductCategory = productCategory;
        ProductPrice = productPrice;
        Source = source;
        TimeGenerated = timeGenerated;
    }

    public string ProductName
    {
        get;
        set;
    }
    public string ProductCategory
    {
        get;
        set;
    }

    public double ProductPrice
    {
        get;
        set;
    }

    public string Source
    {
        get;
        set;
    }

    public DateTime TimeGenerated
    {
        get;
        set;
    }
}

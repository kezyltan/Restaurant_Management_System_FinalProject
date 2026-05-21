using System;
using System.Data.SqlClient;

public class DatabaseHelper
{
    private static string connectionString = 
        @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\InventoryDB.mdf;Integrated Security=True";

    public static SqlConnection GetConnection()
    {
        try
        {
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            return conn;
        }
        catch (Exception ex)
        {
            throw new Exception("Database connection failed: " + ex.Message);
        }
    }
}
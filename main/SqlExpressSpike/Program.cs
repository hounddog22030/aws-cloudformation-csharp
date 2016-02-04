using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlExpressSpike
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.Write("Connection String:");
            var connectionString = Console.ReadLine();
            using (var c = new SqlConnection(connectionString))
            {
                c.Open();
                var command = c.CreateCommand();
                command.CommandText = "SELECT 0";
                var result = command.ExecuteScalar();
                Console.Write("Result:");
                Console.WriteLine(result.ToString());
                Console.ReadLine();
            }
        }
    }
}

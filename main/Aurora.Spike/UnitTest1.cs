using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;

namespace Aurora.Spike
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            //DataBase = DataBaseName;
            //; Database = mydbname
            using ( MySqlConnection c = new MySqlConnection("User id=masterusername;Password=Hy77tttt.;Host=asfphyqvz4in2q.cjb3mfdiqjpi.us-east-1.rds.amazonaws.com; Protocol=TCP; Port=3306; Compress=false; Pooling=true; Min Pool Size=0; Max Pool Size=100; Connection Lifetime=0"))
            {
                c.Open();
                var cc = c.CreateCommand();
                cc.CommandText = "SELECT 0";
                var result = cc.ExecuteScalar();
                Assert.AreEqual((Int64)0,result);

            }
            

        }
        [TestMethod]
        public void ConnectToSqlServer()
        {
            var builder = new System.Data.SqlClient.SqlConnectionStringBuilder();
            builder.DataSource = "sqlserver.alpha.yadayada.software.private";
            builder.UserID = "sqlserveruser";
            builder.Password = "Hy77tttt.";

            using (var c = new System.Data.SqlClient.SqlConnection(builder.ConnectionString))
            {
                c.Open();
                var cc = c.CreateCommand();
                cc.CommandText = "SELECT 0";
                var result = cc.ExecuteScalar();
                Assert.AreEqual(0, result);

            }


        }
    }
}

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
            using ( MySqlConnection c = new MySqlConnection("Host=mydb.cjb3mfdiqjpi.us-east-1.rds.amazonaws.com;Database=mydbname; Protocol=TCP; Port=3306; Compress=false; Pooling=true; Min Pool Size=0; Max Pool Size=100; Connection Lifetime=0; User id=MyDbUserName;Password=Odelay123."))
            {
                c.Open();
                var cc = c.CreateCommand();
                cc.CommandText = "SELECT 0";
                var result = cc.ExecuteScalar();
                Assert.AreEqual(0,result);

            }
            

        }
    }
}

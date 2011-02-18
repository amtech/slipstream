﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;


using Xunit;

using ObjectServer.Model;

namespace ObjectServer.Test
{


    public class ModelBaseTest
    {
        public ModelBaseTest()
        {
            ObjectServerStarter.Initialize();
        }

        [Fact]
        public void TestCrud()
        {
            var modelName = "test.test_object";
            var dbName = "objectserver";

            var proxy = new LocalService();
            var values = new Dictionary<string, object>
            {
                { "name", "sweet_name" },
                { "address", "my address" },
                { "field1", 123 },
                { "field2", 100 },
            };
            var id = (long)proxy.Execute(
                dbName, modelName, "Create", values);
            Assert.True(id > 0);

            var foundIds = (long[])proxy.Execute(
                dbName, modelName, "Search", "(equal name 'sweet_name')", 0, 100);
            Assert.Equal(1, foundIds.Length);
            Assert.Equal(id, foundIds[0]);

            var newValues = new Dictionary<string, object> {
                { "name", "changed_name" },
            };
            proxy.Execute(dbName, modelName, "Write", id, newValues);

            var ids = new long[] { id };
            var data = (Dictionary<string, object>[])proxy.Execute(dbName, modelName, "Read", ids, null);
            Console.WriteLine(data);
            Assert.Equal(1, data.Length);
            Assert.Equal("changed_name", data[0]["name"]);
            Assert.Equal(223, data[0]["field3"]); //检测函数字段的计算是否正确


            proxy.Execute(dbName, modelName, "Delete", ids);

            foundIds = (long[])proxy.Execute(
                dbName, modelName, "Search", "(equal name 'sweet_name')", 0, 100);
            Assert.Equal(0, foundIds.Length);
        }

        /*
        public void Main()
        {
            TcpChannel chan = new TcpChannel();
            ChannelServices.RegisterChannel(chan, false);
            ObjectProxy proxy = (ObjectProxy)Activator.GetObject(typeof(ObjectProxy)
                , "tcp://localhost:5000/ObjectProxy");

            var modelName = "Test.TestObject";

            var values = new Dictionary<string, object>()
            {
                { "name", "Oh my new record!" },
            };
            var id = (long)proxy.Execute("objectserver", modelName, "Create", new object[] { values });
            Console.WriteLine("id=[{0}]", id);

            var ids = new long[] { 1, 2, 17, 18 };
            var fields = new string[] { "name" };
            var args = new object[] { fields, ids };
            var dt = (Hashtable[])proxy.Execute("objectserver", modelName, "Read", args);

            foreach (Hashtable row in dt)
            {
                Console.WriteLine("resule: id=[{0}], name=[{1}]", row["id"], row["name"]);
            }

            Console.ReadLine();
        }
        */

    }
}

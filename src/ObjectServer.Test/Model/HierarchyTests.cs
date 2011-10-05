﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;

using NUnit.Framework;

using ObjectServer.Sql;
using ObjectServer.Model;

namespace ObjectServer.Model.Test
{
    [TestFixture]
    public class HierarchyTests : TransactionContextTestCaseBase
    {
        private static readonly string[] AssertNodeFields = { "name", "_left", "_right", "_children", "_descendants" };

        private void AssertNode(long id, long left, long right)
        {
            var catModel = this.GetResource("test.category");
            dynamic records = catModel.Read(this.TransactionContext, new object[] { id }, AssertNodeFields);
            Assert.AreEqual(1, records.Length);
            var record = records[0];
            Assert.AreEqual(left, (long)record["_left"]);
            Assert.AreEqual(right, (long)record["_right"]);
        }

        [Test]
        public void Test_create_nodes()
        {
            dynamic data = this.PrepareTestingData();
            dynamic catModel = this.GetResource("test.category");

            //确认 _left 与 _right 设置正确
            var fields = AssertNodeFields;

            AssertNode(data.id1, 1, 2);

            dynamic records = catModel.Read(this.TransactionContext, new object[] { data.id2 }, fields);
            dynamic record2 = records[0];
            Assert.AreEqual((long)3, record2["_left"]);
            Assert.AreEqual((long)10, record2["_right"]);
            var children2 = (long[])record2["_children"];
            Array.Sort(children2);
            Assert.AreEqual(2, children2.Length);
            Assert.AreEqual(data.id3, children2[0]);
            Assert.AreEqual(data.id4, children2[1]);
            var descendants2 = (long[])record2["_descendants"];
            Array.Sort(descendants2);
            Assert.AreEqual(3, descendants2.Length);

            AssertNode(data.id3, 4, 7);
            AssertNode(data.id4, 8, 9);
            AssertNode(data.id5, 5, 6);
        }

        [Test]
        public void Test_delete_nodes()
        {
            dynamic data = this.PrepareTestingData();
            dynamic catModel = this.GetResource("test.category");

            catModel.Delete(this.TransactionContext, new long[] { data.id3, data.id1 });

            dynamic ids = catModel.Search(this.TransactionContext, null, null, 0, 0);
            Assert.AreEqual(2, ids.Length);
            Array.Sort<long>(ids);
            Assert.AreEqual(data.id2, ids[0]);
            Assert.AreEqual(data.id4, ids[1]);

            AssertNode(data.id2, 1, 4);
            AssertNode(data.id4, 2, 3);
        }

        [Test]
        public void Test_change_parent1()
        {
            dynamic data = this.PrepareTestingData();
            dynamic catModel = this.GetResource("test.category");

            //把node5 的父节点改成 node2
            dynamic record = new ExpandoObject();
            record.parent = data.id2;
            catModel.Write(this.TransactionContext, data.id5, record);

            var ids = (long[])catModel.Search(this.TransactionContext, null, null, 0, 0);
            Assert.AreEqual(5, ids.Length);

            var fields = new string[] { "_id", "_left", "_right", "name", "parent" };
            var records = (Dictionary<string, object>[])catModel.Read(this.TransactionContext, ids, fields);
            var node5 = records.First(e => (string)e["name"] == "node5");
            Assert.AreEqual(data.id2, ((object[])node5["parent"])[0]);
            Assert.AreEqual((long)8, node5["_left"]);
            Assert.AreEqual((long)9, node5["_right"]);
        }

        [Test]
        public void Test_change_parent2()
        {
            dynamic data = this.PrepareTestingData();
            dynamic catModel = this.GetResource("test.category");

            //把node3 的父节点改成 node1
            dynamic record = new ExpandoObject();
            record.parent = data.id1;
            catModel.Write(this.TransactionContext, data.id3, record);

            dynamic ids = catModel.Search(this.TransactionContext, null, null, 0, 0);
            Assert.AreEqual(5, ids.Length);

            var fields = new string[] { "_id", "_left", "_right", "name", "parent" };
            var records = (Dictionary<string, object>[])catModel.Read(this.TransactionContext, ids, fields);
            var node3 = records.First(e => (string)e["name"] == "node3");
            Assert.AreEqual(data.id1, ((object[])node3["parent"])[0]);
            Assert.AreEqual((long)2, node3["_left"]);
            Assert.AreEqual((long)5, node3["_right"]);

            var node5 = records.First(e => (string)e["name"] == "node5");
            Assert.AreEqual(data.id3, ((object[])node5["parent"])[0]);
            Assert.AreEqual((long)3, node5["_left"]);
            Assert.AreEqual((long)4, node5["_right"]);
        }

        [Test]
        public void Test_childof()
        {
            dynamic catModel = this.GetResource("test.category");
            dynamic data = this.PrepareTestingData();

            var domain1 = new object[][] 
            { 
                new object[] { "_id", "childof", data.id2 }
            };

            dynamic ids1 = catModel.Search(
                this.TransactionContext, domain1, new object[] { new object[] { "_id", "ASC" } }, 0, 0);

            Assert.AreEqual(3, ids1.Length);
            Assert.AreEqual(data.id3, ids1[0]);
            Assert.AreEqual(data.id4, ids1[1]);
            Assert.AreEqual(data.id5, ids1[2]);

            var domain2 = new object[][] 
            { 
                new object[] { "_id", "childof", data.id3 }
            };

            dynamic ids2 = catModel.Search(
                this.TransactionContext, domain2, new object[] { new object[] { "_id", "ASC" } }, 0, 0);

            Assert.AreEqual(1, ids2.Length);
            Assert.AreEqual(data.id5, ids2[0]);
        }


        private dynamic PrepareTestingData()
        {
            dynamic catModel = this.GetResource("test.category");
            this.ClearModel("test.category");

            dynamic data = new ExpandoObject();
            //1
            //
            //2
            //|....3
            //      |....5
            //
            //|....4
            //插入4个根节点，1，2作为根节点，3,4是2的子节点，5 是3的子节点
            data.node1 = new ExpandoObject();
            data.node1.name = "node1";
            data.id1 = catModel.Create(this.TransactionContext, data.node1);

            data.node2 = new ExpandoObject();
            data.node2.name = "node2";
            data.id2 = catModel.Create(this.TransactionContext, data.node2);

            //插入节点3的时候节点2还是叶子
            data.node3 = new ExpandoObject();
            data.node3.name = "node3";
            data.node3.parent = data.id2;
            data.id3 = catModel.Create(this.TransactionContext, data.node3);

            data.node4 = new ExpandoObject();
            data.node4.name = "node4";
            data.node4.parent = data.id2;
            data.id4 = catModel.Create(this.TransactionContext, data.node4);

            data.node5 = new ExpandoObject();
            data.node5.name = "node5";
            data.node5.parent = data.id3;
            data.id5 = catModel.Create(this.TransactionContext, data.node5);

            return data;
        }

    }
}

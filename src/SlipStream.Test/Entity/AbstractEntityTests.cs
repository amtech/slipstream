﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Data;
using System.Dynamic;
using System.Data.Common;

using NUnit.Framework;

using SlipStream.Entity;
using SlipStream.Core;

namespace SlipStream.Entity.Test
{

    [TestFixture(Category = "ORM")]
    public class AbstractModelTests : ServiceContextTestCaseBase
    {
        [Test]
        public void ShouldHandleWithBadConstraints()
        {
            dynamic model = this.GetResource(MetaEntityEntity.EntityName);
            var constraints = new object[] {
                new object[] { "kk", "=", 13 },
            };

            Assert.Throws<ArgumentException>(delegate
            {
                var ids = model.Search(constraints, null, 0, 0);
            });

            Assert.Throws<ArgumentException>(delegate
            {
                var ids = model.Count(constraints);
            });

        }

        [Test]
        public void CanGetFields()
        {
            var entityName = "core.user";
            dynamic userEntity = this.GetResource(entityName);
            var result = userEntity.GetFields(null);
            var records = ((object[])result).Select(i => (Dictionary<string, object>)i);

            Assert.IsTrue(records.Any());
        }

        [Test]
        public void CheckVersionIncrement()
        {
            dynamic masterModel = this.GetResource("test.master");
            this.ClearEntity("test.master");

            dynamic m1 = new ExpandoObject();
            m1.name = "master1";

            //创建第一笔记录
            var id1 = masterModel.Create(m1);
            var record1 = this.GetMasterRecordByName("master1");

            Assert.That(record1.ContainsKey(AbstractEntity.VersionFieldName));
            Assert.AreEqual(0, record1[AbstractEntity.VersionFieldName]);

            //修改
            var name11 = "master1'1";
            record1["name"] = name11;
            masterModel.Write(record1[AbstractEntity.IdFieldName], record1);
            record1 = this.GetMasterRecordByName(name11);

            Assert.That(record1.ContainsKey(AbstractEntity.VersionFieldName));
            Assert.AreEqual(1, record1[AbstractEntity.VersionFieldName]);

            //错误的版本号
            long ver = (long)record1[AbstractEntity.VersionFieldName];
            ver--;

            record1[AbstractEntity.VersionFieldName] = ver;
            Assert.Throws<Exceptions.ConcurrencyException>(delegate
            {
                masterModel.Write(record1[AbstractEntity.IdFieldName], record1);
            });
        }

        [Test]
        public void CanHandleCreationWithUnexistedColumn()
        {
            dynamic masterModel = this.GetResource("test.master");
            this.ClearEntity("test.master");

            dynamic m1 = new ExpandoObject();
            m1.name = "master1";
            m1.age = 1;

            Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                masterModel.Create(m1);
            });
        }

        [Test]
        public void CanHandleWritingWithUnexistedColumn()
        {
            dynamic masterModel = this.GetResource("test.master");
            this.ClearEntity("test.master");

            dynamic m1 = new ExpandoObject();
            m1.name = "master1";
            long id = (long)masterModel.Create(m1);
            dynamic records = masterModel.Read(new long[] { id }, null);

            dynamic m2 = new ExpandoObject();
            m2._version = records[0]["_version"];
            m2.age = 12;

            Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                masterModel.Write(id, m2);
            });
        }

        [Test]
        public void CheckGetFieldDefaultValues()
        {
            dynamic testModel = this.GetResource("test.test_entity");
            var booleanFieldName = "boolean_field";
            var fields = new string[] { booleanFieldName };

            var defaultValues = testModel.GetFieldDefaultValues(fields);

            Assert.AreEqual(1, defaultValues.Count);
            Assert.AreEqual(true, defaultValues[booleanFieldName]);
        }

        private Dictionary<string, object> GetMasterRecordByName(string name)
        {
            var fields = new string[] { "name", AbstractEntity.VersionFieldName };
            dynamic masterModel = this.GetResource("test.master");
            var constraint = new object[][] {
                new object[] { "name", "=", name }
            };
            long[] ids = (long[])masterModel.Search(constraint, null, 0, 0);
            var records = (Dictionary<string, object>[])masterModel.Read(ids, fields);
            return records.First();
        }
    }
}

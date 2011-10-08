﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;

using NHibernate.SqlCommand;

using ObjectServer.Model;

namespace ObjectServer.Core
{
    /// <summary>
    /// 字段访问控制列表
    /// </summary>
    [Resource]
    public sealed class FieldAccessModel : AbstractSqlModel
    {
        public const string ModelName = "core.field_access";

        private static readonly SqlString SqlToQuery = SqlString.Parse(@"
SELECT DISTINCT ma._id, ma.allow_create, ma.allow_read, ma.allow_write, ma.allow_delete
    FROM core_model_access ma
    INNER JOIN core_model m ON m._id=ma.model
    INNER JOIN core_user_role_rel urr ON urr.role=ma.role
    WHERE (urr.user=?) AND (m.name=?)
");

        public FieldAccessModel()
            : base(ModelName)
        {
            Fields.Chars("name").SetLabel("Name");
            Fields.ManyToOne("role", "core.role")
                .OnDelete(OnDeleteAction.Cascade).SetLabel("Role");
            Fields.ManyToOne("field", "core.field").Required()
                .OnDelete(OnDeleteAction.Cascade).SetLabel("Field");
            Fields.Boolean("allow_read").SetLabel("Allow Reading")
                .Required().SetDefaultValueGetter(s => true);
            Fields.Boolean("allow_write").SetLabel("Allow Writing")
                .Required().SetDefaultValueGetter(s => true);
        }

        /// <summary>
        /// TODO 更新缓存
        /// </summary>
        /// <param name="tc"></param>
        /// <param name="userRecord"></param>
        /// <returns></returns>
        public override long CreateInternal(
            ITransactionContext tc, IDictionary<string, object> userRecord)
        {
            return base.CreateInternal(tc, userRecord);
        }

        /// <summary>
        /// TODO 更新缓存
        /// </summary>
        /// <param name="tc"></param>
        /// <param name="id"></param>
        /// <param name="userRecord"></param>
        public override void WriteInternal(
            ITransactionContext tc, long id, IDictionary<string, object> userRecord)
        {
            base.WriteInternal(tc, id, userRecord);
        }

        public IDictionary<string, bool> GetFieldAccess(
            ITransactionContext tc, string modelName, IEnumerable<string> fields, string mode)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                throw new ArgumentNullException("modelName");
            }
            if (mode != "read" && modelName != "write")
            {
                throw new ArgumentOutOfRangeException("model");
            }

            var modelModel = (IModel)tc.GetResource("core.model");
            var fieldModel = (IModel)tc.GetResource("core.field");
            var userRoleRelModel = (IModel)tc.GetResource("core.user_role");
            var sql = String.Format(CultureInfo.InvariantCulture,
                "select f.name, max(case when a.allow_{0} then 1 else 0 end) > 0 " +
                "from {1} a " +
                "inner join {2} f on (a.field = f._id) " +
                "inner join {3} m on (f.model = m._id) " +
                "left join {4} ur on (ur.role = a.role) " +
                "where m.name = ? and (ur.user = ? or a.role is null) " +
                "group by f.name ",
                mode, this.TableName, fieldModel.TableName, modelModel.TableName, userRoleRelModel.TableName);

            Debug.Assert(tc.Session != null);
            var userId = tc.Session.UserId;
            var records = tc.DBContext.QueryAsDictionary(SqlString.Parse(sql), modelName, userId);
            var result = records.First();
            return result.ToDictionary(p => p.Key, p => (bool)p.Value);
        }

    }
}

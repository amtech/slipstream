﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using ObjectServer.Model;
using ObjectServer.Utility;
using ObjectServer.Data;

namespace ObjectServer.Core
{
    [Resource]
    public sealed class UserGroupModel : AbstractTableModel
    {
        private const string UniqueConstraintName = "unique_core_user_group";

        public UserGroupModel()
            : base("core.user_group")
        {
            this.TableName = "core_user_group_rel";

            Fields.ManyToOne("uid", "core.user").SetLabel("User").Required();
            Fields.ManyToOne("gid", "core.group").SetLabel("Group").Required();

        }

        public override void Load(IDBProfile db)
        {
            base.Load(db);

            var tableCtx = db.DBContext.CreateTableContext(this.TableName);

            if (!tableCtx.ConstraintExists(db.DBContext, UniqueConstraintName))
            {
                tableCtx.AddConstraint(db.DBContext, UniqueConstraintName, "UNIQUE(gid, uid)");
            }
        }
    }
}

﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using ObjectServer.Model;

namespace ObjectServer.Test
{

    [Resource]
    public sealed class FunctionalFieldObject : AbstractTableModel
    {
        public const string ModelName = "test.functional_field_object";

        public FunctionalFieldObject()
            : base(ModelName)
        {
            Fields.Chars("name").SetLabel("Name").Required().SetSize(64);
            Fields.ManyToOne("user", "core.user").ValueGetter(GetUser);
        }

        private Dictionary<long, object> GetUser(IServiceScope ctx, IEnumerable<long> ids)
        {
            var userModel = (IModel)ctx.GetResource("core.user");
            var constraints = new object[][] { new object[] { "login", "=", "root" } };
            var userIds = Search(userModel, ctx, constraints, null, 0, 0);
            var rootId = userIds[0];
            var result = new Dictionary<long, object>();
            foreach (var id in ids)
            {
                result[(long)id] = new object[2] { rootId, "root" };
            }

            return result;
        }
    }



}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;

namespace ObjectServer.Model
{
    public interface IMetaModel : IResource
    {
        ICollection<InheritanceInfo> Inheritances { get; }
        IMetaFieldCollection Fields { get; }

        string TableName { get; }
        bool Hierarchy { get; }
        bool CanCreate { get; }
        bool CanRead { get; }
        bool CanWrite { get; }
        bool CanDelete { get; }

        bool LogCreation { get; }
        bool LogWriting { get; }

        NameGetter NameGetter { get; }

        bool AutoMigration { get; }

        IMetaField[] GetAllStorableFields();

        long[] SearchInternal(IResourceScope ctx, object[] domain = null, long offset = 0, long limit = 0);
        long CreateInternal(IResourceScope ctx, IDictionary<string, object> propertyBag);
        void WriteInternal(IResourceScope ctx, long id, IDictionary<string, object> record);
        Dictionary<string, object>[] ReadInternal(
            IResourceScope ctx, IEnumerable<long> ids, IEnumerable<string> fields = null);
        void DeleteInternal(IResourceScope ctx, IEnumerable<long> ids);

        dynamic Browse(IResourceScope ctx, long id);
    }
}

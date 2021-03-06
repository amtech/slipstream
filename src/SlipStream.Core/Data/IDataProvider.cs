﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NHibernate.Dialect;
using NHibernate.Driver;

namespace SlipStream.Data
{
    public interface IDataProvider
    {
        IDataContext OpenDataContext();
        IDataContext OpenDataContext(string dbName);

        string[] ListDatabases();
        void CreateDatabase(string dbName);
        void DeleteDatabase(string dbName);

        Dialect Dialect { get; }
        IDriver Driver { get; }
        bool IsSupportProcedure { get; }
    }
}

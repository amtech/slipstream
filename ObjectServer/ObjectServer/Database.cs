﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ObjectServer.Backend;

namespace ObjectServer
{
    public class Database : IDatabase
    {
        public Database(string dbName)
        {
            this.DataContext = DataProvider.CreateDataContext(dbName);
            this.Objects = new ObjectCollection(this);
        }

        ~Database()
        {
            this.Dispose(false);
        }

        public IDataContext DataContext { get; private set; }

        public IObjectCollection Objects { get; private set; }

        #region IDisposable 成员

        public void Dispose()
        {
            this.Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                //这里处理托管对象
            }

            this.DataContext.Dispose();
        }

        #endregion

        #region IGlobalObject 成员

        public void Initialize(Config cfg)
        {
            this.Objects.Initialize();
        }

        #endregion
    }
}

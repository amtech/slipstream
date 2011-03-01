﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ObjectServer.Backend;

namespace ObjectServer
{
    public interface IDatabase : IDisposable, IGlobalObject
    {
        IObjectCollection Objects { get; }
        IDataContext DataContext { get; }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectServer.Model
{
    public delegate IDictionary<long, string>
        NameGetter(ISession session, object[] ids);
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectServer.Model
{
    public delegate object
       FieldDefaultValueGetter(ITransactionContext ctx);
}

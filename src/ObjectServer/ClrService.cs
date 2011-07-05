﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace ObjectServer
{
    internal class ClrService : IService
    {
        public ClrService(IResource res, string name, MethodInfo mi)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(mi != null);

            this.Name = name;
            this.Method = mi;
            this.Resource = res;
        }

        #region IService Members

        public object Invoke(IResource self, IServiceScope scope, params object[] parameters)
        {
            Debug.Assert(self != null);
            Debug.Assert(scope != null);

            var userParamCount = parameters == null ? 0 : parameters.Length;
            var args = new object[userParamCount + 2];
            args[0] = self;
            args[1] = scope;
            parameters.CopyTo(args, 2);
            return this.Method.Invoke(self, args);
        }

        public IResource Resource { get; private set; }

        public string Name { get; private set; }

        public string Help { get; set; }

        #endregion

        public MethodInfo Method { get; private set; }
    }
}

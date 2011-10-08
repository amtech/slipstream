﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using ObjectServer.Core;

namespace ObjectServer.Model
{
    internal static class ModelSecurityExtensions
    {

        public static bool CanCreateModel(this ITransactionContext scope, string model)
        {
            return scope.IsDeniedModelServiceMethod(model, "create");
        }

        public static bool CanReadModel(this ITransactionContext scope, string model)
        {
            return scope.IsDeniedModelServiceMethod(model, "read");
        }

        public static bool CanWriteModel(this ITransactionContext scope, string model)
        {
            return scope.IsDeniedModelServiceMethod(model, "write");
        }

        public static bool CanDeleteModel(this ITransactionContext scope, string model)
        {
            return scope.IsDeniedModelServiceMethod(model, "delete");
        }

        private static bool IsDeniedModelServiceMethod(
            this ITransactionContext scope, string model, string action)
        {
            Debug.Assert(scope != null);
            Debug.Assert(!string.IsNullOrEmpty(model));
            Debug.Assert(!string.IsNullOrEmpty(action));

            if (!scope.Session.IsSystemUser)
            {
                return ModelAccessModel.CheckForCurrentUser(scope, model, action);
            }
            else
            {
                return true;
            }
        }
    }
}

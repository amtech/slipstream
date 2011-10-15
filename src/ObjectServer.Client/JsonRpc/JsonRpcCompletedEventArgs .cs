﻿using System;
using System.ComponentModel;
using System.Diagnostics;

namespace ObjectServer.Client.JsonRpc
{
    public sealed class JsonRpcCompletedEventArgs : AsyncCompletedEventArgs
    {
        public JsonRpcCompletedEventArgs(object result, Exception error, object userState)
            : base(error, false, userState)
        {
            Debug.Assert(result == null && error == null);

            this.Result = result;
        }

        public object Result { get; private set; }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using System.IO;
using System.Text;

using Newtonsoft.Json;

using ObjectServer.Json;

namespace ObjectServer.Web
{
    public abstract class JsonRpcHandler : IHttpHandler
    {
        private Dictionary<string, MethodInfo> methods
            = new Dictionary<string, MethodInfo>();

        protected JsonRpcHandler()
        {
            this.RegisterRpcMethods(typeof(JsonRpcHandler));
            var sublcassType = this.GetType();
            this.RegisterRpcMethods(sublcassType);
        }

        private void RegisterRpcMethods(Type type)
        {
            var methods = type.GetMethods();
            foreach (var mi in methods)
            {
                var attrs = mi.GetCustomAttributes(typeof(JsonRpcMethodAttribute), false);
                if (attrs.Length == 1)
                {
                    var attr = (JsonRpcMethodAttribute)attrs[0];

                    var name = attr.Name;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = mi.Name;
                    }

                    this.methods[name] = mi;
                }
            }
        }

        #region JSON-RPC system methods

        [JsonRpcMethod(Name = "system.listMethods")]
        public string[] ListMethods()
        {
            return this.methods.Keys.ToArray();
        }

        #endregion


        #region IHttpHandler 成员

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/json";
            var httpMethod = context.Request.HttpMethod.Trim().ToUpperInvariant();

            //RPC 调用
            if (httpMethod == "POST")
            {
                this.ProcessNormalRpc(context);
            }
        }

        private void ProcessNormalRpc(HttpContext context)
        {
            var ins = context.Request.InputStream;
            var jreq = (Dictionary<string, object>)PlainJsonConvert.Deserialize(ins);

            //执行调用
            var id = jreq["id"];
            var methodName = (string)jreq["method"];
            var method = this.methods[methodName];
            string error = null;
            object result = null;

            var args = (object[])jreq["params"];

            try
            {
                result = method.Invoke(this, args);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            var jresponse = new JsonRpcResponse()
            {
                Id = id,
                Error = error,
                Result = result
            };

            var js = new JsonSerializer();
            js.Serialize(context.Response.Output, jresponse);
        }

        #endregion
    }
}

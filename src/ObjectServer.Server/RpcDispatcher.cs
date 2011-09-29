﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

using ZMQ;
using ZMQ.ZMQExt;
using Newtonsoft.Json;

using ObjectServer.Utility;
using ObjectServer.Exceptions;
using ObjectServer.Json;

namespace ObjectServer.Server
{
    /// <summary>
    /// 处理 JSON-RPC 的 分发器
    /// </summary>
    public static class RpcDispatcher
    {
        private static readonly Dictionary<string, MethodInfo> s_methods = new Dictionary<string, MethodInfo>();
        private static readonly IExportedService s_service = Environment.ExportedService;
        private static readonly object lockObj = new object();
        private static bool running = false;

        static RpcDispatcher()
        {
            //注册自己的所有方法
            var selfType = typeof(RpcDispatcher);
            s_methods.Add("system.echo", selfType.GetMethod("Echo"));
            s_methods.Add("system.listMethods", selfType.GetMethod("ListMethods"));

            s_methods.Add("logOn", selfType.GetMethod("LogOn"));
            s_methods.Add("logOff", selfType.GetMethod("LogOff"));
            s_methods.Add("getVersion", selfType.GetMethod("GetVersion"));
            s_methods.Add("listDatabases", selfType.GetMethod("ListDatabases"));
            s_methods.Add("createDatabase", selfType.GetMethod("CreateDatabase"));
            s_methods.Add("deleteDatabase", selfType.GetMethod("DeleteDatabase"));
            s_methods.Add("execute", selfType.GetMethod("Execute"));
        }

        #region JSON-RPC system methods

        public static string[] ListMethods()
        {
            return s_methods.Keys.ToArray();
        }

        public static object Echo(object value)
        {
            return value;
        }

        #endregion

        #region 业务 JSON-RPC 方法

        public static string LogOn(string dbName, string userName, string password)
        {
            return s_service.LogOn(dbName, userName, password);
        }

        public static void LogOff(string sessionId)
        {
            s_service.LogOff(sessionId);
        }

        public static string GetVersion()
        {
            return s_service.GetVersion();
        }

        public static string[] ListDatabases()
        {
            return s_service.ListDatabases();
        }

        public static void CreateDatabase(string hashedRootPassword, string dbName, string adminPassword)
        {
            s_service.CreateDatabase(hashedRootPassword, dbName, adminPassword);
        }

        public static void DeleteDatabase(string hashedRootPassword, string dbName)
        {
            s_service.DeleteDatabase(hashedRootPassword, dbName);
        }

        public static object Execute(string sessionId, string objectName, string method, object[] parameters)
        {
            return s_service.Execute(sessionId, objectName, method, parameters);
        }

        #endregion

        public static void ProcessingLoop()
        {
            if (!Environment.Initialized)
            {
                throw new InvalidOperationException("无法启动 PRC-Handler 工人线程，请先初始化框架");
            }

            var controllerUrl = Environment.Configuration.CommanderUrl;
            var rpcHandlerUrl = Environment.Configuration.RpcHandlerUrl;
            var id = Guid.NewGuid();
            LoggerProvider.EnvironmentLogger.Debug(
                () => string.Format("Starting RpcHandler thread[{0}] URL=[{1}] ...", id, rpcHandlerUrl));


            using (var controller = new ZMQ.Socket(ZMQ.SocketType.SUB))
            using (var receiver = new ZMQ.Socket(ZMQ.SocketType.REP))
            {
                controller.Connect(controllerUrl);
                controller.Subscribe("STOP", Encoding.UTF8);
                LoggerProvider.EnvironmentLogger.Debug(
                    () => string.Format("RpcHandler thread[{0}] is connected to the Commander URL[{1}]", id, controllerUrl));

                receiver.Connect(rpcHandlerUrl);

                var items = new PollItem[2];
                items[0] = controller.CreatePollItem(IOMultiPlex.POLLIN);
                items[0].PollInHandler += new PollHandler(ControllerPollInHandler);

                items[1] = receiver.CreatePollItem(IOMultiPlex.POLLIN);
                items[1].PollInHandler += new PollHandler(ReceiverPollInHandler);

                lock (lockObj)
                {
                    running = true;
                }

                //  Process messages from both sockets
                while (running)
                {
                    Context.Poller(items, -1);
                }

                LoggerProvider.EnvironmentLogger.Debug(
                    () => string.Format("The RpcHandler thread[{0}] is stopped", id));
            }
        }

        private static void ControllerPollInHandler(Socket socket, IOMultiPlex revents)
        {
            var cmd = socket.Recv(Encoding.UTF8);

            if (cmd == "STOP" && running)
            {
                LoggerProvider.EnvironmentLogger.Info("The [STOP] command received, try to stop all RPC-Handlers");
                lock (lockObj)
                {
                    running = false;
                }
            }
        }

        private static void ReceiverPollInHandler(Socket socket, IOMultiPlex revents)
        {
            //TODO 优化，避免转换
            var message = socket.Recv();
            var result = DoJsonRpc(message);
            socket.Send(result);
        }

        private static byte[] DoJsonRpc(byte[] json)
        {
            Debug.Assert(json != null);

            var jreq = (Dictionary<string, object>)PlainJsonConvert.Parse(json);
            //TODO 检查 jreq 格式

            //执行调用
            var id = jreq[JsonRpcProtocol.Id];
            var methodName = (string)jreq[JsonRpcProtocol.Method];
            var method = s_methods[methodName];

            if (method == null)
            {
                throw new InvalidOperationException("Invalid method: " + methodName);
            }

            JsonRpcError error = null;
            object result = null;

            var args = (object[])jreq[JsonRpcProtocol.Params];

            LoggerProvider.RpcLogger.Debug(() =>
                string.Format("JSON-RPC: method=[{0}], params=[{1}]", methodName, args));

            try
            {
                var startTime = Stopwatch.GetTimestamp();

                try
                {
                    result = method.Invoke(null, args);
                }
                catch (TargetInvocationException tiex)
                {
                    throw tiex.InnerException;
                }
                catch
                {
                    throw;
                }

                var endTime = Stopwatch.GetTimestamp();
                var costTime = endTime - startTime;
                LoggerProvider.RpcLogger.Debug(
                    () => String.Format("RPC cost time: [{0:N0}ms]", costTime * 1000 / Stopwatch.Frequency));
            }
            catch (FatalException fex)
            {
                LoggerProvider.EnvironmentLogger.Fatal("系统发生了致命错误", fex);
                throw fex; //接着抛出异常，让系统结束运行
            }
            catch (ArgumentException aex)
            {
                error = JsonRpcError.RpcArgumentError;
                LoggerProvider.EnvironmentLogger.Error("ArgumentException", aex);
            }
            catch (ValidationException vex)
            {
                error = JsonRpcError.ValidationError;
                LoggerProvider.EnvironmentLogger.Error("ValidationException", vex);
            }
            catch (SecurityException sex)
            {
                error = JsonRpcError.LogginError;
                LoggerProvider.EnvironmentLogger.Error("安全异常", sex);
            }
            catch (ResourceNotFoundException ex)
            {
                error = JsonRpcError.ResourceNotFound;
                LoggerProvider.EnvironmentLogger.Error("ResourceNotFoundException", ex);
            }
            catch (RecordNotFoundException rnfex)
            {
                error = JsonRpcError.ResourceNotFound;
                LoggerProvider.EnvironmentLogger.Error("ResourceNotFoundException", rnfex);
            }
            catch (DataException ex)
            {
                error = JsonRpcError.BadData;
                LoggerProvider.EnvironmentLogger.Error("BadData", ex);
            }
            catch (System.Exception ex)
            {
                error = JsonRpcError.ServerInternalError;
                LoggerProvider.EnvironmentLogger.Error("RPCHandler Error", ex);
                throw ex; //未知异常，与致命异常同样处理，直接抛出，让系统结束运行
            }

            var jresponse = new JsonRpcResponse()
            {
                Id = id,
                Error = error,
                Result = result
            };

            var jsonResponse = PlainJsonConvert.Generate(jresponse);
            return Encoding.UTF8.GetBytes(jsonResponse);
        }
    }
}

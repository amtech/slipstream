﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;

using log4net;

using Newtonsoft.Json;

namespace ObjectServer
{
    /// <summary>
    /// SINGLETON
    /// 框架的全局入口点
    /// </summary>
    public sealed class Infrastructure : IDisposable
    {
        private static readonly Infrastructure s_instance = new Infrastructure();

        private bool disposed = false;
        private Config config;
        private bool initialized;
        private DBProfileCollection databaseProfiles = new DBProfileCollection();
        private ModuleCollection modules = new ModuleCollection();
        private SessionStore sessionStore = new SessionStore();
        private IExportedService exportedService = ServiceDispatcher.CreateDispatcher();

        private Infrastructure()
        {
        }

        ~Infrastructure()
        {
            Dispose(false);
        }

        #region IDisposable 成员

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.databaseProfiles.Dispose();
                }

            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion


        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Initialize(string configPath)
        {
            var json = File.ReadAllText(configPath, Encoding.UTF8);
            var cfg = JsonConvert.DeserializeObject<Config>(json);
            s_instance.InitializeInternal(cfg);
        }

        //这个做实际的初始化工作
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Initialize(Config cfg)
        {
            s_instance.InitializeInternal(cfg);
        }


        /// <summary>
        /// 为调试及测试而初始化
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Initialize()
        {
            if (s_instance.initialized)
            {
                return;
            }

            var cfg = new Config()
            {
                ConfigurationPath = null,
                DbType = ObjectServer.Backend.DatabaseType.Postgresql,
                DBHost = "localhost",
                DbName = "objectserver",
                DBPassword = "objectserver",
                DBPort = 5432,
                DBUser = "objectserver",
                ModulePath = "Modules",
                RootPassword = "root",
                Debug = true,
                LogLevel = "debug",
                SessionTimeout = TimeSpan.FromDays(1),
            };
            s_instance.InitializeInternal(cfg);
        }


        private void InitializeInternal(Config cfg)
        {
            try
            {
                TryInitialize(cfg);
            }
            catch (Exception ex)
            {
                var msg = "Failed to initialize framework!";
                Logger.Fatal(msg, ex);
                throw new InitializationException(msg, ex);
            }
        }

        private static void TryInitialize(Config cfg)
        {
            if (cfg == null)
            {
                throw new ArgumentNullException("cfg");
            }

            //日志子系统必须最先初始化
            ConfigurateLog4net(cfg);

            Logger.Info(() => "Initializing Session Storage Subsystem...");
            s_instance.sessionStore.Initialize(cfg.SessionProvider);

            //查找所有模块并加载模块元信息
            Logger.Info(() => "Initializing Module Management Subsystem...");
            if (!string.IsNullOrEmpty(cfg.ModulePath))
            {
                s_instance.modules.Initialize(cfg);
            }

            Logger.Info(() => "Initializing Database Instances...");
            s_instance.databaseProfiles.Initialize(cfg);

            s_instance.config = cfg;
            s_instance.initialized = true;

            Logger.Info(() => "System initialized.");
        }

        public static void Shutdown()
        {
            if (s_instance.initialized)
            {
                s_instance.Dispose(true);
            }
        }


        public static bool Initialized
        {
            get
            {
                return s_instance.initialized;
            }
        }


        internal static DBProfileCollection DBProfiles
        {
            get
            {
                return Instance.databaseProfiles;
            }
        }

        internal static ModuleCollection Modules
        {
            get
            {
                return Instance.modules;
            }
        }

        public static SessionStore SessionStore
        {
            get
            {
                return Instance.sessionStore;
            }
        }

        private static void ConfigurateLog4net(Config cfg)
        {
            log4net.Appender.IAppender appender;
            var layout = new log4net.Layout.PatternLayout(StaticSettings.LogPattern);

            if (string.IsNullOrEmpty(cfg.LogPath))
            {
                var consoleAppender = new log4net.Appender.ConsoleAppender()
                {
                    Layout = layout,
                };
                consoleAppender.ActivateOptions();
                appender = consoleAppender;
            }
            else
            {
                if (!System.IO.Directory.Exists(cfg.LogPath))
                {
                    throw new DirectoryNotFoundException(cfg.LogPath);
                }

                var fileAppender = new log4net.Appender.RollingFileAppender()
                {
                    File = Path.Combine(cfg.LogPath, StaticSettings.LogFileName),
                    AppendToFile = true,
                    RollingStyle = log4net.Appender.RollingFileAppender.RollingMode.Size,
                    Layout = layout,
                    Encoding = Encoding.UTF8,
                    StaticLogFileName = true,
                };
                fileAppender.ActivateOptions();
                appender = fileAppender;
            }
            layout.ActivateOptions();

            var hierarchy = 
                (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
            var rootLogger = hierarchy.Root;

            rootLogger.RemoveAllAppenders();
            rootLogger.AddAppender(appender);

            if (cfg.Debug)
            {
                rootLogger.Level = log4net.Core.Level.All;
            }
            else
            {
                rootLogger.Level = log4net.Core.Level.Info;
            }

        }


        public static Config Configuration
        {
            get
            {
                return Instance.config;
            }
        }

        public static IExportedService ExportedService
        {
            get
            {
                return Instance.exportedService;

            }
        }

        private static Infrastructure Instance
        {
            get
            {
                if (!s_instance.initialized)
                {
                    throw new Exception(
                        "尚未初始化系统，请调用 ObjectServerStarter.Initialize() 初始化");
                }

                return s_instance;
            }
        }


    }
}

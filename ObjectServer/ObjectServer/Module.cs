﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

using Newtonsoft.Json;

using ObjectServer.Runtime;
using ObjectServer.Backend;
using ObjectServer.Core;
using ObjectServer.Utility;

namespace ObjectServer
{
    /// <summary>
    /// TODO: 线程安全
    /// </summary>
    [Serializable]
    [JsonObject("module")]
    public sealed class Module
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<Assembly> assembly = new List<Assembly>();

        public const string ModuleFileName = "module.js";

        /// <summary>
        /// 整个系统中发现的所有模块
        /// </summary>
        private static Module[] s_allModules = new Module[] { };

        public Module()
        {
            //设置属性默认值
            this.Depends = new string[] { };
        }

        #region Serializable Fields
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty("label", Required = Required.Default)]
        public string Label { get; set; }

        [JsonProperty("description", Required = Required.Default)]
        public string Description { get; set; }

        [JsonProperty("source_files")]
        public string[] SourceFiles { get; set; }

        [JsonProperty("data_files")]
        public string[] DataFiles { get; set; }

        [JsonProperty("script_language")]
        public string ScriptLanguage { get; set; }

        [JsonProperty("auto_load")]
        public bool AutoLoad { get; set; }

        [JsonProperty("depends")]
        public string[] Depends { get; set; }

        #endregion

        public void Load(ObjectPool pool)
        {
            if (Log.IsInfoEnabled)
            {
                Log.InfoFormat("Begin to load module: '{0}'", this.Name);
            }

            var a = CompileSourceFiles(this.Path);
            this.Assemblies.Add(a);
            pool.AddModelsWithinAssembly(a);

            var moduleModel = (ModuleModel)pool["core.module"];
            this.State = ModuleStatus.Actived;
            moduleModel.LoadedModules.Add(this);

            if (Log.IsInfoEnabled)
            {
                Log.InfoFormat("Module '{0}' has been loaded.", this.Name);
            }
        }

        [JsonIgnore]
        public ICollection<Assembly> Assemblies { get { return this.assembly; } }

        [JsonIgnore]
        public string Path { get; set; }

        [JsonIgnore]
        public ModuleStatus State { get; set; }

        public static void LookupAllModules(string modulePath)
        {
            lock (typeof(Module))
            {
                var modules = new List<Module>();

                if (string.IsNullOrEmpty(modulePath) || !Directory.Exists(modulePath))
                {
                    return;
                }

                var moduleDirs = Directory.GetDirectories(modulePath);
                foreach (var moduleDir in moduleDirs)
                {
                    var moduleFilePath = System.IO.Path.Combine(moduleDir, ModuleFileName);
                    var module = DeserializeFromFile(moduleFilePath);
                    module.Path = moduleDir;
                    modules.Add(module);

                    if (Log.IsInfoEnabled)
                    {
                        Log.InfoFormat("Found module: [{0}], Path='{1}'", module.Name, module.Path);
                    }
                }

                //做依赖排序
                s_allModules = DependencySorter<Module, string>.Sort(
                    modules, m => m.Name, m => m.Depends);
            }
        }


        public static void UpdateModuleList(IDatabaseContext db)
        {
            lock (typeof(Module))
            {
                var sql = "select count(*) from core_module where name = @0";

                foreach (var m in s_allModules)
                {
                    var count = (long)db.QueryValue(sql, m.Name);
                    if (count == 0)
                    {
                        AddModuleToDb(db, m);
                    }
                }
            }
        }


        public static void LoadModules(IDatabaseContext db, ObjectPool pool)
        {
            //加载的策略是：
            //只加载存在于文件系统，且数据库中设置为 state = 'actived' 的
            lock (typeof(Module))
            {
                var sql = "select name from core_module where state = 'actived'";
                var modules = db.QueryAsDictionary(sql);

                foreach (var m in modules)
                {
                    var moduleName = (string)m["name"];
                    var module = s_allModules.SingleOrDefault(i => i.Name == moduleName);
                    if (module != null)
                    {
                        module.Load(pool);
                    }
                    else
                    {
                        var msg = string.Format("Cannot found module: '{0}'", moduleName);
                        throw new FileNotFoundException(msg, moduleName);
                    }
                }
            }
        }

        private static void AddModuleToDb(IDatabaseContext db, Module m)
        {
            var state = "deactived";
            if (m.AutoLoad)
            {
                state = "actived";
            }

            var insertSql = "insert into core_module(name, state, info) values(@0, @1, @2)";
            db.Execute(insertSql, m.Name, state, m.Label);
        }

        private static Module DeserializeFromFile(string moduleFilePath)
        {
            var json = File.ReadAllText(moduleFilePath, Encoding.UTF8);
            var module = JsonConvert.DeserializeObject<Module>(json);
            return module;
        }

        private Assembly CompileSourceFiles(string moduleDir)
        {
            var sourceFiles = new List<string>();
            foreach (var file in this.SourceFiles)
            {
                sourceFiles.Add(System.IO.Path.Combine(moduleDir, file));
            }

            //编译模块程序并注册所有对象
            var compiler = CompilerProvider.GetCompiler(this.ScriptLanguage);
            var a = compiler.CompileFromFile(sourceFiles);
            return a;
        }
    }
}

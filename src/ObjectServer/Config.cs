﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using ObjectServer.Utility;

namespace ObjectServer
{
    [Serializable]
    [XmlRoot("objectserver-config")]
    public sealed class Config
    {
        public Config()
        {
            this.LogPath = null;
            this.LogLevel = "info";
            this.SessionTimeout = new TimeSpan(0, 20, 0);
            this.SessionProvider = "ObjectServer.StaticSessionStoreProvider, ObjectServer.Core";

        }

        private string rootPassword;

        /// <summary>
        /// 配置文件路径
        /// </summary>
        [XmlIgnore]
        public string ConfigurationPath { get; set; }

        [XmlElement("db-type", IsNullable = false)]
        public Backend.DatabaseType DbType { get; set; }

        [XmlElement("db-host")]
        public string DBHost { get; set; }

        [XmlElement("db-port")]
        public int DBPort { get; set; }

        [XmlElement("db-user")]
        public string DBUser { get; set; }

        [XmlElement("db-password")]
        public string DBPassword { get; set; }

        /// <summary>
        /// 指定连接的数据库名，如果没有指定，则可以连接到用户所属的多个数据库。
        /// </summary>
        [XmlElement("db-name")]
        public string DbName { get; set; }

        [XmlElement("module-path")]
        public string ModulePath { get; set; }

        [XmlElement("debug")]
        public bool Debug { get; set; }

        [XmlElement("root-password")]
        public string RootPassword
        {
            get { return this.rootPassword; }
            set
            {
                this.rootPassword = value;
                this.RootPasswordHash = value.ToSha1();
            }
        }

        [XmlElement("log-path")]
        public string LogPath { get; set; }

        [XmlElement("log-level")]
        public string LogLevel { get; set; }

        [XmlElement("session-timeout")]
        public TimeSpan SessionTimeout { get; set; }

        [XmlElement("session-provider", IsNullable = false)]
        public string SessionProvider { get; set; }

        [XmlIgnore]
        public string RootPasswordHash { get; private set; }

    }
}
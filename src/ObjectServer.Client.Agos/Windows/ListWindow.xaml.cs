﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

using ObjectServer.Client.Agos.Models;
using ObjectServer.Client.Agos;

namespace ObjectServer.Client.Agos.Windows
{
    public partial class ListWindow : UserControl, IWindowAction
    {
        sealed class ManyToOneFieldConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value != null)
                {
                    var objs = (object[])value;
                    return objs[1];
                }
                else
                {
                    return value;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        sealed class EnumFieldConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value != null)
                {
                    var objs = (object[])value;
                    return objs[1];
                }
                else
                {
                    return value;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        private static Dictionary<string, Tuple<Type, IValueConverter>> COLUMN_TYPE_MAPPING
            = new Dictionary<string, Tuple<Type, IValueConverter>>()
        {
            {"Integer", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), null) },
            {"Chars", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), null) },
            {"Boolean", new Tuple<Type, IValueConverter>(typeof(DataGridCheckBoxColumn), null) },
            {"DateTime", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), null) },
            {"ManyToOne", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new ManyToOneFieldConverter()) },
            {"Enumeration", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new EnumFieldConverter()) },

            /*
            {"selection", typeof(DataGridTextColumn)},
            {"Text", typeof(DataGridTextColumn)},
            
            {"datetime", typeof(DataGridTextColumn)},
            {"date", typeof(DataGridTextColumn)},
            {"float", typeof(DataGridTextColumn)},
            {"reference", typeof(DataGridTextColumn)},
            {"many2one", typeof(DataGridTextColumn)},
             */
        };

        private readonly IList<string> fields = new List<string>();

        public ListWindow()
        {
            InitializeComponent();
        }

        #region IWindowAction Members

        public void Load(long actionId)
        {
            var app = (App)Application.Current;
            var actionIds = new long[] { actionId };
            app.ClientService.ReadModel("core.action_window", actionIds, null, actionRecords =>
            {
                var view = (object[])actionRecords[0]["view"];
                var viewIds = new long[] { (long)view[0] };
                app.ClientService.ReadModel("core.view", viewIds, null, viewRecords =>
                {
                    this.LoadInternal(actionRecords[0], viewRecords[0]);
                });
            });
        }

        private void LoadInternal(IDictionary<string, object> actionRecord, IDictionary<string, object> viewRecord)
        {
            var app = (App)Application.Current;

            var layout = (string)viewRecord["layout"];
            var layoutDoc = XDocument.Parse(layout);
            var modelName = (string)actionRecord["model"];

            this.InitializeColumns(app, layoutDoc, modelName);
        }

        private void LoadRecords(App app, string modelName)
        {
            //加载数据
            app.ClientService.SearchModel(modelName, null, null, 0, 80, ids =>
            {
                app.ClientService.ReadModel(modelName, ids, this.fields, records =>
                {
                    //我们需要一个唯一的字符串型 ID
                    var typeid = Guid.NewGuid().ToString();
                    this.gridList.ItemsSource = DataSourceCreator.ToDataSource(records, typeid, fields.ToArray());
                });
            });
        }

        private void InitializeColumns(App app, XDocument layoutDoc, string modelName)
        {
            var args = new object[] { modelName };
            app.ClientService.Execute("core.model", "GetFields", args, result =>
            {
                var fields = ((object[])result).Select(r => (Dictionary<string, object>)r);
                var viewFields = layoutDoc.Elements("tree").Elements();
                foreach (var f in viewFields)
                {
                    var fieldName = f.Attribute("name").Value;
                    var metaField = fields.Single(i => (string)i["name"] == fieldName);
                    this.AddColumn(fieldName, (string)metaField["type"], (string)metaField["label"]);
                }

                this.LoadRecords(app, modelName);
            });
        }

        #endregion

        private void AddColumn(string fieldName, string fieldType, string fieldLabel)
        {
            this.fields.Add(fieldName);
            var tuple = COLUMN_TYPE_MAPPING[fieldType];
            var col = Activator.CreateInstance(tuple.Item1) as DataGridBoundColumn;
            col.Header = fieldLabel;
            col.Binding = new System.Windows.Data.Binding(fieldName);
            if (tuple.Item2 != null)
            {
                col.Binding.Converter = tuple.Item2;
            }
            this.gridList.Columns.Add(col);

        }

    }
}

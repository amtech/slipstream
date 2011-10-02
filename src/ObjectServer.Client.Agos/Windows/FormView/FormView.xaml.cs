﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Threading;
using System.Windows.Data;

using ObjectServer.Client.Agos.Models;
using ObjectServer.Client.Agos;

namespace ObjectServer.Client.Agos.Windows.FormView
{
    public partial class FormView : UserControl
    {
        private IDictionary<string, object> viewRecord;

        private IDictionary<string, IFieldWidget> fieldWidgets;

        private readonly IList<string> fields = new List<string>();
        private long recordID;
        private string modelName;
        private FormModel formModel;

        public FormView(string model, long recordID)
        {
            InitializeComponent();

            this.modelName = model;
            this.recordID = recordID;

            this.Loaded += new RoutedEventHandler(this.OnLoaded);
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            this.Init();
        }

        private void Init()
        {
            var app = (App)Application.Current;

            //查询 form 视图
            var constraints = new object[][] { 
                new object[] { "kind", "=", "form" },
                new object[] { "model", "=", this.modelName },
            };

            app.ClientService.BeginExecute("core.view", "GetView", new object[] { this.modelName, "form", null }, o =>
            {
                this.viewRecord = (IDictionary<string, object>)o;
                var args = new object[] { this.modelName };
                app.ClientService.BeginExecute("core.model", "GetFields", args, result =>
                {
                    var metaFields = ((object[])result).Select(r => (IDictionary<string, object>)r).ToArray();

                    this.LoadForm(metaFields);

                    if (this.recordID > 0)
                    {
                        this.LoadData();
                    }
                });
            });
        }

        private void LoadForm(IDictionary<string, object>[] metaFields)
        {
            Debug.Assert(!string.IsNullOrEmpty(this.modelName));
            Debug.Assert(this.viewRecord != null);
            Debug.Assert(this.fields != null);

            //this.modelName = (string)this.actionRecord["model"];
            //var layout = (string)this.viewRecord["layout"];

            var app = (App)Application.Current;
            var layout = (String)this.viewRecord["layout"];

            var xs = new XmlSerializer(typeof(Malt.Layout.Models.Form));
            Malt.Layout.Models.Form form;
            using (var sr = new System.IO.StringReader(layout))
            {
                form = (Malt.Layout.Models.Form)xs.Deserialize(sr);
            }

            var factory = new FieldControlFactory(metaFields);
            var le = new Malt.Layout.LayoutEngine(factory);
            var layoutGrid = (UIElement)le.CreateLayoutTable(form);
            this.fieldWidgets = factory.CreatedFieldWidgets;
            this.Content = layoutGrid;

        }

        private void LoadData()
        {
            Debug.Assert(!string.IsNullOrEmpty(this.modelName));
            Debug.Assert(this.recordID > 0);
            Debug.Assert(this.viewRecord != null);
            Debug.Assert(this.fieldWidgets != null);

            //加载 Record 数据
            var app = (App)Application.Current;
            var ids = new long[] { this.recordID };

            var readArgs = new object[] { ids, this.fieldWidgets.Keys };
            app.ClientService.BeginExecute(this.modelName, "Read", readArgs, (result) =>
            {
                var objs = (object[])result;
                var records = objs.Select(r => (Dictionary<string, object>)r).ToArray();
                var record = records[0];
                this.formModel = new FormModel(record);
                foreach (var p in this.fieldWidgets)
                {
                    p.Value.Value = record[p.Key];
                }

            });

        }
    }
}

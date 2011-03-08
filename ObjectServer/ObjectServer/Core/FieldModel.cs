﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ObjectServer.Model;

namespace ObjectServer.Core
{
    [Resource]
    public class FieldModel : TableModel
    {
        public const string ModelName = "core.field";

        public FieldModel()
            : base(ModelName)
        {

            Fields.ManyToOne("model", "core.model").SetLabel("Model").SetRequired();
            Fields.Chars("name").SetLabel("Name").SetSize(64).SetRequired();
            Fields.Chars("label").SetLabel("Label").SetSize(256).SetNotRequired();
            Fields.Chars("type").SetLabel("Type").SetSize(32).SetRequired();
            Fields.Chars("help").SetLabel("Help").SetSize(256).SetNotRequired();
        }


    }
}

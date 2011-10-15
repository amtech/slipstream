﻿using System;
using System.Net;

namespace ObjectServer.Client.Model
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class FieldAttribute : Attribute
    {
        public FieldAttribute(string field)
        {
            this.Name = field;
        }

        public string Name { get; private set; }
    }
}

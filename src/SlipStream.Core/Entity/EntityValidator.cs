﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Sandwych;
using SlipStream.Exceptions;

namespace SlipStream.Entity
{
    /// <summary>
    /// 模型字段校验器
    /// </summary>
    internal static class EntityValidator
    {

        public static void ValidateRecordForCreation(
            this IEntity entity, IDictionary<string, object> record)
        {
            Debug.Assert(entity != null);
            Debug.Assert(record != null);

            var badFields = new Dictionary<string, string>();
            foreach (var pair in entity.Fields)
            {
                var field = pair.Value;

                if (AbstractSqlEntity.SystemReadonlyFields.Contains(field.Name))
                {
                    continue;
                }
                else if (field.IsRequired
                    && field.DefaultProc == null
                    && field.ValueGetter == null
                    && record.ContainsKey(field.Name)
                    && record[field.Name].IsNull())
                {
                    badFields.Add(field.Name, "字段不能为空");
                }
            }

            if (badFields.Count > 0)
            {
                throw new ValidationException("校验待创建记录的字段时发现错误", badFields);
            }
        }

        public static void ValidateRecordForWriting(
            this IEntity entity, IDictionary<string, object> record)
        {
            Debug.Assert(entity != null);
            Debug.Assert(record != null);

            var badFields = new Dictionary<string, string>();
            foreach (var fieldName in record.Keys)
            {
                var field = entity.Fields[fieldName];

                if (AbstractSqlEntity.SystemReadonlyFields.Contains(field.Name))
                {
                    continue;
                }
                else if (field.IsRequired && field.DefaultProc == null && record[field.Name].IsNull())
                {
                    badFields.Add(field.Name, "字段不能为空");
                }
                else if (field.IsReadonly && record.ContainsKey(field.Name))
                {
                    badFields.Add(field.Name, "不能修改只读字段");
                }
            }

            if (badFields.Count > 0)
            {
                throw new ValidationException("校验待更新记录的字段时发现错误", badFields);
            }
        }

    }
}

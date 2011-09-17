﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Data;

using NHibernate.SqlCommand;

using ObjectServer.Exceptions;
using ObjectServer.Sql;
using ObjectServer.Data;

namespace ObjectServer.Model
{
    /// <summary>
    /// 实体类基类
    /// </summary>
    public abstract class AbstractModel : AbstractResource, IModel
    {
        public const string IDFieldName = "_id";
        public const string VersionFieldName = "_version";
        public const string CreatedTimeFieldName = "_created_time";
        public const string UpdatedTimeFieldName = "_updated_time";
        public const string CreatedUserFieldName = "_created_user";
        public const string UpdatedUserFieldName = "_updated_user";
        public const string ActiveFieldName = "_active";

        private readonly IFieldCollection fields;

        public static readonly string QuotedIdColumn =
            DataProvider.Dialect.QuoteForColumnName(IDFieldName);

        protected AbstractModel(string name)
            : base(name)
        {
            this.AutoMigration = true;
            this.fields = new FieldCollection(this);
            this.Inheritances = new InheritanceCollection();

            this.RegisterInternalServiceMethods();
            this.AddInternalFields();
        }

        /// <summary>
        /// 此函数要允许多次调用
        /// </summary>
        /// <param name="db"></param>
        public override void Initialize(IDBProfile db, bool update)
        {
            if (db == null)
            {
                throw new ArgumentNullException("db");
            }

            base.Initialize(db, update);

            this.InitializeInheritances(db);
            this.VerifyFields();

            if (update)
            {
                this.SyncModel(db);
            }
        }

        private void VerifyFields()
        {

            foreach (var pair in this.Fields)
            {
                pair.Value.VerifyDefinition();
            }
        }


        /// <summary>
        /// 注册内部服务，每个模型都有
        /// </summary>
        private void RegisterInternalServiceMethods()
        {
            var selfType = typeof(AbstractModel);
            this.RegisterServiceMethod(selfType.GetMethod("Count"));
            this.RegisterServiceMethod(selfType.GetMethod("Search"));
            this.RegisterServiceMethod(selfType.GetMethod("Create"));
            this.RegisterServiceMethod(selfType.GetMethod("Read"));
            this.RegisterServiceMethod(selfType.GetMethod("Write"));
            this.RegisterServiceMethod(selfType.GetMethod("Delete"));
        }

        private void AddInternalFields()
        {
            Debug.Assert(!this.Fields.ContainsKey(IDFieldName));

            var idField = new ScalarField(this, IDFieldName, FieldType.ID)
                .Required().Readonly();
            this.fields.Add(IDFieldName, idField);
        }

        /// <summary>
        /// 初始化继承设置
        /// </summary>
        /// <param name="db"></param>
        private void InitializeInheritances(IDBProfile db)
        {
            Debug.Assert(db != null);
            //验证继承声明
            //这里可以安全地访问 many-to-one 指向的 ResourceContainer 里的对象，因为依赖排序的原因
            //被指向的对象肯定已经更早注册了
            foreach (var ii in this.Inheritances)
            {
                if (!db.ContainsResource(ii.BaseModel))
                {
                    var msg = string.Format(
                        "Cannot found the base model '{0}' in inheritances", ii.BaseModel);
                    throw new ResourceNotFoundException(msg, ii.BaseModel);
                }

                if (!this.Fields.ContainsKey(ii.RelatedField))
                {
                    throw new FieldAccessException();
                }

                //把“基类”模型的字段引用复制过来
                var baseModel = (IModel)db.GetResource(ii.BaseModel);
                foreach (var baseField in baseModel.Fields)
                {
                    if (!this.Fields.ContainsKey(baseField.Key))
                    {
                        var imf = new InheritedField(this, baseField.Value);
                        this.Fields.Add(baseField.Key, imf);
                    }
                }

            }
        }

        #region Inheritance staff

        public ICollection<InheritanceInfo> Inheritances { get; private set; }

        protected AbstractModel Inherit(string modelName, string relatedField)
        {
            var ii = new InheritanceInfo(modelName, relatedField);
            if (this.Inheritances.Select(i => i.BaseModel).Contains(modelName))
            {
                var msg = string.Format("Duplicated inheritance: '{0}'", modelName);
                throw new ArgumentException(msg, modelName);
            }

            this.Inheritances.Add(ii);

            return this;
        }

        #endregion

        /// <summary>
        /// 同步代码定义的模型到数据库
        /// </summary>
        /// <param name="db"></param>
        private void SyncModel(IDBProfile db)
        {
            Debug.Assert(db != null);

            //检测此模型是否存在于数据库 core_model 表
            var modelId = this.FindExistedModelInDb(db);

            if (modelId == null)
            {
                this.CreateModel(db);
                modelId = this.FindExistedModelInDb(db);
            }

            this.SyncFields(db, modelId.Value);
        }

        /// <summary>
        /// 同步代码定义的字段到数据库
        /// </summary>
        /// <param name="db"></param>
        /// <param name="modelId"></param>
        private void SyncFields(IDBProfile db, long modelId)
        {
            Debug.Assert(db != null);

            //同步代码定义的字段与数据库 core_model_field 表里记录的字段信息
            var sqlQuery = SqlString.Parse("select * from core_field where module=? and model=?");

            var dbFields = db.DBContext.QueryAsDictionary(sqlQuery, this.Module, modelId);
            var dbFieldsNames = (from f in dbFields select (string)f["name"]).ToArray();

            //先插入代码定义了，但数据库不存在的            
            var sql = @"
insert into core_field(module, model, name, relation, label, type, help) 
    values(?,?,?,?,?,?,?)";
            var sqlInsert = SqlString.Parse(sql);
            var fieldsToAppend = this.Fields.Keys.Except(dbFieldsNames);
            foreach (var fieldName in fieldsToAppend)
            {
                var field = this.Fields[fieldName];
                db.DBContext.Execute(sqlInsert,
                    this.Module, modelId, fieldName, field.Relation, field.Label, field.Type.ToString(), "");
            }

            //删除数据库存在，但代码未定义的
            var fieldsToDelete = dbFieldsNames.Except(this.Fields.Keys);
            sql = @"delete from core_field where name=? and module=? and model=?";
            var sqlDelete = SqlString.Parse(sql);
            foreach (var f in fieldsToDelete)
            {
                db.DBContext.Execute(sqlDelete, f, this.Module, modelId);
            }

            //更新现存的（交集）
            var fieldsToUpdate = dbFieldsNames.Intersect(this.Fields.Keys);
            foreach (var dbField in dbFields)
            {
                var fieldName = (string)dbField["name"];

                if (fieldsToUpdate.Contains(fieldName))
                {
                    SyncSingleField(db, dbField, fieldName);
                }
            }
        }

        /// <summary>
        /// 同步单个字段
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <param name="dbField"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private void SyncSingleField(IDBProfile db, Dictionary<string, object> dbField, string fieldName)
        {
            Debug.Assert(db != null);
            Debug.Assert(dbField != null);
            Debug.Assert(!string.IsNullOrEmpty(fieldName));

            var fieldLabel = dbField["label"].IsNull() ? null : (string)dbField["label"];
            var fieldRelation = dbField["relation"].IsNull() ? null : (string)dbField["relation"];
            var fieldHelp = dbField["help"].IsNull() ? null : (string)dbField["help"];
            var fieldType = (string)dbField["type"];
            var fieldId = (long)dbField[IDFieldName];

            var metaField = this.Fields[fieldName];
            var metaFieldType = metaField.Type.ToString();
            if (fieldLabel != metaField.Label ||
                fieldRelation != metaField.Relation ||
                fieldType != metaFieldType ||
                fieldHelp != metaField.Help)
            {
                /*
                 * 
                UPDATE ""core_field"" SET ""type""=@0, ""relation""=@1, ""label""=@2, 
                ""help""=@3  WHERE ""_id""=@4";
                */
                var sql =
                    new SqlString(
                        "update core_field set ",
                        "type=", Parameter.Placeholder, ",",
                        "relation=", Parameter.Placeholder, ",",
                        "label=", Parameter.Placeholder, ",",
                        "help=", Parameter.Placeholder,
                        " where _id=", Parameter.Placeholder);
                db.DBContext.Execute(
                    sql, metaFieldType, metaField.Relation, metaField.Label, metaField.Help, fieldId);

            }
        }

        private long? FindExistedModelInDb(IDBProfile db)
        {
            Debug.Assert(db != null);

            //var sql = "SELECT MAX(\"_id\") FROM core_model WHERE name=@0";
            var sql = new SqlString(
                "select max(",
                QuotedIdColumn,
                ") from ",
                DataProvider.Dialect.QuoteForTableName("core_model"),
                " where ",
                DataProvider.Dialect.QuoteForColumnName("name"), "=", Parameter.Placeholder);
            var o = db.DBContext.QueryValue(sql, this.Name);
            if (o.IsNull())
            {
                return null;
            }
            else
            {
                return (long)o;
            }
        }

        private void CreateModel(IDBProfile db)
        {
            Debug.Assert(db != null);

            var sql = new SqlString(
                "insert into core_model(name, module, label) values(",
                Parameter.Placeholder, ",",
                Parameter.Placeholder, ",",
                Parameter.Placeholder, ")");
            var rowCount = db.DBContext.Execute(sql, this.Name, this.Module, this.Label);

            if (rowCount != 1)
            {
                throw new ObjectServer.Exceptions.DataException("Failed to insert record of table core_model");
            }

            sql = new SqlString(
                "select max(",
                QuotedIdColumn,
                ") from ",
                DataProvider.Dialect.QuoteForTableName("core_model"),
                " where ",
                DataProvider.Dialect.QuoteForColumnName("name"), "=", Parameter.Placeholder,
                " and ",
                DataProvider.Dialect.QuoteForColumnName("module"), "=", Parameter.Placeholder);

            var modelId = (long)db.DBContext.QueryValue(sql, this.Name, this.Module);

            //插入一笔到 core_model_data 方便以后导入时引用
            var key = "model_" + this.Name.Replace('.', '_');
            Core.ModelDataModel.Create(db.DBContext, this.Module, Core.ModelModel.ModelName, key, modelId);
        }

        /// <summary>
        /// 获取此对象以来的所有其他对象名称
        /// 这里处理的很简单，就是直接检测 many-to-one 的对象
        /// </summary>
        /// <returns></returns>
        public override string[] GetReferencedObjects()
        {
            var query =
                from f in this.Fields.Values
                where f.Type == FieldType.ManyToOne
                select f.Relation;

            //自己不能依赖自己
            query = from m in query
                    where m != this.Name
                    select m;

            return query.Distinct().ToArray();
        }

        public IFieldCollection Fields { get { return this.fields; } }

        public bool ContainsField(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException("fieldName");
            }

            return this.Fields.ContainsKey(fieldName);
        }

        public IField[] GetAllStorableFields()
        {
            return this.Fields.Values.Where(f => f.IsColumn() && f.Name != IDFieldName).ToArray();
        }

        public override void MergeFrom(IResource res)
        {
            if (res == null)
            {
                throw new ArgumentNullException("res");
            }

            base.MergeFrom(res);

            var model = res as IModel;
            if (model != null)
            {
                //这里的字段合并策略也是添加，如果存在就直接替换
                foreach (var field in model.Fields)
                {
                    this.Fields[field.Key] = field.Value;
                }
            }
        }



        #region Service Methods

        [ServiceMethod]
        public static long Count(IModel model, IServiceContext scope, object[] constraints = null)
        {
            EnsureServiceMethodArgs(model, scope);

            return model.CountInternal(scope, constraints);
        }

        [ServiceMethod]
        public static long[] Search(
            IModel model, IServiceContext scope, object[] constraints = null, object[] order = null, long offset = 0, long limit = 0)
        {
            EnsureServiceMethodArgs(model, scope);

            OrderExpression[] orderInfos = OrderExpression.GetDefaultOrders();

            if (order != null)
            {
                orderInfos = new OrderExpression[order.Length];
                for (int i = 0; i < orderInfos.Length; i++)
                {
                    var orderTuple = (object[])order[i];
                    var so = SortDirectionParser.Parser((string)orderTuple[1]);
                    orderInfos[i] = new OrderExpression((string)orderTuple[0], so);
                }
            }

            return model.SearchInternal(scope, constraints, orderInfos, offset, limit);
        }

        [ServiceMethod]
        public static Dictionary<string, object>[] Read(
            IModel model, IServiceContext scope, dynamic clientIds, dynamic clientFields = null)
        {
            EnsureServiceMethodArgs(model, scope);

            string[] strFields = null;
            if (clientFields != null)
            {
                strFields = new string[clientFields.Length];
                for (int i = 0; i < strFields.Length; i++)
                {
                    strFields[i] = clientFields[i];
                }
            }
            var ids = new long[clientIds.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = clientIds[i];
            }
            return model.ReadInternal(scope, ids, strFields);
        }

        [ServiceMethod]
        public static long Create(
            IModel model, IServiceContext scope, IDictionary<string, object> propertyBag)
        {
            EnsureServiceMethodArgs(model, scope);
            return model.CreateInternal(scope, propertyBag);
        }

        [ServiceMethod]
        public static void Write(
           IModel model, IServiceContext scope, object id, IDictionary<string, object> userRecord)
        {
            EnsureServiceMethodArgs(model, scope);
            model.WriteInternal(scope, (long)id, userRecord);
        }

        [ServiceMethod]
        public static void Delete(
            IModel model, IServiceContext scope, dynamic clientIDs)
        {
            EnsureServiceMethodArgs(model, scope);

            long[] ids;

            if (clientIDs is long)
            {
                ids = new long[] { clientIDs };
            }
            else
            {
                ids = new long[clientIDs.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    ids[i] = clientIDs[i];
                }
            }

            model.DeleteInternal(scope, ids);
        }


        private static void EnsureServiceMethodArgs(IModel self, IServiceContext scope)
        {
            if (self == null)
            {
                throw new ArgumentNullException("self");
            }

            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }
        }


        #endregion


        #region IModel Members

        public abstract string TableName { get; protected set; }
        public virtual bool Hierarchy { get; protected set; }
        public virtual bool CanCreate { get; protected set; }
        public virtual bool CanRead { get; protected set; }
        public virtual bool CanWrite { get; protected set; }
        public virtual bool CanDelete { get; protected set; }

        public bool AutoMigration { get; protected set; }

        public virtual bool LogCreation { get; protected set; }
        public virtual bool LogWriting { get; protected set; }


        public virtual NameGetter NameGetter { get; protected set; }

        public abstract long CountInternal(IServiceContext scope, object[] constraints = null);
        public abstract long[] SearchInternal(
            IServiceContext scope, object[] constraints = null, OrderExpression[] orders = null, long offset = 0, long limit = 0);
        public abstract long CreateInternal(
            IServiceContext scope, IDictionary<string, object> propertyBag);
        public abstract void WriteInternal(
            IServiceContext scope, long id, IDictionary<string, object> record);
        public abstract Dictionary<string, object>[] ReadInternal(
            IServiceContext scope, long[] ids, string[] requiredFields = null);
        public abstract void DeleteInternal(IServiceContext scope, long[] ids);
        public abstract dynamic Browse(IServiceContext scope, long id);

        #endregion
    }
}

namespace Terrasoft.Configuration.OneCBaseEntity
{
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.ServiceModel.Activation;
    using System.Linq;
    using System.Runtime.Serialization;
    using Terrasoft.Web.Common;
    using Web.Http.Abstractions;
    using System.Reflection;

    using System.Diagnostics;
    using System;
    using Core;
    using Core.DB;
    using Core.Entities;
    using Terrasoft.Core.Configuration;
    using Common;
    using System.Globalization;

    using Configuration.GenIntegrationLogHelper;
    using Configuration.GenOneCSvcIntegration;
    using Terrasoft.Configuration.OneCBaseEntity;
    using Terrasoft.Configuration.GenOneCIntegrationHelper;

    [DataContract]
    public abstract class OneCBaseEntity<T> where T : OneCBaseEntity<T>
    {
        [DataMember(Name = "BPMId")]
        public string LocalId { get; set; }
        [DataMember(Name = "Id")]
        public string Id1C { get; set; }
        [IgnoreDataMember]
        public Guid BpmId { get; set; }


        [DataMember(Name = "CreatedOn")]
        public string CreatedOn { get; set; }
        [DataMember(Name = "ModifiedOn")]
        public string ModifiedOn { get; set; }

        [IgnoreDataMember]
        public string EntityName => typeof(T).Name.Replace("OneC", "");

        [IgnoreDataMember]
        private UserConnection _userConnection;
        [IgnoreDataMember]
        public UserConnection UserConnection
        {
            get =>
                _userConnection ??
                (_userConnection = HttpContext.Current.Session["UserConnection"] as UserConnection);
            set => _userConnection = value;
        }

        public OneCBaseEntity<T> ProcessRemoteItem(bool isFull = true)
        {
            bool shouldResolveRemoteItem = (!string.IsNullOrEmpty(this.LocalId) && this.LocalId != "00000000-0000-0000-0000-000000000000") ||
                               (!string.IsNullOrEmpty(this.Id1C) && this.Id1C != "00000000-0000-0000-0000-000000000000");

            if (shouldResolveRemoteItem && this.BpmId == Guid.Empty)
            {
                this.ResolveRemoteItem();
            }

            if (shouldResolveRemoteItem && (this.BpmId == Guid.Empty || isFull))
            {
                this.SaveRemoteItem();
            }

            return (OneCBaseEntity<T>)this;
        }

        public abstract bool SaveRemoteItem();
        public abstract List<T> GetItem(SearchFilter searchFilter);
        public abstract bool ResolveRemoteItem();

        protected Select GetItemByFilters(Select selectQuery, SearchFilter searchFilters)
        {
            if (searchFilters == null)
            {
                return selectQuery;
            }

            if (!string.IsNullOrEmpty(searchFilters.Id1C))
            {
                selectQuery = selectQuery.Where(EntityName, "GenID1C").IsEqual(Column.Parameter(searchFilters.Id1C)) as Select;
            }
            else if (!string.IsNullOrEmpty(searchFilters.LocalId))
            {
                selectQuery = selectQuery.Where(EntityName, "Id").IsEqual(Column.Parameter(new Guid(searchFilters.LocalId))) as Select;
            }
            else if (!string.IsNullOrEmpty(searchFilters.CreatedFrom) || !string.IsNullOrEmpty(searchFilters.CreatedTo))
            {
                if (!string.IsNullOrEmpty(searchFilters.CreatedFrom))
                {
                    selectQuery = selectQuery.Where(EntityName, "CreatedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(searchFilters.CreatedFrom))) as Select;
                }
                if (!string.IsNullOrEmpty(searchFilters.CreatedTo))
                {
                    selectQuery = selectQuery.And(EntityName, "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(searchFilters.CreatedTo))) as Select;
                }
            }
            else if (!string.IsNullOrEmpty(searchFilters.ModifiedFrom) || !string.IsNullOrEmpty(searchFilters.ModifiedTo))
            {
                if (!string.IsNullOrEmpty(searchFilters.ModifiedFrom))
                {
                    selectQuery = selectQuery.Where(EntityName, "ModifiedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(searchFilters.ModifiedFrom))) as Select;
                }
                if (!string.IsNullOrEmpty(searchFilters.ModifiedTo))
                {
                    selectQuery = selectQuery.And(EntityName, "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(searchFilters.ModifiedTo))) as Select;
                }
            }

            return selectQuery;
        }

        protected bool ResolveRemoteItemByQuery(Select selectQuery)
        {
            if (string.IsNullOrEmpty(LocalId) && string.IsNullOrEmpty(Id1C))
            {
                return false;
            }

            var success = false;

            if (!string.IsNullOrEmpty(LocalId))
            {
                selectQuery = selectQuery.Where(EntityName, "Id").IsEqual(Column.Parameter(new Guid(LocalId))) as Select;
            }
            else if (!string.IsNullOrEmpty(Id1C))
            {
                selectQuery = selectQuery.Where(EntityName, "GenID1C").IsEqual(Column.Parameter(Id1C)) as Select;
            }
            else
            {
                return false;
            }

            var entityId = selectQuery.ExecuteScalar<Guid>();
            if (entityId != Guid.Empty)
            {
                BpmId = entityId;
                success = true;
            }

            return success;
        }

        public bool SaveToDatabase()
        {
            var success = false;
            var oneCHelper = new OneCIntegrationHelper();
            var entity = UserConnection.EntitySchemaManager
                .GetInstanceByName(EntityName).CreateEntity(UserConnection);

            if (this.BpmId == Guid.Empty)
            {
                entity.SetDefColumnValues();
            }
            else if (!entity.FetchFromDB(entity.Schema.PrimaryColumn.Name, this.BpmId))
            {
                entity.SetDefColumnValues();
            }

            if (!string.IsNullOrEmpty(this.Id1C))
            {
                entity.SetColumnValue("GenID1C", this.Id1C);
            }

            entity.SetColumnValue("ModifiedOn", DateTime.Now);

            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties()
            .Where(property => property.GetCustomAttribute<DatabaseColumnAttribute>() != null)
            .ToArray();

            foreach (PropertyInfo property in properties)
            {
                DatabaseColumnAttribute attribute = property.GetCustomAttribute<DatabaseColumnAttribute>();

                var propertyValue = property.GetValue(this);
                if (propertyValue != null && propertyValue.ToString() != "00000000-0000-0000-0000-000000000000")
                {
                    string joinColumn = attribute.JoinColumn;
                    if (joinColumn != null)
                    {
                        string tableName = attribute.TableName;
                        var valueFromDatabase = oneCHelper.GetId(tableName, propertyValue.ToString());
                        if (valueFromDatabase != null)
                        {
                            entity.SetColumnValue(joinColumn, valueFromDatabase);
                        }
                    }
                    else
                    {
                        string columnName = attribute.ColumnName;
                        entity.SetColumnValue(columnName, propertyValue);
                    }
                }
            }

            if (entity.StoringState == StoringObjectState.Changed || this.BpmId == Guid.Empty)
            {
                success = entity.Save(true);
            }
            else
            {
                success = true;
            }

            this.CreatedOn = entity.GetColumnValue("CreatedOn").ToString();
            this.ModifiedOn = entity.GetColumnValue("ModifiedOn").ToString();
            this.BpmId = (Guid)entity.GetColumnValue("Id");
            return success;
        }

        public List<T> GetFromDatabase(SearchFilter searchFilter, Dictionary<string, string> searchableColumns = null)
        {
            var result = new List<T>();

            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties()
            .Where(property => property.GetCustomAttribute<DatabaseColumnAttribute>() != null)
            .ToArray();

            var selectQuery = new Select(UserConnection)
                .Column(EntityName, "Id")
                .Column(EntityName, "GenID1C")
                .Column(EntityName, nameof(ModifiedOn))
                .Column(EntityName, nameof(CreatedOn))
                .From(EntityName) as Select;
            foreach (PropertyInfo property in properties)
            {
                DatabaseColumnAttribute attribute = property.GetCustomAttribute<DatabaseColumnAttribute>();
                string columnName = attribute.ColumnName;
                string tableName = attribute.TableName;
                string joinColumn = attribute.JoinColumn;

                selectQuery = selectQuery.Column(tableName, columnName).As(attribute.ColumnFullName);
                if (joinColumn != null)
                {
                    selectQuery = selectQuery.LeftOuterJoin(tableName)
                    .On(tableName, "Id").IsEqual(EntityName, joinColumn) as Select;
                }
            }

            if (searchFilter != null)
            {
                selectQuery = GetItemByFilters(selectQuery, searchFilter);
            }

            if (searchableColumns != null && searchableColumns.Count > 0)
            {
                foreach (var searchField in searchableColumns)
                {
                    selectQuery = selectQuery.Where(EntityName, searchField.Key).IsEqual(Column.Parameter(searchField.Value)) as Select;

                }
            }

            using (var dbExecutor = UserConnection.EnsureDBConnection())
            {
                using (var reader = selectQuery.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        T newEntity = Activator.CreateInstance<T>();

                        foreach (PropertyInfo property in properties)
                        {
                            var columnName = property.GetCustomAttribute<DatabaseColumnAttribute>().ColumnFullName;
                            int columnIndex = reader.GetOrdinal(columnName);

                            if (!reader.IsDBNull(columnIndex))
                            {
                                object columnValue = reader.GetValue(columnIndex);
                                property.SetValue(newEntity, columnValue);
                            }
                        }

                        newEntity.LocalId = reader.GetValue(reader.GetOrdinal("Id")).ToString() ?? "";
                        newEntity.Id1C = reader.GetValue(reader.GetOrdinal("GenID1C")).ToString() ?? "";
                        newEntity.ModifiedOn = reader.GetDateTime(reader.GetOrdinal(nameof(ModifiedOn))).ToString() ?? "";
                        newEntity.CreatedOn = reader.GetDateTime(reader.GetOrdinal(nameof(CreatedOn))).ToString() ?? "";
                        result.Add(newEntity);
                    }
                }
            }

            return result;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class DatabaseColumnAttribute : Attribute
    {
        public string ColumnName { get; set; }
        public string TableName { get; set; }
        public string JoinColumn { get; set; }

        public string ColumnFullName => $"{TableName}.{ColumnName}";

        public DatabaseColumnAttribute(string parentName, string columnName, string joinColumn = null)
        {
            this.TableName = parentName;
            this.ColumnName = columnName;
            JoinColumn = joinColumn;
        }
    }
}
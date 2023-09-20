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
        private  OneCQueryBuilder queryBuilder;

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

        public abstract List<T> GetItem(SearchFilter searchFilter);

        protected bool ResolveRemoteItem()
        {
            queryBuilder = new OneCQueryBuilder(UserConnection, EntityName);
            queryBuilder.FindFirstEntityId();

            if (string.IsNullOrEmpty(LocalId) && string.IsNullOrEmpty(Id1C))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(LocalId))
            {
                queryBuilder.AddCondition<Guid>("Id", new Guid(LocalId));
            }
            else if (!string.IsNullOrEmpty(Id1C))
            {
                queryBuilder.AddCondition<string>("GenID1C", Id1C);
            }
            else
            {
                return false;
            }

            try
            {
                var entityId = queryBuilder.BuildAndExecuteScalar<Guid>();
                if (entityId != Guid.Empty)
                {
                    BpmId = entityId;
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                var result = ex.Message;
                throw;
            }
            

            return false;
        }

        public virtual bool SaveRemoteItem()
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
                var propertyValue = property.GetValue(this);                

                if (propertyValue != null && propertyValue.ToString() != "00000000-0000-0000-0000-000000000000")
                {
                    string[] formats = { "yyyy-MM-ddTHH:mm:ss.fff", "dd.MM.yyyy" };
                    if (DateTime.TryParseExact(propertyValue.ToString(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTimeValue))
                    {
                        propertyValue = dateTimeValue;
                    }

                    DatabaseColumnAttribute attribute = property.GetCustomAttribute<DatabaseColumnAttribute>();
                    string joinColumn = attribute.JoinColumn;
                    if (joinColumn != null)
                    {
                        var valueFromDatabase = oneCHelper.GetId(attribute.TableName, propertyValue.ToString(), attribute.ColumnName);
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
                try
                {
                    success = entity.Save(true);
                }
                catch (System.Exception ex)
                {

                    throw;
                }                
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

        protected List<T> GetFromDatabase(SearchFilter searchFilter, Dictionary<string, string> searchableColumns = null)
        {
            queryBuilder = new OneCQueryBuilder(UserConnection, EntityName);
            queryBuilder.AddBaseEntityFields();

            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties()
            .Where(property => property.GetCustomAttribute<DatabaseColumnAttribute>() != null)
            .ToArray();

            foreach (PropertyInfo property in properties)
            {
                DatabaseColumnAttribute attribute = property.GetCustomAttribute<DatabaseColumnAttribute>();

                var joinParameter = new JoinParameter(
                    attribute.ColumnName,
                    attribute.TableName,
                    attribute.JoinColumn,
                    attribute.ColumnAlias);

                queryBuilder.AddColumn(joinParameter);
            }

            queryBuilder.AddSearchFilters(searchFilter);

            if (searchableColumns != null && searchableColumns.Count > 0)
            {
                foreach (var searchField in searchableColumns)
                {
                    queryBuilder.AddCondition<string>(searchField.Key, searchField.Value);
                }
            }

            var result = new List<T>();
            using (var dbExecutor = UserConnection.EnsureDBConnection())
            {
                using (var reader = queryBuilder.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        T newEntity = Activator.CreateInstance<T>();

                        foreach (PropertyInfo property in properties)
                        {
                            var columnName = property.GetCustomAttribute<DatabaseColumnAttribute>().ColumnAlias;
                            int columnIndex = reader.GetOrdinal(columnName);

                            if (!reader.IsDBNull(columnIndex))
                            {
                                object columnValue = reader.GetValue(columnIndex);
                                if (columnValue is DateTime)
                                {
                                    property.SetValue(newEntity, columnValue.ToString());
                                }
                                else
                                {
                                    property.SetValue(newEntity, columnValue);
                                }                                
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

    /// <summary>
    /// ������� ��� ���������� ������������, �� ������������� �� ������� � ��� �����.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class DatabaseColumnAttribute : Attribute
    {
        /// <summary>
        /// ����� ������� � ��� �����.
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// ����� �������, �� ��� ���������� ��������.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// ����� �������, �� ���� ���������� �'������� (���� �).
        /// </summary>
        public string JoinColumn { get; set; }

        /// <summary>
        /// ��������� ����������� ������� (���� �).
        /// </summary>
        public string AdditionalColumnAlias { get; set; }

        /// <summary>
        /// ����� ����� �������, ��������� ����� ������� ��, �� ��������, ��������� ����������� �������.
        /// </summary>
        public string ColumnAlias
        {
            get
            {
                if (!string.IsNullOrEmpty(AdditionalColumnAlias))
                {
                    return $"{TableName}.{ColumnName}.{AdditionalColumnAlias}";
                }
                else
                {
                    return $"{TableName}.{ColumnName}";
                }
            }
        }

        /// <summary>
        /// ����������� �������� DatabaseColumnAttribute.
        /// </summary>
        /// <param name="parentName">����� �������, �� ��� ���������� ��������.</param>
        /// <param name="columnName">����� ������� � ��� �����.</param>
        /// <param name="joinColumn">����� �������, �� ���� ���������� �'������� (���� �).</param>
        /// <param name="additionalColumnAlias">��������� ����������� ������� (���� �).</param>
        public DatabaseColumnAttribute(string parentName, string columnName, string joinColumn = null, string additionalColumnAlias = null)
        {
            TableName = parentName;
            ColumnName = columnName;
            JoinColumn = joinColumn;
            AdditionalColumnAlias = additionalColumnAlias;
        }
    }
}
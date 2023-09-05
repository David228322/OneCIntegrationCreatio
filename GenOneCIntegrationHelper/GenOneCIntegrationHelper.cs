namespace Terrasoft.Configuration.GenOneCIntegrationHelper
{
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.ServiceModel.Activation;
    using System.Linq;
    using System.Runtime.Serialization;
    using Terrasoft.Web.Common;
    using Terrasoft.Web.Http.Abstractions;

    using System.Diagnostics;
    using System;
    using Terrasoft.Core;
    using Terrasoft.Core.DB;
    using Terrasoft.Core.Entities;
    using Terrasoft.Core.Configuration;
    using Terrasoft.Common;
    using System.Globalization;

    using Configuration.GenIntegrationLogHelper;

    public class OneCIntegrationHelper
    {
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

        public Guid GetId(string schemaName, string name, string columnName = "Name")
        {
            var result = Guid.Empty;
            var selEntity = new Select(UserConnection)
                .Column("Id").Top(1)
                .From(schemaName)
                .Where(columnName).IsLike(Column.Parameter("%" + name + "%"))
            as Select;

            result = selEntity.ExecuteScalar<Guid>();

            if (result == Guid.Empty && columnName == "Name")
            {
                var entity = UserConnection.EntitySchemaManager
                .GetInstanceByName(schemaName).CreateEntity(UserConnection);
                var now = DateTime.Now;

                entity.SetDefColumnValues();

                entity.SetColumnValue("Name", name);
                entity.SetColumnValue("ModifiedOn", now);
                entity.Save(true);

                result = (Guid)entity.GetColumnValue("Id");
            }

            return result;
        }

        public bool CheckId(string schemaName, string id = "", string name = "")
        {
            var result = false;
            var guidId = Guid.Empty;
            var selEntity = new Select(UserConnection)
                    .Column("Id").Top(1)
                    .From(schemaName)
                as Select;

            if (!string.IsNullOrEmpty(id))
                selEntity = selEntity.Where("Id").IsEqual(Column.Parameter(new Guid(id))) as Select;
            else if (!string.IsNullOrEmpty(name))
                selEntity = selEntity.Where("Name").IsLike(Column.Parameter("%" + name + "%")) as Select;
            else
                return false;

            guidId = selEntity.ExecuteScalar<Guid>();

            if (guidId != Guid.Empty)
            {
                result = true;
            }

            return result;
        }

        public Guid GetSerialId(string productId, string serialName)
        {
            var result = Guid.Empty;
            var selEntity = new Select(UserConnection)
                .Column("Id").Top(1)
                .From("GenSerial")
                .Where("GenProductId").IsEqual(Column.Parameter(new Guid(productId)))
                .And("Name").IsLike(Column.Parameter("%" + serialName + "%"))
            as Select;

            result = selEntity.ExecuteScalar<Guid>();

            if (result != Guid.Empty) return result;
            var entity = UserConnection.EntitySchemaManager
                .GetInstanceByName("GenSerial").CreateEntity(UserConnection);
            var now = DateTime.Now;

            entity.SetDefColumnValues();

            entity.SetColumnValue("Name", serialName);
            entity.SetColumnValue("GenProductId", new Guid(productId));
            entity.SetColumnValue("ModifiedOn", now);
            entity.Save(true);

            result = (Guid)entity.GetColumnValue("Id");

            return result;
        }

        public void DelItem(string itemId, string itemColumnName, string elementId, string elementColumnName, string schemaName)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Guid id = Guid.Empty;
            Select selEntity = new Select(UserConnection)
                .Column("Id").Top(1)
                .From(schemaName)
                .Where(itemColumnName).IsEqual(Column.Parameter(itemId))
                .And(elementColumnName).IsEqual(Column.Parameter(new Guid(elementId)))
            as Select;

            id = selEntity.ExecuteScalar<Guid>();
            LogHelper.Log(UserConnection, LogHelper.LogResult.Ok, "DelProduct", stopwatch, LogHelper.IntegrationDirection.Import, id);
            if (id != Guid.Empty)
            {
                Delete delete = new Delete(UserConnection)
                .From(schemaName)
                .Where("Id").IsEqual(Column.Parameter(id)) as Delete;
                delete.Execute();
            }
        }

        public List<string> GetList(string id, string searchColumnName, string getColumnName, string schemaName)
        {
            List<string> result = new List<string>();

            var selectQuery = new Select(UserConnection)
                .Column(getColumnName)
                .From(schemaName)
                .Where(searchColumnName).IsEqual(Column.Parameter(new Guid(id))) as Select;

            using (var dbExecutor = UserConnection.EnsureDBConnection())
            using (var reader = selectQuery.ExecuteReader(dbExecutor))
            {
                while (reader.Read())
                {
                    string temp = (reader[0] != DBNull.Value) ? reader[0].ToString() : string.Empty;
                    if (!string.IsNullOrEmpty(temp))
                    {
                        result.Add(temp);
                    }
                }
            }
            return result;
        }

        public Guid GetCultureId(string langCode)
        {
            var result = Guid.Empty;
            var selEntity = new Select(UserConnection)
                .Column("Id").Top(1)
                .From("SysCulture")
                .Where("Name").IsLike(Column.Parameter("%" + langCode + "%"))
            as Select;

            result = selEntity.ExecuteScalar<Guid>();
            return result;
        }
    }
}
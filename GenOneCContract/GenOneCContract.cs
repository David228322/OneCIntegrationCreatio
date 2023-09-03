namespace Terrasoft.Configuration.GenOneCContract
{
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.ServiceModel.Activation;
    using System.Linq;
    using System.Runtime.Serialization;
    using Terrasoft.Web.Common;
    using Terrasoft.Web.Http.Abstractions;
    using System.IO;

    using System.Diagnostics;
    using System;
    using Terrasoft.Core;
    using Terrasoft.Core.DB;
    using Terrasoft.Core.Entities;
    using Terrasoft.Core.Configuration;
    using Terrasoft.Common;
    using System.Globalization;

    using Terrasoft.Configuration.GenIntegrationLogHelper;
    using Terrasoft.Configuration.GenOneCSvcIntegration;
    using Terrasoft.Configuration.GenOneCIntegrationHelper;
    using Terrasoft.Configuration.OneCBaseEntity;

    [DataContract]
    public sealed class OneCContract : OneCBaseEntity<OneCContract>
    {
        [DataMember(Name = "Number")]
        public string Number { get; set; }
        [DataMember(Name = "Type")]
        public string Type { get; set; }
        [DataMember(Name = "ContactId")]
        public string ContactId { get; set; }

        public OneCBaseEntity<OneCContract> ProcessRemoteItem(bool isFull = true)
        {
            return base.ProcessRemoteItem(isFull);
        }

        public override bool ResolveRemoteItem()
        {
            var selEntity = new Select(UserConnection)
                .Column("Contract", "Id").Top(1)
                .From("Contract").As("Contract") as Select;

            return base.ResolveRemoteItemByQuery(selEntity);
        }

        public override bool SaveRemoteItem()
        {
            var success = false;
            var oneCHelper = new OneCIntegrationHelper();

            var type = Guid.Empty;
            var contactId = Guid.Empty;

            if (!string.IsNullOrEmpty(this.Type))
            {
                type = oneCHelper.GetId("ContractType", this.Type);
            }

            if (!string.IsNullOrEmpty(this.ContactId))
            {
                type = oneCHelper.GetId("ContactlId", contactId.ToString());
            }

            var entity = UserConnection.EntitySchemaManager
                .GetInstanceByName("Contract").CreateEntity(UserConnection);

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

            if (!string.IsNullOrEmpty(this.Number))
            {
                entity.SetColumnValue("Number", this.Number);
            }

            if (type != Guid.Empty)
            {
                entity.SetColumnValue("TypeId", type);
            }

            if (contactId != Guid.Empty)
            {
                entity.SetColumnValue("ContactId", contactId);
            }

            entity.SetColumnValue("ModifiedOn", DateTime.Now);

            if (entity.StoringState == StoringObjectState.Changed || base.BpmId == Guid.Empty)
            {
                success = entity.Save(true);
            }
            else
            {
                success = true;
            }
            this.BpmId = (Guid)entity.GetColumnValue("Id");
            return success;
        }

        public override List<OneCContract> GetItem(SearchFilter searchFilter)
        {
            var result = new List<OneCContract>();
            var selCon = new Select(UserConnection)
                .Column("Contract", "Id")
                .Column("Contract", "GenID1C")
                .Column("Contract", "Number")
                .Column("Contract", "AccountId")
                .Column("ContractType", "Name")
                .Column("Contract", "ModifiedOn")
                .Column("Contract", "CreatedOn")
                .From("Contract")
                .LeftOuterJoin("ContractType")
                .On("ContractType", "Id").IsEqual("Contract", "TypeId")
            as Select;

            selCon = base.GetItemByFilters(selCon, searchFilter);

            using (var dbExecutor = UserConnection.EnsureDBConnection())
            {
                try
                {
                    using (var reader = selCon.ExecuteReader(dbExecutor))
                    {
                        while (reader.Read())
                        {
                            result.Add(new OneCContract()
                            {
                                LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
                                Id1C = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
                                Number = (reader.GetValue(2) != System.DBNull.Value) ? (string)reader.GetValue(2) : "",
                                ContactId = (reader.GetValue(3) != System.DBNull.Value) ? (string)reader.GetValue(3).ToString() : "",
                                Type = (reader.GetValue(4) != System.DBNull.Value) ? (string)reader.GetValue(4) : "",
                           //     CreatedOn = (reader.GetValue(5) != System.DBNull.Value) ? DateTime.Parse(reader.GetValue(5).ToString()).ToLocalTime().ToString() : "",
                           //     ModifiedOn = (reader.GetValue(6) != System.DBNull.Value) ? DateTime.Parse(reader.GetValue(6).ToString()).ToLocalTime().ToString() : ""
                            });
                        }
                    }
                }
                catch (System.Exception ex)
                {

                    throw ex;
                }
            }
            return result;
        }
    }
}
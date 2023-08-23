namespace Terrasoft.Configuration.GenOneCOrder
{
    using System.IO;
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

    using Terrasoft.Configuration.GenIntegrationLogHelper;
    using Terrasoft.Configuration.GenOneCSvcIntegration;
    using Terrasoft.Configuration.GenOneCProduct;
    using Terrasoft.Configuration.OneCBaseEntity;
    using Terrasoft.Configuration.GenCOrderProduct;

    [DataContract]
    public class OneCOrder : OneCBaseEntity<OneCOrder>
    {
        [IgnoreDataMember]
        public List<OneCProduct> OneCProducts { get; set; } = new List<OneCProduct>();
        [DataMember(Name = "PrimaryAmount")]
        public decimal PrimaryAmount { get; set; }
        [DataMember(Name = "PaymentAmount")]
        public decimal PaymentAmount { get; set; }
        [DataMember(Name = "Number")]
        public string Number { get; set; }
        [DataMember(Name = "DeliveryAddress")]
        public string DeliveryAddress { get; set; }
        [DataMember(Name = "OrderStatus")]
        public string OrderStatus { get; set; }

        [DataMember(Name = "OrderProducts")]
        public List<OneCOrderProduct> OrderProducts { get; set; }

        public string ProcessRemoteItem(bool isFull = true)
        {
            return base.ProcessRemoteItem(isFull);
        }

        public override bool ResolveRemoteItem()
        {
            if (string.IsNullOrEmpty(this.LocalId) && string.IsNullOrEmpty(this.Id1C))
            {
                return false;
            }

            var selEntity = new Select(UserConnection)
                .Column("Order", "Id").Top(1)
                .From("Order")
                as Select;

            if (!string.IsNullOrEmpty(this.LocalId))
            {
                selEntity = selEntity.Where("Order", "Id").IsEqual(Column.Parameter(new Guid(this.LocalId))) as Select;
            }
            else if (!string.IsNullOrEmpty(this.Id1C))
            {
                selEntity = selEntity.Where("Order", "GenID1C").IsEqual(Column.Parameter(this.Id1C)) as Select;
            }
            else
            {
                return false;
            }

            var entityId = selEntity.ExecuteScalar<Guid>();
            if (entityId == Guid.Empty)
            {
                return false;
            }

            this.BpmId = entityId;
            return true;
        }
        
        public override bool SaveRemoteItem()
        {
            throw new NotImplementedException();
            /*
            var success = false;
            Terrasoft.Configuration.GenOneCSvcIntegration.Directory directory = new Terrasoft.Configuration.GenOneCSvcIntegration.Directory();
            var type = Guid.Empty;
            var counterparty = Guid.Empty;

            if (!string.IsNullOrEmpty(this.Type))
            {
                type = directory.GetId("OrderType", this.Type);
            }

            if (!string.IsNullOrEmpty(this.CounterpartyLocalId) && directory.СhekId("Account", this.CounterpartyLocalId))
            {
                counterparty = new Guid(this.CounterpartyLocalId);
            }

            var entity = UserConnection.EntitySchemaManager
                .GetInstanceByName("Order").CreateEntity(UserConnection);

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

            if (counterparty != Guid.Empty)
            {
                entity.SetColumnValue("AccountId", counterparty);
            }

            entity.SetColumnValue("ModifiedOn", DateTime.Now);

            if (entity.StoringState == StoringObjectState.Changed || this.BpmId == Guid.Empty)
            {
                success = entity.Save(true);
            }
            else
            {
                success = true;
            }
            this.BpmId = (Guid)entity.GetColumnValue("Id");
            return success; */
        }

        public override List<OneCOrder> GetItem(Search data)
        {
            var result = new List<OneCOrder>();
            var date = DateTime.Now;

            var selCon = new Select(UserConnection)
                .Column("Order", "Id")
                .Column("Order", "GenID1C")
                .Column("Order", "Number")
                .Column("Order", "PrimaryAmount")
                .Column("Order", "PaymentAmount")
                .Column("Order", "DeliveryAddress")
                .Column("OrderStatus", "Name").As("OrderStatusName")
                .From("Order")
                .LeftOuterJoin("OrderStatus").On("Order", "StatusId").IsEqual("OrderStatus", "Id")
            as Select;

            if (!string.IsNullOrEmpty(data.Id1C) || !string.IsNullOrEmpty(data.LocalId))
            {
                if (!string.IsNullOrEmpty(data.LocalId))
                    selCon = selCon.Where("Order", "Id").IsEqual(Column.Parameter(new Guid(data.LocalId))) as Select;
                else if (!string.IsNullOrEmpty(data.Id1C))
                    selCon = selCon.Where("Order", "GenID1C").IsEqual(Column.Parameter(data.Id1C)) as Select;

            }
            else if (!string.IsNullOrEmpty(data.CreatedFrom) || !string.IsNullOrEmpty(data.CreatedTo))
            {
                if (!string.IsNullOrEmpty(data.CreatedFrom))
                    selCon = selCon.Where("Order", "CreatedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(data.CreatedFrom))) as Select;
                else if (!string.IsNullOrEmpty(data.CreatedFrom) && !string.IsNullOrEmpty(data.CreatedTo))
                    selCon = selCon.And("Order", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.CreatedTo))) as Select;
                else if (!string.IsNullOrEmpty(data.CreatedTo))
                    selCon = selCon.Where("Order", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.CreatedTo))) as Select;

            }
            else if (!string.IsNullOrEmpty(data.ModifiedFrom) || !string.IsNullOrEmpty(data.ModifiedTo))
            {
                if (!string.IsNullOrEmpty(data.ModifiedFrom))
                    selCon = selCon.Where("Order", "ModifiedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedFrom))) as Select;
                else if (!string.IsNullOrEmpty(data.ModifiedFrom) && !string.IsNullOrEmpty(data.ModifiedTo))
                    selCon = selCon.And("Order", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedTo))) as Select;
                else if (!string.IsNullOrEmpty(data.ModifiedTo))
                    selCon = selCon.Where("Order", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedTo))) as Select;

            }	
            else
            {
                return result;
            }

            using (var dbExecutor = UserConnection.EnsureDBConnection())
            {
                using (var reader = selCon.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        result.Add(new OneCOrder()
                        {
                            LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
                            Id1C = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
                            Number = (reader.GetValue(2) != System.DBNull.Value) ? (string)reader.GetValue(2) : "",
                            PrimaryAmount = (reader.GetValue(3) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(3).ToString()) : 0,
                            PaymentAmount = (reader.GetValue(4) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(4).ToString()) : 0,
                            DeliveryAddress = (reader.GetValue(5) != System.DBNull.Value) ? (string)reader.GetValue(5) : "",
                            OrderStatus = (reader.GetValue(6) != System.DBNull.Value) ? (string)reader.GetValue(6) : "",
                        });
                    }
                }
            }

            var orderProducts = new OneCOrderProduct();
            foreach (var order in result)
            {
                order.OrderProducts = orderProducts.GetItem(order.LocalId);
            }

            return result;
        }
    }
}
namespace Terrasoft.Configuration.GenCOrderProduct
{
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.ServiceModel.Activation;
    using System.Linq;
    using System.Runtime.Serialization;
    using Terrasoft.Web.Common;
    using Web.Http.Abstractions;

    using System.Diagnostics;
    using System;
    using Core;
    using Core.DB;
    using Core.Entities;
    using Terrasoft.Core.Configuration;
    using Common;
    using System.Globalization;

    using Terrasoft.Configuration.GenIntegrationLogHelper;
    using Terrasoft.Configuration.GenOneCSvcIntegration;
    using Terrasoft.Configuration.OneCBaseEntity;

    [DataContract]
    public class OneCOrderProduct : OneCBaseEntity<OneCOrderProduct>
    {
        [DataMember(Name = "Name")]
        public string Name { get; set; }
        [DataMember(Name = "Currency")]
        public string Currency { get; set; }
        [DataMember(Name = "Price")]
        public decimal Price { get; set; }
        [DataMember(Name = "Quantity")]
        public decimal Quantity { get; set; }
        [DataMember(Name = "DiscountPercent")]
        public decimal DiscountPercent { get; set; }
        [DataMember(Name = "DiscountAmount")]
        public decimal DiscountAmount { get; set; }
        [DataMember(Name = "TotalAmount")]
        public decimal TotalAmount { get; set; }
        [DataMember(Name = "Unit")]
        public string Unit { get; set; }

        [DataMember(Name = "ProductLocalId")]
        public Guid ProductId { get; set; }
        [DataMember(Name = "OrderId")]
        public Guid OrderId { get; set; }

        public string ProcessRemoteItem(bool isFull = true)
        {
            return base.ProcessRemoteItem(isFull);
        }

        public override bool ResolveRemoteItem()
        {
            var selectQuery = new Select(UserConnection)
                .Column(base.Entity, "Id").Top(1)
                .From(base.Entity)
                .Where(base.Entity, "OrderId").IsEqual(Column.Parameter(OrderId)) as Select;

            return base.ResolveRemoteItemByQuery(selectQuery);
        }

        public override bool SaveRemoteItem()
        {
            if (ProductId == null || ProductId == Guid.Empty)
            {
                return false;
            }

            Directory directory = new Directory();

            if (!directory.ÑhekId("Product", ProductId.ToString()))
            {
                return false;
            }

            Guid unitId = Guid.Empty;
            Guid currencyId = Guid.Empty;

            if (!string.IsNullOrEmpty(Unit))
            {
                unitId = directory.GetId("Unit", Unit);
            }

            if (!string.IsNullOrEmpty(Currency))
            {
                if (Currency == "ãðí")
                {
                    Currency = "UAH";
                }
                currencyId = directory.GetId("Currency", Currency, "ShortName");
            }

            var entitySchema = UserConnection.EntitySchemaManager.GetInstanceByName("OrderProduct");
            var entity = entitySchema.CreateEntity(UserConnection);
            var now = DateTime.Now;

            bool isNewEntity = false;
            if (BpmId == Guid.Empty || !entity.FetchFromDB(entitySchema.PrimaryColumn.Name, BpmId))
            {
                entity.SetDefColumnValues();
                isNewEntity = true;
            }

            entity.SetColumnValue("GenID1C", Id1C);
            entity.SetColumnValue("ProductId", ProductId);
            entity.SetColumnValue("OrderId", OrderId);

            if (Price > 0)
            {
                entity.SetColumnValue("Price", Price);
            }

            if (currencyId != Guid.Empty)
            {
                entity.SetColumnValue("CurrencyId", currencyId);
            }

            if (Quantity > 0)
            {
                entity.SetColumnValue("Quantity", Quantity);
            }

            if (unitId != Guid.Empty)
            {
                entity.SetColumnValue("UnitId", unitId);
            }

            if (DiscountPercent > 0)
            {
                entity.SetColumnValue("DiscountPercent", DiscountPercent);
            }

            if (DiscountAmount > 0)
            {
                entity.SetColumnValue("DiscountAmount", DiscountAmount);
            }

            if (TotalAmount > 0)
            {
                entity.SetColumnValue("Amount", TotalAmount);
            }

            entity.SetColumnValue("TaxAmount", 0);

            if (isNewEntity || entity.StoringState == StoringObjectState.Changed)
            {
                entity.SetColumnValue("ModifiedOn", now);
                return entity.Save(true);
            }

            return true;
        }

        public List<OneCOrderProduct> GetAllByOrderId(string orderId)
        {
            var result = new List<OneCOrderProduct>();
            var date = DateTime.Now;

            orderId = (string.IsNullOrEmpty(orderId) ? this.OrderId.ToString() : orderId);
            var selCon = new Select(UserConnection)
                    .Column("OrderProduct", "Id")
                    .Column("OrderProduct", "Name")
                    .Column("OrderProduct", "Price")
                    .Column("OrderProduct", "Quantity")
                    .Column("OrderProduct", "DiscountPercent")
                    .Column("OrderProduct", "DiscountAmount")
                    .Column("OrderProduct", "TotalAmount")
                    .Column("Unit", "Name").As("Unit")
                    .Column("OrderProduct", "ProductId")
                    .Column("OrderProduct", "OrderId")
                    .From("OrderProduct")
                    .LeftOuterJoin("Unit").On("OrderProduct", "UnitId").IsEqual("Unit", "Id")
                    .Where("OrderProduct", "OrderId").IsEqual(Column.Parameter(new Guid(orderId)))
                    as Select;

            using (var dbExecutor = UserConnection.EnsureDBConnection())
            {
                using (var reader = selCon.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        result.Add(new OneCOrderProduct()
                        {
                            LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
                            Name = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
                            Price = (reader.GetValue(2) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(2).ToString()) : 0,
                            Quantity = (reader.GetValue(3) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(3).ToString()) : 0,
                            DiscountPercent = (reader.GetValue(4) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(4).ToString()) : 0,
                            DiscountAmount = (reader.GetValue(5) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(5).ToString()) : 0,
                            TotalAmount = (reader.GetValue(6) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(6).ToString()) : 0,
                            Unit = (reader.GetValue(7) != System.DBNull.Value) ? (string)reader.GetValue(7).ToString() : "",
                            ProductId = (reader.GetValue(8) != System.DBNull.Value) ? Guid.Parse(reader.GetValue(8).ToString()) : Guid.Empty,
                            OrderId = (reader.GetValue(9) != System.DBNull.Value) ? Guid.Parse(reader.GetValue(9).ToString()) : Guid.Empty,
                        });
                    }
                }
            }
            return result;
        }

        public override List<OneCOrderProduct> GetItem(SearchFilter searchFilter)
        {
            var result = new List<OneCOrderProduct>();
            var date = DateTime.Now;

            var selCon = new Select(UserConnection)
                    .Column("OrderProduct", "Id")
                    .Column("OrderProduct", "Name")
                    .Column("OrderProduct", "Price")
                    .Column("OrderProduct", "Quantity")
                    .Column("OrderProduct", "DiscountPercent")
                    .Column("OrderProduct", "DiscountAmount")
                    .Column("OrderProduct", "TotalAmount")
                    .Column("Unit", "Name").As("Unit")
                    .Column("OrderProduct", "ProductId")
                    .Column("OrderProduct", "OrderId")
                    .From("OrderProduct")
                    .LeftOuterJoin("Unit").On("OrderProduct", "UnitId").IsEqual("Unit", "Id")
                as Select;

            selCon = base.GetItemByFilters(selCon, searchFilter);

            using (var dbExecutor = UserConnection.EnsureDBConnection())
            {
                using (var reader = selCon.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        result.Add(new OneCOrderProduct()
                        {
                            LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
                            Name = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
                            Price = (reader.GetValue(2) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(2).ToString()) : 0,
                            Quantity = (reader.GetValue(3) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(3).ToString()) : 0,
                            DiscountPercent = (reader.GetValue(4) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(4).ToString()) : 0,
                            DiscountAmount = (reader.GetValue(4) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(4).ToString()) : 0,
                            TotalAmount = (reader.GetValue(5) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(5).ToString()) : 0,
                            Unit = (reader.GetValue(6) != System.DBNull.Value) ? (string)reader.GetValue(6).ToString() : "",
                            ProductId = (reader.GetValue(7) != System.DBNull.Value) ? Guid.Parse(reader.GetValue(7).ToString()) : Guid.Empty,
                            OrderId = (reader.GetValue(7) != System.DBNull.Value) ? Guid.Parse(reader.GetValue(7).ToString()) : Guid.Empty,
                        });
                    }
                }
            }
            return result;
        }
    }
}
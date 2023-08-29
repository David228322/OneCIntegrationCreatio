namespace Terrasoft.Configuration.GenOneCInvoiceProduct
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

    using Terrasoft.Configuration.GenIntegrationLogHelper;
    using Terrasoft.Configuration.GenOneCSvcIntegration;
    using Terrasoft.Configuration.GenOneCIntegrationHelper;
    using Terrasoft.Configuration.OneCBaseEntity;

    [DataContract]
    public class OneCInvoiceProduct : OneCBaseEntity<OneCInvoiceProduct>
    {
        [DataMember(Name = "InvoiceId")]
        public Guid InvoiceId { get; set; }
        [DataMember(Name = "ProductLocalId")]
        public string ProductLocalId { get; set; }


        [DataMember(Name = "Price")]
        public decimal Price { get; set; }
        [DataMember(Name = "Currency")]
        public string Currency { get; set; }
        [DataMember(Name = "Quantity")]
        public decimal Quantity { get; set; }
        [DataMember(Name = "Unit")]
        public string Unit { get; set; }
        [DataMember(Name = "DiscountPercent")]
        public decimal DiscountPercent { get; set; }
        [DataMember(Name = "DiscountAmount")]
        public decimal DiscountAmount { get; set; }
        [DataMember(Name = "TotalAmount")]
        public decimal TotalAmount { get; set; }

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

            var selectQuery = new Select(UserConnection)
                .Column("InvoiceProduct", "Id").Top(1)
                .From("InvoiceProduct").As("InvoiceProduct")
                .Where("InvoiceProduct", "InvoiceId").IsEqual(Column.Parameter(this.InvoiceId))
            as Select;

            return base.ResolveRemoteItemByQuery(selectQuery);
        }

        public override bool SaveRemoteItem()
        {
            bool success = false;
            var oneCHelper = new OneCIntegrationHelper();

            if (!string.IsNullOrEmpty(this.ProductLocalId) && this.ProductLocalId != "00000000-0000-0000-0000-000000000000")
            {
                if (oneCHelper.ÑhekId("Product", this.ProductLocalId))
                {
                    Guid unit = Guid.Empty;
                    Guid currency = Guid.Empty;

                    if (!string.IsNullOrEmpty(this.Unit))
                    {
                        unit = oneCHelper.GetId("Unit", this.Unit);
                    }

                    if (!string.IsNullOrEmpty(this.Currency))
                    {
                        if (this.Currency == "ãðí")
                            this.Currency = "UAH";

                        currency = oneCHelper.GetId("Currency", this.Currency, "ShortName");
                    }

                    var entity = UserConnection.EntitySchemaManager
                        .GetInstanceByName("InvoiceProduct").CreateEntity(UserConnection);
                    var now = DateTime.Now;

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

                    entity.SetColumnValue("ProductId", new Guid(this.ProductLocalId));
                    entity.SetColumnValue("InvoiceId", this.InvoiceId);

                    if (oneCHelper.ÑhekId("Warehouse", this.WarehouseLocalId))
                    {
                        entity.SetColumnValue("GenWarehouseId", new Guid(this.WarehouseLocalId));
                    }

                    if (this.Price > 0)
                    {
                        entity.SetColumnValue("Price", this.Price);
                    }

                    if (currency != Guid.Empty)
                    {
                        entity.SetColumnValue("GenCurrencyId", currency);
                    }

                    if (this.Quantity > 0)
                    {
                        entity.SetColumnValue("Quantity", this.Quantity);
                    }

                    if (unit != Guid.Empty)
                    {
                        entity.SetColumnValue("UnitId", unit);
                    }

                    if (this.DiscountPercent > 0)
                    {
                        entity.SetColumnValue("DiscountPercent", this.DiscountPercent);
                    }

                    if (this.DiscountAmount > 0)
                    {
                        entity.SetColumnValue("DiscountAmount", this.DiscountAmount);
                    }

                    if (this.TotalAmount > 0)
                    {
                        entity.SetColumnValue("TotalAmount", this.TotalAmount);
                    }

                    if (entity.StoringState == StoringObjectState.Changed || this.BpmId == Guid.Empty)
                    {
                        entity.SetColumnValue("ModifiedOn", now);
                        success = entity.Save(true);
                    }
                    else
                    {
                        success = true;
                    }
                }
            }
            return success;
        }

        public override List<OneCInvoiceProduct> GetItem(SearchFilter searchFilter)
        {
            List<OneCInvoiceProduct> result = new List<OneCInvoiceProduct>();
            Select selCol = new Select(UserConnection)
                .Column("InvoiceProduct", "Id")
                .Column("InvoiceProduct", "GenID1C")
                .Column("InvoiceProduct", "InvoiceId")
                .Column("InvoiceProduct", "ProductId")
                .Column("InvoiceProduct", "Price")
                .Column("InvoiceProduct", "Quantity")
                .Column("Unit", "Name")
                .Column("InvoiceProduct", "DiscountPercent")
                .Column("InvoiceProduct", "DiscountAmount")
                .Column("InvoiceProduct", "TotalAmount")
                .Column("Currency", "ShortName")
                .From("InvoiceProduct")
                .LeftOuterJoin("Unit")
                    .On("Unit", "Id").IsEqual("InvoiceProduct", "UnitId")
                .LeftOuterJoin("Currency")
                    .On("Currency", "Id").IsEqual("InvoiceProduct", "GenCurrencyId")
            as Select;

            if (searchFilter != null)
            {
                selCol = base.GetItemByFilters(selCol, searchFilter);
            }

            using (var dbExecutor = UserConnection.EnsureDBConnection())
            {
                using (var reader = selCol.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        result.Add(new OneCInvoiceProduct()
                        {
                            LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
                            Id1C = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
                            InvoiceId = (reader.GetValue(2) != System.DBNull.Value) ? (Guid)reader.GetValue(2) : Guid.Empty,
                            ProductLocalId = (reader.GetValue(3) != System.DBNull.Value) ? (string)reader.GetValue(3).ToString() : "",
                            Price = (reader.GetValue(4) != System.DBNull.Value) ? (decimal)reader.GetValue(4) : 0,
                            Quantity = (reader.GetValue(5) != System.DBNull.Value) ? (decimal)reader.GetValue(5) : 0,
                            Unit = (reader.GetValue(6) != System.DBNull.Value) ? (string)reader.GetValue(6) : "",
                            DiscountPercent = (reader.GetValue(7) != System.DBNull.Value) ? (decimal)reader.GetValue(7) : 0,
                            DiscountAmount = (reader.GetValue(8) != System.DBNull.Value) ? (decimal)reader.GetValue(8) : 0,
                            TotalAmount = (reader.GetValue(9) != System.DBNull.Value) ? (decimal)reader.GetValue(9) : 0,
                            Currency = (reader.GetValue(10) != System.DBNull.Value) ? (string)reader.GetValue(10) : "",
                        });
                    }
                }
            }
            return result;
        }

        public List<OneCInvoiceProduct> GetItem(string invoiceId)
        {
            List<OneCInvoiceProduct> result = new List<OneCInvoiceProduct>();
            Select selCol = new Select(UserConnection)
                .Column("InvoiceProduct", "Id")
                .Column("InvoiceProduct", "GenID1C")
                .Column("InvoiceProduct", "InvoiceId")
                .Column("InvoiceProduct", "ProductId")
                .Column("InvoiceProduct", "Price")
                .Column("InvoiceProduct", "Quantity")
                .Column("Unit", "Name")
                .Column("InvoiceProduct", "DiscountPercent")
                .Column("InvoiceProduct", "DiscountAmount")
                .Column("InvoiceProduct", "TotalAmount")
                .From("InvoiceProduct")
                .LeftOuterJoin("Unit")
                    .On("Unit", "Id").IsEqual("InvoiceProduct", "UnitId")
                .Where("InvoiceProduct", "InvoiceId").IsEqual(Column.Parameter(new Guid(invoiceId)))
            as Select;

            using (var dbExecutor = UserConnection.EnsureDBConnection())
            {
                using (var reader = selCol.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        result.Add(new OneCInvoiceProduct()
                        {
                            LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
                            Id1C = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
                            InvoiceId = (reader.GetValue(2) != System.DBNull.Value) ? (Guid)reader.GetValue(2) : Guid.Empty,
                            ProductLocalId = (reader.GetValue(3) != System.DBNull.Value) ? (string)reader.GetValue(3).ToString() : "",
                            Price = (reader.GetValue(4) != System.DBNull.Value) ? (decimal)reader.GetValue(4) : 0,
                            Quantity = (reader.GetValue(5) != System.DBNull.Value) ? (decimal)reader.GetValue(5) : 0,
                            Unit = (reader.GetValue(6) != System.DBNull.Value) ? (string)reader.GetValue(6) : "",
                            DiscountPercent = (reader.GetValue(7) != System.DBNull.Value) ? (decimal)reader.GetValue(7) : 0,
                            DiscountAmount = (reader.GetValue(8) != System.DBNull.Value) ? (decimal)reader.GetValue(8) : 0,
                            TotalAmount = (reader.GetValue(9) != System.DBNull.Value) ? (decimal)reader.GetValue(9) : 0,
                        });
                    }
                }
            }
            return result;
        }
    }
}
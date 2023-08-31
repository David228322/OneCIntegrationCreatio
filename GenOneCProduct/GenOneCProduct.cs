namespace Terrasoft.Configuration.GenOneCProduct
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
    using Terrasoft.Configuration.GenCOrderProduct;
    using Terrasoft.Configuration.GenOneCIntegrationHelper;

    [DataContract]
    public sealed class OneCProduct : OneCBaseEntity<OneCProduct>
    {
        [DataMember(Name = "Name")]
        public string Name { get; set; }
        [DataMember(Name = "Description")]
        public string Description { get; set; }
        [DataMember(Name = "ShortDescription")]
        public string ShortDescription { get; set; }
        [DataMember(Name = "Price")]
        public decimal Price { get; set; }
        [DataMember(Name = "Unit")]
        public string Unit { get; set; }
        [DataMember(Name = "Type")]
        public string Type { get; set; }
        [DataMember(Name = "Currency")]
        public string Currency { get; set; }

        public string ProcessRemoteItem(bool isFull = true)
        {
            return base.ProcessRemoteItem(isFull);
        }

        public override bool ResolveRemoteItem()
        {
            var selEntity = new Select(UserConnection)
                .Column("Product", "Id").Top(1)
                .From("Product").As("Product")
            as Select;

            return base.ResolveRemoteItemByQuery(selEntity);
        }

        public override bool SaveRemoteItem()
        {
            var success = false;
            var oneCHelper = new OneCIntegrationHelper();
            var unit = Guid.Empty;
            var residueStorageUnit = Guid.Empty;
            var reportingUnit = Guid.Empty;
            var unitOfSeats = Guid.Empty;
            var type = Guid.Empty;
            var nomenclatureGroup = Guid.Empty;
            var responsibleForPurchases = Guid.Empty;

            if (!string.IsNullOrEmpty(Unit))
            {
                unit = oneCHelper.GetId("Unit", Unit);
            }

            if (!string.IsNullOrEmpty(Type))
            {
                type = oneCHelper.GetId("ProductType", Type);
            }

            var entity = UserConnection.EntitySchemaManager
                .GetInstanceByName("Product").CreateEntity(UserConnection);
            var now = DateTime.Now;

            if (BpmId == Guid.Empty)
            {
                entity.SetDefColumnValues();
            }
            else if (!entity.FetchFromDB(entity.Schema.PrimaryColumn.Name, BpmId))
            {
                entity.SetDefColumnValues();
            }

            if (!string.IsNullOrEmpty(Id1C))
            {
                entity.SetColumnValue("GenID1C", Id1C);
            }

            if (!string.IsNullOrEmpty(Name))
            {
                entity.SetColumnValue("Name", Name);
            }

            if (unit != Guid.Empty)
            {
                entity.SetColumnValue("UnitId", unit);
            }

            if (type != Guid.Empty)
            {
                entity.SetColumnValue("TypeId", type);
            }

            entity.SetColumnValue("ModifiedOn", now);

            if (entity.StoringState == StoringObjectState.Changed || BpmId == Guid.Empty)
            {
                success = entity.Save(true);
            }
            else
            {
                success = true;
            }
            BpmId = (Guid)entity.GetColumnValue("Id");

            return success;
        }

        public override List<OneCProduct> GetItem(SearchFilter searchFilter)
        {
            var result = new List<OneCProduct>();
            var date = DateTime.Now;

            var selCon = new Select(UserConnection)
                .Column("Product", "Id")
                .Column("Product", "GenID1C")
                .Column("Product", "Name")
                .Column("Product", "Description")
                .Column("Product", "ShortDescription")
                .Column("Product", "Price")
                .Column("ProductType", "Name").As("ProductType")
                .Column("Currency", "Name").As("Currency")
                .Column("Unit", "Name").As("Unit")
                .From("Product")
                .LeftOuterJoin("ProductType").On("Product", "TypeId").IsEqual("ProductType", "Id")
                .LeftOuterJoin("Currency").On("Product", "CurrencyId").IsEqual("Currency", "Id")
                .LeftOuterJoin("Unit").On("Product", "UnitId").IsEqual("Unit", "Id")
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
                            result.Add(new OneCProduct()
                            {
                                LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
                                Id1C = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
                                Name = (reader.GetValue(2) != System.DBNull.Value) ? (string)reader.GetValue(2) : "",
                                Description = (reader.GetValue(3) != System.DBNull.Value) ? (string)reader.GetValue(3) : "",
                                ShortDescription = (reader.GetValue(4) != System.DBNull.Value) ? (string)reader.GetValue(4).ToString() : "",
                                Price = (reader.GetValue(5) != System.DBNull.Value) ? (decimal)reader.GetValue(5) : 0,
                                Type = (reader.GetValue(6) != System.DBNull.Value) ? (string)reader.GetValue(6).ToString() : "",
                                Currency = (reader.GetValue(7) != System.DBNull.Value) ? (string)reader.GetValue(7).ToString() : "",
                                Unit = (reader.GetValue(8) != System.DBNull.Value) ? (string)reader.GetValue(8).ToString() : "",
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

        public void SetAssociatedProduct(Guid productId)
        {
            var selEntity = new Select(UserConnection)
                .Column("Id").Top(1)
                .From("GenAssociateProduct")
                .Where("GenProductId").IsEqual(Column.Parameter(productId))
                .And("GenAssociatedProductId").IsEqual(Column.Parameter(BpmId))
            as Select;
            var ap = selEntity.ExecuteScalar<Guid>();

            if (ap != Guid.Empty) return;
            var entity = UserConnection.EntitySchemaManager
                .GetInstanceByName("GenAssociateProduct").CreateEntity(UserConnection);

            entity.SetDefColumnValues();

            entity.SetColumnValue("GenProductId", productId);
            entity.SetColumnValue("GenAssociatedProductId", BpmId);
            entity.Save(true);
        }
    }
}
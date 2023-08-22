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

    [DataContract]
    public class OneCProduct : OneCBaseEntity<OneCProduct>
    {
        [DataMember(Name = "LangCode")]
        public string LangCode { get; set; }

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
            if (string.IsNullOrEmpty(LocalId) && string.IsNullOrEmpty(Id1C))
                return false;
            var selEntity = new Select(UserConnection)
                .Column("Product", "Id").Top(1)
                .From("Product").As("Product")
            as Select;
            if (!string.IsNullOrEmpty(LocalId))
                selEntity = selEntity.Where("Product", "Id").IsEqual(Column.Parameter(new Guid(LocalId))) as Select;
            else
                return false;

            var entityId = selEntity.ExecuteScalar<Guid>();
            if (entityId == Guid.Empty) return false;
            BpmId = entityId;
            return true;
        }

        public override bool SaveRemoteItem()
        {
            var success = false;
            var directory = new Directory();
            var unit = Guid.Empty;
            var residueStorageUnit = Guid.Empty;
            var reportingUnit = Guid.Empty;
            var unitOfSeats = Guid.Empty;
            var type = Guid.Empty;
            var nomenclatureGroup = Guid.Empty;
            var responsibleForPurchases = Guid.Empty;

            if (!string.IsNullOrEmpty(Unit))
            {
                unit = directory.GetId("Unit", Unit);
            }

            if (!string.IsNullOrEmpty(Type))
            {
                type = directory.GetId("ProductType", Type);
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
                var localizableId1C = new LocalizableString();
                localizableId1C.SetCultureValue(new CultureInfo(LangCode), Id1C);
                entity.SetColumnValue("GenID1C", localizableId1C);
            }

            if (!string.IsNullOrEmpty(Name))
            {
                var localizableString = new LocalizableString();
                localizableString.SetCultureValue(new CultureInfo(LangCode), Name);
                entity.SetColumnValue("Name", localizableString);
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

        public override List<OneCProduct> GetItem(Search data)
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

            if (!string.IsNullOrEmpty(data.Id1C))
            {
                selCon = selCon.Where("Product", "GenID1C").IsEqual(Column.Parameter(data.Id1C)) as Select;
            }
            else if (!string.IsNullOrEmpty(data.LocalId))
            {
                selCon = selCon.Where("Product", "Id").IsEqual(Column.Parameter(new Guid(data.LocalId))) as Select;
            }
            else if (!string.IsNullOrEmpty(data.CreatedFrom) || !string.IsNullOrEmpty(data.CreatedTo))
            {
                if (!string.IsNullOrEmpty(data.CreatedFrom))
                {
                    selCon = selCon.Where("Product", "CreatedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(data.CreatedFrom))) as Select;
                }
                if (!string.IsNullOrEmpty(data.CreatedTo))
                {
                    selCon = selCon.And("Product", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.CreatedTo))) as Select;
                }
            }
            else if (!string.IsNullOrEmpty(data.ModifiedFrom) || !string.IsNullOrEmpty(data.ModifiedTo))
            {
                if (!string.IsNullOrEmpty(data.ModifiedFrom))
                {
                    selCon = selCon.Where("Product", "ModifiedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedFrom))) as Select;
                }
                if (!string.IsNullOrEmpty(data.ModifiedTo))
                {
                    selCon = selCon.And("Product", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedTo))) as Select;
                }
            }

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
                                Price = (reader.GetValue(4) != System.DBNull.Value) ? (decimal)reader.GetValue(5) : 0,
                                Type = (reader.GetValue(4) != System.DBNull.Value) ? (string)reader.GetValue(7).ToString() : "",
                                Currency = (reader.GetValue(4) != System.DBNull.Value) ? (string)reader.GetValue(8).ToString() : "",
                                Unit = (reader.GetValue(4) != System.DBNull.Value) ? (string)reader.GetValue(6).ToString() : "",
                            });
                        }
                    }
                }
                catch (global::System.Exception ex)
                {                    
                    throw;
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

    [DataContract]
    public class OneCOrderProduct
    {
        [DataMember(Name = "Id")]
        public string Id1C { get; set; }
        [DataMember(Name = "BPMId")]
        public string LocalId { get; set; }
        [IgnoreDataMember]
        public Guid BpmId { get; set; }

        [DataMember(Name = "Id")]
        public string Id { get; set; }
        [DataMember(Name = "Name")]
        public string Name { get; set; }
        [DataMember(Name = "Price")]
        public decimal Price { get; set; }
        [DataMember(Name = "Quantity")]
        public decimal Quantity { get; set; }
        [DataMember(Name = "DiscountPercent")]
        public decimal DiscountPercent { get; set; }
        [DataMember(Name = "TotalAmount")]
        public decimal TotalAmount { get; set; }
        [DataMember(Name = "Unit")]
        public string Unit { get; set; }

        public Guid ProductId { get; set; }
        public OneCProduct OneCProduct { get; set; }

        [DataMember(Name = "CreatedOn")]
        public string CreatedOn { get; set; }
        [DataMember(Name = "ModifiedOn")]
        public string ModifiedOn { get; set; }


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

        public string ProcessRemoteItem(bool isFull = true)
        {
            if ((string.IsNullOrEmpty(LocalId) || LocalId == "00000000-0000-0000-0000-000000000000"))
            {
                return BpmId.ToString();
            }
            if (BpmId == Guid.Empty)
            {
                ResolveRemoteItem();
            }
            if (BpmId == Guid.Empty || isFull)
            {
                // SaveRemoteItem();
            }
            return BpmId.ToString();
        }

        public bool ResolveRemoteItem()
        {
            if (string.IsNullOrEmpty(LocalId) && string.IsNullOrEmpty(Id1C))
                return false;
            var selEntity = new Select(UserConnection)
                .Column("OrderProduct", "Id").Top(1)
                .From("Product")
            as Select;
            if (!string.IsNullOrEmpty(LocalId))
                selEntity = selEntity.Where("OrderProduct", "Id").IsEqual(Column.Parameter(new Guid(LocalId))) as Select;
            else
                return false;

            var entityId = selEntity.ExecuteScalar<Guid>();
            if (entityId == Guid.Empty) return false;
            BpmId = entityId;
            return true;
        }
    }
}
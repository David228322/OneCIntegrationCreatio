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

    using Terrasoft.Configuration.GenOneCProductStockBalance;
    using Terrasoft.Configuration.GenOneCSpecificationInProduct;

    [DataContract]
    public sealed class OneCProduct : OneCBaseEntity<OneCProduct>
    {
        [DataMember(Name = "Name")]
        [DatabaseColumn("Product", nameof(Name))]
        public string Name { get; set; }

        [DataMember(Name = "Description")]
        [DatabaseColumn("Product", nameof(Description))]
        public string Description { get; set; }

        [DataMember(Name = "ShortDescription")]
        [DatabaseColumn("Product", nameof(ShortDescription))]
        public string ShortDescription { get; set; }

        [DataMember(Name = "Price")]
        [DatabaseColumn("Product", nameof(Price))]
        public decimal Price { get; set; }

        [DataMember(Name = "Unit")]
        [DatabaseColumn("Unit", "Name", "UnitId")]
        public string Unit { get; set; }

        [DataMember(Name = "Type")]
        [DatabaseColumn("ProductType", "Name", "TypeId")]
        public string Type { get; set; }

        [DataMember(Name = "Currency")]
        [DatabaseColumn("Currency", "Name", "CurrencyId")]
        public string Currency { get; set; }

        [DataMember(Name = "Specification")]
        public List<OneCSpecificationInProduct> Specification { get; set; }
        [DataMember(Name = "StockBalance")]
        public List<OneCProductStockBalance> StockBalance { get; set; }

        public OneCBaseEntity<OneCProduct> ProcessRemoteItem(bool isFull = true)
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
            base.SaveToDatabase();
            return true;

            if (this.BpmId != Guid.Empty)
            {
                if (this.StockBalance != null && this.StockBalance.Count > 0)
                {
                    foreach (var stock in this.StockBalance)
                    {
                        stock.ProductId = this.BpmId;
                        stock.ProcessRemoteItem();
                    }
                }

                if (this.Specification != null && this.Specification.Count > 0)
                {
                    foreach (var spec in this.Specification)
                    {
                        spec.ProductId = this.BpmId;
                        spec.ProcessRemoteItem();
                    }
                }
            }
        }

        public override List<OneCProduct> GetItem(SearchFilter searchFilter)
        {
            var products = base.GetFromDatabase(searchFilter);

            var productSpecification = new OneCSpecificationInProduct();
            foreach (var product in products)
            {
                product.Specification = productSpecification.GetItem(product.LocalId);
            }

            var stockBalance = new OneCProductStockBalance();
            foreach (var product in products)
            {
                product.StockBalance = stockBalance.GetItem(product.LocalId);
            }

            return products;
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
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
    using Terrasoft.Configuration.GenOneCIntegrationHelper;

    [DataContract]
    public sealed class OneCOrderProduct : OneCBaseEntity<OneCOrderProduct>
    {
        [DataMember(Name = "Name")]
        [DatabaseColumn("OrderProduct", "Name")]
        public string Name { get; set; }

        [DatabaseColumn("OrderProduct", "Price")]
        [DataMember(Name = "Price")]
        public decimal Price { get; set; }

        [DataMember(Name = "Quantity")]
        [DatabaseColumn("OrderProduct", "Quantity")]
        public decimal Quantity { get; set; }

        [DataMember(Name = "DiscountPercent")]
        [DatabaseColumn("OrderProduct", "DiscountPercent")]
        public decimal DiscountPercent { get; set; }

        [DataMember(Name = "DiscountAmount")]
        [DatabaseColumn("OrderProduct", "DiscountAmount")]
        public decimal DiscountAmount { get; set; }

        [DataMember(Name = "TotalAmount")]
        [DatabaseColumn("OrderProduct", "TotalAmount")]
        public decimal TotalAmount { get; set; }

        [DataMember(Name = "Unit")]
        [DatabaseColumn("Unit", "Name", "UnitId")]
        public string Unit { get; set; }

        [DataMember(Name = "ProductLocalId")]
        [DatabaseColumn("OrderProduct", "ProductId")]
        public Guid ProductId { get; set; }

        [DataMember(Name = "OrderId")]
        [DatabaseColumn("OrderProduct", "OrderId")]
        public Guid OrderId { get; set; }

        public OneCBaseEntity<OneCOrderProduct> ProcessRemoteItem(bool isFull = true)
        {
            return base.ProcessRemoteItem(isFull);
        }

        public override bool ResolveRemoteItem()
        {
            var selectQuery = new Select(UserConnection)
                .Column("OrderProduct", "Id").Top(1)
                .From("OrderProduct")
                .Where("OrderProduct", "OrderId").IsEqual(Column.Parameter(OrderId)) as Select;

            return base.ResolveRemoteItemByQuery(selectQuery);
        }

        public override bool SaveRemoteItem()
        {
            return base.SaveToDatabase();
        }

        public List<OneCOrderProduct> GetItem(string orderId)
        {
            return base.GetFromDatabase(null, new Dictionary<string,string>() {{"OrderId", orderId}});
        }

        public override List<OneCOrderProduct> GetItem(SearchFilter searchFilter)
        {
            return base.GetFromDatabase(searchFilter);
        }
    }
}
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
    public sealed class OneCInvoiceProduct : OneCBaseEntity<OneCInvoiceProduct>
    {
        [DataMember(Name = "InvoiceId")]
        [DatabaseColumn("InvoiceProduct", nameof(InvoiceId))]
        public Guid InvoiceId { get; set; }

        [DataMember(Name = "ProductLocalId")]
        [DatabaseColumn("InvoiceProduct", "ProductId")]
        public Guid ProductId { get; set; }

        [DataMember(Name = "Price")]
        [DatabaseColumn("InvoiceProduct", nameof(Price))]
        public decimal Price { get; set; }

        [DataMember(Name = "Quantity")]
        [DatabaseColumn("InvoiceProduct", nameof(Quantity))]
        public decimal Quantity { get; set; }

        [DataMember(Name = "Unit")]
        [DatabaseColumn("Unit", "Name", "UnitId")]
        public string Unit { get; set; }

        [DataMember(Name = "DiscountPercent")]
        [DatabaseColumn("InvoiceProduct", nameof(DiscountPercent))]
        public decimal DiscountPercent { get; set; }

        [DataMember(Name = "DiscountAmount")]
        [DatabaseColumn("InvoiceProduct", nameof(DiscountAmount))]
        public decimal DiscountAmount { get; set; }

        [DataMember(Name = "TotalAmount")]
        [DatabaseColumn("InvoiceProduct", nameof(TotalAmount))]
        public decimal TotalAmount { get; set; }

        public OneCBaseEntity<OneCInvoiceProduct> ProcessRemoteItem(bool isFull = true)
        {
            return base.ProcessRemoteItem(isFull);
        }

        public override bool ResolveRemoteItem()
        {
            var selectQuery = new Select(UserConnection)
                .Column("InvoiceProduct", "Id").Top(1)
                .From("InvoiceProduct")
                .Where("InvoiceProduct", "InvoiceId").IsEqual(Column.Parameter(this.InvoiceId))
            as Select;

            return base.ResolveRemoteItemByQuery(selectQuery);
        }

        public override bool SaveRemoteItem()
        {
            base.SaveToDatabase();

            return true;
        }

        public override List<OneCInvoiceProduct> GetItem(SearchFilter searchFilter)
        {
            var result = base.GetFromDatabase(searchFilter);
            return result;
        }

        public List<OneCInvoiceProduct> GetItem(string invoiceId)
        {
            return base.GetFromDatabase(null, new Dictionary<string, string>() { { "InvoiceId", invoiceId } });
        }
    }
}
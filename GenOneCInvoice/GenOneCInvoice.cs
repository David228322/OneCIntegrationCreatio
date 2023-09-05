namespace Terrasoft.Configuration.GenOneCInvoice
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
    using Terrasoft.Configuration.GenOneCInvoiceProduct;

    [DataContract]
    public sealed class OneCInvoice : OneCBaseEntity<OneCInvoice>
    {
        [DataMember(Name = "Number")]
        [DatabaseColumn("Invoice", nameof(Number))]
        public string Number { get; set; }

        [DataMember(Name = "StartDate")]
        [DatabaseColumn("Invoice", nameof(StartDate))]
        public string StartDate { get; set; }

        [DataMember(Name = "Notes")]
        [DatabaseColumn("Invoice", nameof(Notes))]
        public string Notes { get; set; }

        [DataMember(Name = "DueDate")]
        [DatabaseColumn("Invoice", nameof(DueDate))]
        public string DueDate { get; set; }

        [DataMember(Name = "Amount")]
        [DatabaseColumn("Invoice", nameof(Amount))]
        public decimal Amount { get; set; }

        [DataMember(Name = "AmountWithoutTax")]
        [DatabaseColumn("Invoice", nameof(AmountWithoutTax))]
        public decimal AmountWithoutTax { get; set; }


        [DataMember(Name = "ContractLocalId")]
        [DatabaseColumn("Invoice", "ContractId")]
        public Guid ContractLocalId { get; set; }

        [DataMember(Name = "AccountLocalId")]
        [DatabaseColumn("Invoice", "AccountId")]
        public Guid AccountLocalId { get; set; }

        [DataMember(Name = "OrderLocalId")]
        [DatabaseColumn("Invoice", "OrderId")]
        public Guid OrderLocalId { get; set; }

        [DataMember(Name = "OwnerLocalId")]
        [DatabaseColumn("Invoice", "OwnerId")]
        public Guid OwnerLocalId { get; set; }

        [DataMember(Name = "Currency")]
        [DatabaseColumn("Currency", "ShortName", "CurrencyId")]
        public string Currency { get; set; }

        [DataMember(Name = "Products")]
        public List<OneCInvoiceProduct> Products { get; set; }

        public OneCBaseEntity<OneCInvoice> ProcessRemoteItem(bool isFull = true)
        {
            return base.ProcessRemoteItem(isFull);
        }

        public override bool ResolveRemoteItem()
        {
            Select selectQuery = new Select(UserConnection)
                .Column("Invoice", "Id").Top(1)
                .From("Invoice").As("Invoice")
                as Select;

            return base.ResolveRemoteItemByQuery(selectQuery);
        }


        public override bool SaveRemoteItem()
        {
             base.SaveToDatabase();
             if (this.BpmId != Guid.Empty)
             {
                 if (this.Products != null && this.Products.Count > 0)
                 {
                    var oneCHelper = new OneCIntegrationHelper();
                     List<string> products = oneCHelper.GetList(this.BpmId.ToString(), "InvoiceId", "GenID1C", "InvoiceProduct");
                     if (products != null && products.Count > 0)
                     {
                         foreach (string productId in products)
                         {

                             if (this.Products.Exists(x => x.Id1C == productId) == false)
                             {
                                 oneCHelper.DelItem(productId, "GenID1C", this.BpmId.ToString(), "InvoiceId", "InvoiceProduct");
                             }
                         }
                     }
                     foreach (var product in this.Products)
                     {
                         product.InvoiceId = this.BpmId;
                         product.ProcessRemoteItem();
                     }
                 }            
             }
            return true;
        }

        public override List<OneCInvoice> GetItem(SearchFilter searchFilter)
        {
            var result = base.GetFromDatabase(searchFilter);

            OneCInvoiceProduct invoiceProducts = new OneCInvoiceProduct();
            foreach (var inv in result)
            {
                inv.Products = invoiceProducts.GetItem(inv.LocalId);
            }
            
            return result;
        }
    }
}
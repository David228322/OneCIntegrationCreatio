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
    using Terrasoft.Configuration.GenOneCIntegrationHelper;

    [DataContract]
    public sealed class OneCOrder : OneCBaseEntity<OneCOrder>
    { 
        [DataMember(Name = "PrimaryAmount")]
        [DatabaseColumn("Order", "PrimaryAmount")]
        public decimal PrimaryAmount { get; set; }

        [DataMember(Name = "PaymentAmount")]
        [DatabaseColumn("Order", "PaymentAmount")]
        public decimal PaymentAmount { get; set; }

        [DataMember(Name = "Amount")]
        [DatabaseColumn("Order", "Amount")]
        public decimal Amount { get; set; }

        [DataMember(Name = "Number")]
        [DatabaseColumn("Order", "Number")]
        public string Number { get; set; }

        [DataMember(Name = "DeliveryAddress")]
        [DatabaseColumn("Order", "DeliveryAddress")]
        public string DeliveryAddress { get; set; }

        [DataMember(Name = "OrderStatus")]
        [DatabaseColumn("OrderStatus", "Name", "StatusId")]
        public string OrderStatus { get; set; }

        [DataMember(Name = "Comment")]
        [DatabaseColumn("Order", "Comment")]
        public string Comment { get; set; }

        [DataMember(Name = "AccountId")]
        [DatabaseColumn("Order", "AccountId")]
        public Guid AccountId { get; set; }

        [DataMember(Name = "ContactId")]
        [DatabaseColumn("Order", "ContactId")]
        public Guid ContactId { get; set; }

        [DataMember(Name = "OrderProducts")]
        public List<OneCOrderProduct> OrderProducts { get; set; }
        [IgnoreDataMember]
        public List<OneCProduct> OneCProducts { get; set; } = new List<OneCProduct>();

        public OneCBaseEntity<OneCOrder> ProcessRemoteItem(bool isFull = true)
        {
            return base.ProcessRemoteItem(isFull);
        }

        public override bool ResolveRemoteItem()
        {
            var selEntity = new Select(UserConnection)
                .Column("Order", "Id").Top(1)
                .From("Order")
                as Select;

            return base.ResolveRemoteItemByQuery(selEntity);
        }

        public override bool SaveRemoteItem()
        {
            bool success = false;
            var oneCHelper = new OneCIntegrationHelper();
            Guid contactId = Guid.Empty;
            Guid accountId = Guid.Empty;
            /*
            if (!string.IsNullOrEmpty(this.AccountId))
            {
                accountId = oneCHelper.GetId("Account", this.AccountId);
            }

            if (!string.IsNullOrEmpty(this.ContactId))
            {
                contactId = oneCHelper.GetId("Contact", this.ContactId);
            }
            */
            var entity = UserConnection.EntitySchemaManager
                .GetInstanceByName("Order").CreateEntity(UserConnection);

            if (base.BpmId == Guid.Empty)
            {
                entity.SetDefColumnValues();
            }
            else if (!entity.FetchFromDB(entity.Schema.PrimaryColumn.Name, base.BpmId))
            {
                entity.SetDefColumnValues();
            }

            if (!string.IsNullOrEmpty(base.Id1C))
            {
                entity.SetColumnValue("GenID1C", base.Id1C);
            }

            if (!string.IsNullOrEmpty(this.Number))
            {
                entity.SetColumnValue("Number", this.Number);
            }

            if (accountId != Guid.Empty)
            {
                entity.SetColumnValue("AccountId", accountId);
            }

            if (accountId != Guid.Empty)
            {
                entity.SetColumnValue("ContactId", contactId);
            }

            if (!string.IsNullOrEmpty(this.Comment))
            {
                entity.SetColumnValue("Comment", this.Comment);
            }

            if (this.Amount > 0)
            {
                entity.SetColumnValue("Amount", this.Amount);
            }

            var now = DateTime.Now;
            if (entity.StoringState == StoringObjectState.Changed || this.BpmId == Guid.Empty)
            {
                entity.SetColumnValue("ModifiedOn", now);
                success = entity.Save(true);
            }
            else
            {
                success = true;
            }
            this.BpmId = (Guid)entity.GetColumnValue("Id");
            this.ModifiedOn = now.ToString();

            /*
            if (this.BPMId != Guid.Empty)
            {
                if (this.Products != null && this.Products.Count > 0)
                {
                    List<string> products = oneCHelper.GetList(this.BpmId.ToString(), "OrderId", "GenID1C", "OrderProduct");
                    if (products != null && products.Count > 0)
                    {
                        foreach (string productId in products)
                        {

                            if (this.Products.Exists(x => x.Id1C == productId) == false)
                            {
                                oneCHelper.delItem(productId, "GenID1C", this.BPMId.ToString(), "OrderId", "OrderProduct");
                            }
                        }
                    }

                    foreach (var product in this.Products)
                    {
                        product.OrderId = this.BPMId;
                        product.ProcessRemoteItem();
                    }
                }

                if (this.AdditionalServices != null && this.AdditionalServices.Count > 0)
                {
                    List<string> _additionServices = oneCHelper.GetList(this.BPMId.ToString(), "GenOrderId", "GenID1C", "GenAdditionalServices");
                    if (_additionServices != null && _additionServices.Count > 0)
                    {
                        foreach (string _additionServiceId in _additionServices)
                        {

                            if (this.AdditionalServices.Exists(x => x.ID1C == _additionServiceId) == false)
                            {
                                oneCHelper.delItem(_additionServiceId, "GenID1C", this.BPMId.ToString(), "GenOrderId", "GenAdditionalServices");
                            }
                        }
                    }

                    foreach (var service in this.AdditionalServices)
                    {
                        service.OrderId = this.BPMId;
                        service.ProcessRemoteItem();
                    }
                }

                if (this.AutomaticDiscount != null && this.AutomaticDiscount.Count > 0)
                {
                    foreach (var discount in this.AutomaticDiscount)
                    {
                        discount.OrderId = this.BPMId;
                        discount.ProcessRemoteItem();
                    }
                }

                if (this.OrderPaid != null && this.OrderPaid.Count > 0)
                {
                    foreach (var paid in this.OrderPaid)
                    {
                        paid.OrderLocalId = this.BPMId.ToString();
                        paid.ProcessRemoteItem();
                    }
                }
            } */

            return success;
        }

        public override List<OneCOrder> GetItem(SearchFilter searchFilter)
        {
            var result = base.GetFromDatabase(searchFilter);

            var orderProducts = new OneCOrderProduct();
            foreach (var order in result)
            {
                order.OrderProducts = orderProducts.GetItem(order.LocalId);
            } 

            return result;
        }
    }
}
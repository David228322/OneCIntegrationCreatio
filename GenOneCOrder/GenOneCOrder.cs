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
            base.SaveToDatabase();
            
            if (this.BpmId != Guid.Empty)
            {
                if (this.OrderProducts != null && this.OrderProducts.Count > 0)
                {
                    var oneCHelper = new OneCIntegrationHelper();
                    List<string> orderProducts = oneCHelper.GetList(this.BpmId.ToString(), "OrderId", "GenID1C", "OrderProduct");
                    if (orderProducts != null && orderProducts.Count > 0)
                    {
                        foreach (string productId in orderProducts)
                        {

                            if (this.OrderProducts.Exists(x => x.Id1C == productId) == false)
                            {
                                oneCHelper.DelItem(productId, "GenID1C", this.BpmId.ToString(), "OrderId", "OrderProduct");
                            }
                        }
                    }

                    foreach (var product in this.OrderProducts)
                    {
                        product.OrderId = this.BpmId;
                        product.ProcessRemoteItem();
                    }
                }  
            }
            return true;
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
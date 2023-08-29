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
    public class OneCOrder : OneCBaseEntity<OneCOrder>
    {
        [IgnoreDataMember]
        public List<OneCProduct> OneCProducts { get; set; } = new List<OneCProduct>();
        [DataMember(Name = "PrimaryAmount")]
        public decimal PrimaryAmount { get; set; }
        [DataMember(Name = "PaymentAmount")]
        public decimal PaymentAmount { get; set; }
        [DataMember(Name = "Number")]
        public string Number { get; set; }
        [DataMember(Name = "DeliveryAddress")]
        public string DeliveryAddress { get; set; }
        [DataMember(Name = "OrderStatus")]
        public string OrderStatus { get; set; }
        [DataMember(Name = "Comment")]
        public string Comment { get; set; }

        [DataMember(Name = "OrderProducts")]
        public List<OneCOrderProduct> OrderProducts { get; set; }

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

            var selEntity = new Select(UserConnection)
                .Column("Order", "Id").Top(1)
                .From("Order")
                as Select;

            if (!string.IsNullOrEmpty(this.LocalId))
            {
                selEntity = selEntity.Where("Order", "Id").IsEqual(Column.Parameter(new Guid(this.LocalId))) as Select;
            }
            else if (!string.IsNullOrEmpty(this.Id1C))
            {
                selEntity = selEntity.Where("Order", "GenID1C").IsEqual(Column.Parameter(this.Id1C)) as Select;
            }
            else
            {
                return false;
            }

            var entityId = selEntity.ExecuteScalar<Guid>();
            if (entityId == Guid.Empty)
            {
                return false;
            }

            this.BpmId = entityId;
            return true;
        }

        public override bool SaveRemoteItem()
        {     
            //TODO: Realize this method
            throw new NotImplementedException();
            /*
            bool success = false;
            var oneCHelper = new OneCIntegrationHelper();
            Guid _Country = Guid.Empty;
            Guid _Organization = Guid.Empty;
            Guid counterParty = Guid.Empty;
            Guid _Contract = Guid.Empty;
            Guid _Warehouse = Guid.Empty;
            Guid _BasisAdditionalDiscount = Guid.Empty;
            Guid _ResponsibleMRK = Guid.Empty;
            Guid _ResponsibleMAP = Guid.Empty;
            Guid _ResponsibleMAP2 = Guid.Empty;

            var _entity = UserConnection.EntitySchemaManager
                .GetInstanceByName("Order").CreateEntity(UserConnection);
            var now = DateTime.Now;

            if (base.BpmId == Guid.Empty)
            {
                _entity.SetDefColumnValues();
            }
            else if (!_entity.FetchFromDB(_entity.Schema.PrimaryColumn.Name, base.BpmId))
            {
                _entity.SetDefColumnValues();
            }

            if (!string.IsNullOrEmpty(base.Id1C))
            {
                _entity.SetColumnValue("GenID1C", base.Id1C);
            }

            if (!string.IsNullOrEmpty(this.Number))
            {
                _entity.SetColumnValue("Number", this.Number);
            }

            if (counterParty != Guid.Empty)
            {
                _entity.SetColumnValue("AccountId", counterParty);
            }

            if (!string.IsNullOrEmpty(this.Comment))
            {
                _entity.SetColumnValue("Comment", this.Comment);
            }

            if (_ResponsibleMRK != Guid.Empty)
            {
                _entity.SetColumnValue("OwnerId", _ResponsibleMRK);
            }

            if (this.Amount > 0)
            {
                _entity.SetColumnValue("Amount", this.Amount);
            }

            if (_entity.StoringState == StoringObjectState.Changed || this.BPMId == Guid.Empty)
            {
                _entity.SetColumnValue("ModifiedOn", now);
                success = _entity.Save(true);
            }
            else
            {
                success = true;
            }
            this.BPMId = (Guid)_entity.GetColumnValue("Id");
            this.DateModified = now.ToString();

            
            if (this.BPMId != Guid.Empty)
            {
                if (this.Products != null && this.Products.Count > 0)
                {
                    List<string> _products = oneCHelper.GetList(this.BPMId.ToString(), "OrderId", "GenID1C", "OrderProduct");
                    if (_products != null && _products.Count > 0)
                    {
                        foreach (string _productId in _products)
                        {

                            if (this.Products.Exists(x => x.Id1C == _productId) == false)
                            {
                                oneCHelper.delItem(_productId, "GenID1C", this.BPMId.ToString(), "OrderId", "OrderProduct");
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
            }
            
            return success;
            */
        }

        public override List<OneCOrder> GetItem(SearchFilter searchFilter)
        {
            var result = new List<OneCOrder>();
            var date = DateTime.Now;

            var selCon = new Select(UserConnection)
                .Column("Order", "Id")
                .Column("Order", "GenID1C")
                .Column("Order", "Number")
                .Column("Order", "PrimaryAmount")
                .Column("Order", "PaymentAmount")
                .Column("Order", "DeliveryAddress")
                .Column("OrderStatus", "Name").As("OrderStatusName")
                .Column("Comment")
                .From("Order")
                .LeftOuterJoin("OrderStatus").On("Order", "StatusId").IsEqual("OrderStatus", "Id")
            as Select;

            selCon = base.GetItemByFilters(selCon, searchFilter);

            using (var dbExecutor = UserConnection.EnsureDBConnection())
            {
                using (var reader = selCon.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        result.Add(new OneCOrder()
                        {
                            LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
                            Id1C = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
                            Number = (reader.GetValue(2) != System.DBNull.Value) ? (string)reader.GetValue(2) : "",
                            PrimaryAmount = (reader.GetValue(3) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(3).ToString()) : 0,
                            PaymentAmount = (reader.GetValue(4) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(4).ToString()) : 0,
                            DeliveryAddress = (reader.GetValue(5) != System.DBNull.Value) ? (string)reader.GetValue(5) : "",
                            OrderStatus = (reader.GetValue(6) != System.DBNull.Value) ? (string)reader.GetValue(6) : "",
                            Comment = (reader.GetValue(7) != System.DBNull.Value) ? (string)reader.GetValue(7) : "",
                        });
                    }
                }
            }

            var orderProducts = new OneCOrderProduct();
            foreach (var order in result)
            {
                order.OrderProducts = orderProducts.GetAllByOrderId(order.LocalId);
            }

            return result;
        }
    }
}
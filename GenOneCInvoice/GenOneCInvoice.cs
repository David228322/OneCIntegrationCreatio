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

    [DataContract]
    public class OneCInvoice : OneCBaseEntity<OneCInvoice>
    {
        [DataMember(Name = "Status")]
        public string Status { get; set; }
        [DataMember(Name = "Number")]
        public string Number { get; set; }
        [DataMember(Name = "StartDate")]
        public string StartDate { get; set; }
        [DataMember(Name = "Notes")]
        public string Notes { get; set; }

        [DataMember(Name = "ContractLocalId")]
        public string ContractLocalId { get; set; }
        [DataMember(Name = "AccountLocalId")]
        public string AccountLocalId { get; set; }
        [DataMember(Name = "OrderLocalId")]
        public string OrderLocalId { get; set; }
        [DataMember(Name = "OwnerLocalId")]
        public string OwnerLocalId { get; set; }

        [DataMember(Name = "DueDate")]
        public string DueDate { get; set; }

        [DataMember(Name = "Amount")]
        public decimal Amount { get; set; }
        [DataMember(Name = "AmountWithoutTax")]
        public decimal AmountWithoutTax { get; set; }
        [DataMember(Name = "Currency")]
        public string Currency { get; set; }

        /* [DataMember(Name = "Products")]
        public List<OneCInvoiceProduct> Products { get; set; }
        [DataMember(Name = "AdditionalServices")]
        public List<OneCInvoiceAdditionalServices> AdditionalServices { get; set; }
        [DataMember(Name = "AutomaticDiscount")]
        public List<OneCInvoiceAutomaticDiscount> AutomaticDiscount { get; set; }
        [DataMember(Name = "InvoicePaid")]
        public List<OneCInvoicePaid> InvoicePaid { get; set; } */

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

            bool success = false;

            Select selCol = new Select(UserConnection)
                .Column("Invoice", "Id").Top(1)
                .Column("Invoice", "ModifiedOn")
                .From("Invoice").As("Invoice")
                as Select;

            if (!string.IsNullOrEmpty(this.LocalId))
            {
                selCol = selCol.Where("Invoice", "Id").IsEqual(Column.Parameter(new Guid(this.LocalId))) as Select;
            }
            else if (!string.IsNullOrEmpty(this.Id1C))
            {
                selCol = selCol.Where("Invoice", "GenID1C").IsEqual(Column.Parameter(this.Id1C)) as Select;
            }
            else
            {
                return false;
            }

            using (var dbExecutor = UserConnection.EnsureDBConnection())
            using (var reader = selCol.ExecuteReader(dbExecutor))
            {
                if (reader.Read())
                {
                    string dateModified = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1).ToString() : "";
                    if (!string.IsNullOrEmpty(dateModified))
                        dateModified = DateTime.Parse(dateModified).ToLocalTime().ToString();

                    this.BpmId = (reader.GetValue(0) != System.DBNull.Value) ? (Guid)reader.GetValue(0) : Guid.Empty;
                    this.LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "";
                    this.ModifiedOn = dateModified;
                    success = true;
                }
            }

            return success;
        }


        public override bool SaveRemoteItem()
        {
            bool success = false;
            var oneCHelper = new OneCIntegrationHelper();
            Guid _Country = Guid.Empty;
            Guid _Organization = Guid.Empty;
            Guid _Counterparty = Guid.Empty;
            Guid _Contract = Guid.Empty;
            Guid _Warehouse = Guid.Empty;
            Guid _BasisAdditionalDiscount = Guid.Empty;
            Guid _ResponsibleMRK = Guid.Empty;
            Guid _ResponsibleMAP = Guid.Empty;
            Guid _ResponsibleMAP2 = Guid.Empty;
            Guid _Currency = Guid.Empty;
            Guid _Status = Guid.Empty;

            if (!string.IsNullOrEmpty(this.ContractLocalId) && oneCHelper.ÑhekId("Contract", this.ContractLocalId))
            {
                _Contract = new Guid(this.ContractLocalId);
            }

            if (!string.IsNullOrEmpty(this.Currency))
            {
                if (this.Currency == "ãðí")
                    this.Currency = "UAH";

                _Currency = oneCHelper.GetId("Currency", this.Currency, "ShortName");
            }

            if (!string.IsNullOrEmpty(this.Status))
            {
                _Status = oneCHelper.GetId("InvoicePaymentStatus", this.Status, "GenShortName");
            }

            var _entity = UserConnection.EntitySchemaManager
                .GetInstanceByName("Invoice").CreateEntity(UserConnection);

            if (this.BpmId == Guid.Empty)
            {
                _entity.SetDefColumnValues();
            }
            else if (!_entity.FetchFromDB(_entity.Schema.PrimaryColumn.Name, this.BpmId))
            {
                _entity.SetDefColumnValues();
            }

            if (!string.IsNullOrEmpty(this.Id1C))
            {
                _entity.SetColumnValue("GenID1C", this.Id1C);
            }

            if (_Status != Guid.Empty)
            {
                _entity.SetColumnValue("PaymentStatusId", _Status);
            }

            if (!string.IsNullOrEmpty(this.Number))
            {
                _entity.SetColumnValue("Number", this.Number);
            }

            if (!string.IsNullOrEmpty(this.StartDate))
            {
                _entity.SetColumnValue("StartDate", DateTime.Parse(this.StartDate));
            }

            if (_Contract != Guid.Empty)
            {
                _entity.SetColumnValue("ContractId", _Contract);
            }

            if (!string.IsNullOrEmpty(this.OrderLocalId) && this.OrderLocalId != "00000000-0000-0000-0000-000000000000" && oneCHelper.ÑhekId("Order", this.OrderLocalId))
            {
                _entity.SetColumnValue("OrderId", new Guid(this.OrderLocalId));
            }

            if (!string.IsNullOrEmpty(this.DueDate))
            {
                _entity.SetColumnValue("DueDate", DateTime.Parse(this.DueDate));
            }

            if (!string.IsNullOrEmpty(this.Notes))
            {
                _entity.SetColumnValue("Notes", this.Notes);
            }

            if (this.Amount > 0)
            {
                _entity.SetColumnValue("Amount", this.Amount);
            }

            if (this.AmountWithoutTax > 0)
            {
                _entity.SetColumnValue("AmountWithoutTax", this.AmountWithoutTax);
            }

            if (_Currency != Guid.Empty)
            {
                _entity.SetColumnValue("CurrencyId", _Currency);
            }

            var now = DateTime.Now;
            if (_entity.StoringState == StoringObjectState.Changed || this.BpmId == Guid.Empty)
            {
                _entity.SetColumnValue("ModifiedOn", now);
                success = _entity.Save(true);
            }
            else
            {
                success = true;
            }
            this.BpmId = (Guid)_entity.GetColumnValue("Id");
            this.ModifiedOn = now.ToString();
            //TODO: complete this part of code
            /*
            if (this.BPMId != Guid.Empty)
            {
                if (this.Products != null && this.Products.Count > 0)
                {
                    List<string> _products = oneCHelper.GetList(this.BPMId.ToString(), "InvoiceId", "GenID1C", "InvoiceProduct");
                    if (_products != null && _products.Count > 0)
                    {
                        foreach (string _productId in _products)
                        {

                            if (this.Products.Exists(x => x.ID1C == _productId) == false)
                            {
                                oneCHelper.delItem(_productId, "GenID1C", this.BPMId.ToString(), "InvoiceId", "InvoiceProduct");
                            }
                        }
                    }

                    foreach (var product in this.Products)
                    {
                        product.InvoiceId = this.BPMId;
                        product.ProcessRemoteItem();
                    }
                }

                if (this.AdditionalServices != null && this.AdditionalServices.Count > 0)
                {
                    List<string> _additionServices = oneCHelper.GetList(this.BPMId.ToString(), "GenInvoiceId", "GenID1C", "GenAdditionalServicesInv");
                    if (_additionServices != null && _additionServices.Count > 0)
                    {
                        foreach (string _additionServiceId in _additionServices)
                        {

                            if (this.AdditionalServices.Exists(x => x.ID1C == _additionServiceId) == false)
                            {
                                oneCHelper.delItem(_additionServiceId, "GenID1C", this.BPMId.ToString(), "GenInvoiceId", "GenAdditionalServicesInv");
                            }
                        }
                    }

                    foreach (var service in this.AdditionalServices)
                    {
                        service.InvoiceId = this.BPMId;
                        service.ProcessRemoteItem();
                    }
                }

                if (this.AutomaticDiscount != null && this.AutomaticDiscount.Count > 0)
                {
                    foreach (var discount in this.AutomaticDiscount)
                    {
                        discount.InvoiceId = this.BPMId;
                        discount.ProcessRemoteItem();
                    }
                }

                if (this.InvoicePaid != null && this.InvoicePaid.Count > 0)
                {
                    foreach (var paid in this.InvoicePaid)
                    {
                        paid.InvoiceLocalId = this.BPMId.ToString();
                        paid.ProcessRemoteItem();
                    }
                }
            } */

            return success;
        }

        public override List<OneCInvoice> GetItem(SearchFilter searchFilter)
        {
            List<OneCInvoice> result = new List<OneCInvoice>();
            /* OneCInvoiceProduct _products = new OneCInvoiceProduct();
             OneCInvoiceAdditionalServices _additionalServices = new OneCInvoiceAdditionalServices();
             OneCInvoiceAutomaticDiscount _automaticDiscount = new OneCInvoiceAutomaticDiscount();
             OneCInvoicePaid _invoicePaid = new OneCInvoicePaid(); */
            Select selCon = new Select(UserConnection)
                .Column("Invoice", "Id")
                .Column("Invoice", "GenID1C")
                .Column("Invoice", "Number")
                .Column("Invoice", "StartDate")
                .Column("Invoice", "ContractId")
                .Column("Invoice", "AccountId")
                .Column("Invoice", "OwnerId")
                .Column("Invoice", "OrderId")
                .Column("Invoice", "DueDate")
                .Column("Invoice", "Notes")
                .Column("Invoice", "Amount")
                .Column("Invoice", "AmountWithoutTax")
                .Column("Currency", "ShortName")
                .Column("InvoicePaymentStatus", "Name")
                .Column("Invoice", "ModifiedOn")
                .From("Invoice").As("Invoice")
                .LeftOuterJoin("Account").As("Account")
                    .On("Account", "Id").IsEqual("Invoice", "AccountId")
                .LeftOuterJoin("Contract").As("Contract")
                    .On("Contract", "Id").IsEqual("Invoice", "ContractId")
                .LeftOuterJoin("Contact").As("C1")
                    .On("C1", "Id").IsEqual("Invoice", "OwnerId")
                .LeftOuterJoin("Currency").As("Currency")
                    .On("Currency", "Id").IsEqual("Invoice", "CurrencyId")
                .LeftOuterJoin("InvoicePaymentStatus")
                    .On("InvoicePaymentStatus", "Id").IsEqual("Invoice", "PaymentStatusId")
            as Select;

            selCon = base.GetItemByFilters(selCon, searchFilter);

            using (var dbExecutor = UserConnection.EnsureDBConnection())
            {
                using (var reader = selCon.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        var dateModified = (reader.GetValue(14) != System.DBNull.Value) ? (string)reader.GetValue(14).ToString() : "";
                        if (!string.IsNullOrEmpty(dateModified))
                        {
                            dateModified = DateTime.Parse(dateModified).ToLocalTime().ToString();
                        }

                        result.Add(new OneCInvoice()
                        {
                            LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
                            Id1C = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
                            Number = (reader.GetValue(2) != System.DBNull.Value) ? (string)reader.GetValue(2) : "",
                            StartDate = (reader.GetValue(3) != System.DBNull.Value) ? (string)reader.GetValue(3).ToString() : "",
                            ContractLocalId = (reader.GetValue(4) != System.DBNull.Value) ? (string)reader.GetValue(4).ToString() : "",
                            AccountLocalId = (reader.GetValue(5) != System.DBNull.Value) ? (string)reader.GetValue(5).ToString() : "",
                            OwnerLocalId = (reader.GetValue(6) != System.DBNull.Value) ? (string)reader.GetValue(6).ToString() : "",
                            OrderLocalId = (reader.GetValue(7) != System.DBNull.Value) ? (string)reader.GetValue(7).ToString() : "",
                            DueDate = (reader.GetValue(8) != System.DBNull.Value) ? (string)reader.GetValue(8).ToString() : "",
                            Notes = (reader.GetValue(9) != System.DBNull.Value) ? (string)reader.GetValue(9) : "",
                            Amount = (reader.GetValue(10) != System.DBNull.Value) ? (decimal)reader.GetValue(10) : 0,
                            AmountWithoutTax = (reader.GetValue(11) != System.DBNull.Value) ? (decimal)reader.GetValue(11) : 0,
                            Currency = (reader.GetValue(12) != System.DBNull.Value) ? (string)reader.GetValue(12) : "",
                            Status = (reader.GetValue(13) != System.DBNull.Value) ? (string)reader.GetValue(13) : "",
                            ModifiedOn = dateModified,
                        });
                    }
                }
            }
            //TODO: Get Products, AdditionalServices, AutomaticDiscount, InvoicePaid
            /*
            foreach (var inv in result)
            {
                inv.Products = _products.getItem(inv.LocalId);
                inv.AdditionalServices = _additionalServices.getItem(inv.LocalId);
                inv.AutomaticDiscount = _automaticDiscount.getItem(inv.LocalId);
                inv.InvoicePaid = _invoicePaid.getItem(inv.LocalId);
            }
            */
            return result;
        }
    }
}
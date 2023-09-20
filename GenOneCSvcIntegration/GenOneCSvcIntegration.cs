namespace Terrasoft.Configuration.GenOneCSvcIntegration
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

    using Terrasoft.Configuration.GenOneCAccount;
    using Terrasoft.Configuration.GenOneCContact;
    using Terrasoft.Configuration.GenOneCContract;
    using Terrasoft.Configuration.GenOneCProduct;
    using Terrasoft.Configuration.GenOneCOrder;
    using Terrasoft.Configuration.GenOneCInvoice;

    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public sealed class OneCSvc : BaseService
    {
        #region Order

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
            RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public List<OneCOrder> GetOrderInfo(SearchFilter account)
        {
            var oneCOrder = new OneCOrder();
            var result = new List<OneCOrder>();
            result = oneCOrder.GetItem(account);
            return result;
        }

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
            RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public InfoResult SetOrderInfo(OneCOrder orderRequest)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            InfoResult result = new InfoResult();

            try
            {
                result.Result = "OK";
                var order = orderRequest.ProcessRemoteItem();
                result.LocalId = order.BpmId.ToString();
				result.Date = order.ModifiedOn.ToString();
            }
            catch (Exception ex)
            {
                result.Result = "ERROR";
                result.Error = ex.Message;

                LogHelper.Log(UserConnection, LogHelper.LogResult.Error, "SetOneCOrder" + ex.Message, stopwatch, LogHelper.IntegrationDirection.Import, orderRequest);
                throw;
            }
            LogHelper.Log(UserConnection, LogHelper.LogResult.Ok, "SetOneCOrder", stopwatch, LogHelper.IntegrationDirection.Import, orderRequest);

            return result;
        }

        #endregion

        #region Invoice

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
            RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public List<OneCInvoice> GetInvoiceInfo(SearchFilter search)
        {
            OneCInvoice invoice = new OneCInvoice();
            List<OneCInvoice> result = new List<OneCInvoice>();
            result = invoice.GetItem(search);
            return result;
        }

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
            RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public InfoResult SetInvoiceInfo(OneCInvoice invoiceRequest)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = new InfoResult();

            try
            {
                result.Result = "OK";
                var invoice = invoiceRequest.ProcessRemoteItem();
                result.LocalId = invoice.BpmId.ToString();
				result.Date = invoice.ModifiedOn.ToString();             
            }
            catch (Exception ex)
            {
                result.Result = "ERROR";
                result.Error = ex.Message;

                LogHelper.Log(UserConnection, LogHelper.LogResult.Error, "SetOneCOrder" + ex.Message, stopwatch, LogHelper.IntegrationDirection.Import, invoiceRequest);
                throw;
            }
            LogHelper.Log(UserConnection, LogHelper.LogResult.Ok, "SetOneCOrder", stopwatch, LogHelper.IntegrationDirection.Import, invoiceRequest);

            return result;
        }

        #endregion

        #region Product

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
            RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public List<OneCProduct> GetProductInfo(SearchFilter search)
        {
            var oneCProduct = new OneCProduct();
            var result = new List<OneCProduct>();
            result = oneCProduct.GetItem(search);
            return result;
        }

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
            RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public InfoResult SetProductInfo(OneCProduct productRequest)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            InfoResult result = new InfoResult();

            try
            {
                result.Result = "OK";
                var product = productRequest.ProcessRemoteItem();
                result.LocalId = product.BpmId.ToString();
				result.Date = product.ModifiedOn.ToString(); 
            }
            catch (Exception ex)
            {
                result.Result = "ERROR";
                result.Error = ex.Message;

                LogHelper.Log(UserConnection, LogHelper.LogResult.Error, "SetOneCProduct" + ex.Message, stopwatch, LogHelper.IntegrationDirection.Import, productRequest);
                throw;
            }
            LogHelper.Log(UserConnection, LogHelper.LogResult.Ok, "SetOneCProduct", stopwatch, LogHelper.IntegrationDirection.Import, productRequest);

            return result;
        }

        #endregion

        #region Contact

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
            RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public InfoResult SetContactInfo(OneCContact contactRequest)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = new InfoResult();

            try
            {
                result.Result = "OK";
                var contact = contactRequest.ProcessRemoteItem();
                result.LocalId = contact.BpmId.ToString();
				result.Date = contact.ModifiedOn.ToString(); 
            }
            catch (Exception ex)
            {
                result.Result = "ERROR";
                result.Error = ex.Message;

                LogHelper.Log(UserConnection, LogHelper.LogResult.Error, "SetOneCContact" + ex.Message, stopwatch, LogHelper.IntegrationDirection.Import, contactRequest);
                throw;
            }
            LogHelper.Log(UserConnection, LogHelper.LogResult.Ok, "SetOneCContact", stopwatch, LogHelper.IntegrationDirection.Import, contactRequest);

            return result;
        }

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
            RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public List<OneCContact> GetContactInfo(SearchFilter searchFilter)
        {
            var oneCContact = new OneCContact();
            var result = new List<OneCContact>();
            result = oneCContact.GetItem(searchFilter);
            return result;
        }

        #endregion

        #region Account

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
            RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public InfoResult SetAccountInfo(OneCAccount accountRequest)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = new InfoResult();

            try
            {
                result.Result = "OK";
                var account = accountRequest.ProcessRemoteItem();
                result.LocalId = account.BpmId.ToString();
				result.Date = account.ModifiedOn.ToString(); 
            }
            catch (Exception ex)
            {
                result.Result = "ERROR";
                result.Error = ex.Message;

                LogHelper.Log(UserConnection, LogHelper.LogResult.Error, "SetOneCAccount" + ex.Message, stopwatch, LogHelper.IntegrationDirection.Import, accountRequest);
                throw;
            }
            LogHelper.Log(UserConnection, LogHelper.LogResult.Ok, "SetOneCAccount", stopwatch, LogHelper.IntegrationDirection.Import, accountRequest);

            return result;
        }

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
            RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public List<OneCAccount> GetAccountInfo(SearchFilter account)
        {
            var oneCAccount = new OneCAccount();
            var result = new List<OneCAccount>();
            result = oneCAccount.GetItem(account);
            return result;
        }

        #endregion

        #region Contract

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
            RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public InfoResult SetContractInfo(OneCContract contractRequest)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = new InfoResult();

            try
            {
                result.Result = "OK";
                var contract = contractRequest.ProcessRemoteItem();
                result.LocalId = contract.BpmId.ToString();
				result.Date = contract.ModifiedOn.ToString(); 
            }
            catch (Exception ex)
            {
                result.Result = "ERROR";
                result.Error = ex.Message;

                LogHelper.Log(UserConnection, LogHelper.LogResult.Error, "SetOneCContract" + ex.Message, stopwatch, LogHelper.IntegrationDirection.Import, contractRequest);
                throw;
            }
            LogHelper.Log(UserConnection, LogHelper.LogResult.Ok, "SetOneCContract", stopwatch, LogHelper.IntegrationDirection.Import, contractRequest);

            return result;
        }

        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
            RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        public List<OneCContract> GetContractInfo(SearchFilter contract)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = new List<OneCContract>();
            var oneCContract = new OneCContract();
            result = oneCContract.GetItem(contract);
            return result;
        }

        #endregion
    }

    [DataContract]
    public class SearchFilter
    {
        [DataMember(Name = "Id")]
        public string Id1C { get; set; }
        [DataMember(Name = "LocalId")]
        public string LocalId { get; set; }
        [DataMember(Name = "CreatedFrom")]
        public string CreatedFrom { get; set; }
        [DataMember(Name = "CreatedTo")]
        public string CreatedTo { get; set; }
        [DataMember(Name = "ModifiedFrom")]
        public string ModifiedFrom { get; set; }
        [DataMember(Name = "ModifiedTo")]
        public string ModifiedTo { get; set; }
    }

    [DataContract]
    public class InfoResult
    {
        [DataMember]
        public string Result { get; set; }//OK / ERROR
        [DataMember]
        public string Error { get; set; }
        [DataMember(Name = "LocalId")]
        public string LocalId { get; set; }
        [DataMember(Name = "Date")]
        public string Date { get; set; }
    }
}
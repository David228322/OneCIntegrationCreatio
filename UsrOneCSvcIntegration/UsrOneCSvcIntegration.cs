namespace Terrasoft.Configuration.UsrOneCSvcIntegration
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

	using Terrasoft.Configuration.UsrIntegrationLogHelper;
	
	using Terrasoft.Configuration.UsrOneCAccount;
	using Terrasoft.Configuration.UsrOneCContact;
	using Terrasoft.Configuration.UsrOneCContract;
	
	[ServiceContract]
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
	public class OneCSvc : BaseService
	{
		[OperationContract]
		[WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
			RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		public InfoResult SetOneCAccount(OneCAccount account)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var result = new InfoResult();
			
			try
			{
				result.Result = "OK";
				result.LocalId = account.ProcessRemoteItem();
			}
			catch (Exception ex)
			{
				result.Result = "ERROR";
				result.Error = ex.Message;
			
				LogHelper.Log(UserConnection, LogHelper.LogResult.Error, "SetOneCAccount"+ex.Message, stopwatch, LogHelper.IntegrationDirection.Import, account);
				throw new Exception(ex.Message);
			}
			LogHelper.Log(UserConnection, LogHelper.LogResult.Ok, "SetOneCAccount", stopwatch, LogHelper.IntegrationDirection.Import, account);
			
			return result;
		}
		
		[OperationContract]
		[WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
			RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		public List<OneCAccount> GetAccountInfo(Search account)
		{
			var oneCAccount = new OneCAccount();
			var result = new List<OneCAccount>();
			result = oneCAccount.GetItem(account);
			return result;
		}
		
		[OperationContract]
		[WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
			RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		public InfoResult SetContactInfo(OneCContact contact)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var result = new InfoResult();
			
			try
			{
				result.Result = "OK";
				result.LocalId = contact.ProcessRemoteItem();
			}
			catch (Exception ex)
			{
				result.Result = "ERROR";
				result.Error = ex.Message;
			
				LogHelper.Log(UserConnection, LogHelper.LogResult.Error, "SetOneCContact"+ex.Message, stopwatch, LogHelper.IntegrationDirection.Import, contact);
				throw new Exception(ex.Message);
			}
			LogHelper.Log(UserConnection, LogHelper.LogResult.Ok, "SetOneCContact", stopwatch, LogHelper.IntegrationDirection.Import, contact);
			
			return result;
		}
		
		[OperationContract]
		[WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
			RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		public List<OneCContact> GetContactInfo(Search contact)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var oneCContact = new OneCContact();
			var result = new List<OneCContact>();
			result = oneCContact.GetItem(contact);
			return result;
		}
		
		[OperationContract]
		[WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
			RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		public InfoResult SetContractInfo(OneCContract contract)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var result = new InfoResult();
			
			try
			{
				result.Result = "OK";
				result.LocalId = contract.ProcessRemoteItem();
			}
			catch (Exception ex)
			{
				result.Result = "ERROR";
				result.Error = ex.Message;
			
				LogHelper.Log(UserConnection, LogHelper.LogResult.Error, "SetOneCContract"+ex.Message, stopwatch, LogHelper.IntegrationDirection.Import, contract);
				throw new Exception(ex.Message);
			}
			LogHelper.Log(UserConnection, LogHelper.LogResult.Ok, "SetOneCContract", stopwatch, LogHelper.IntegrationDirection.Import, contract);
			
			return result;
		}
		
		[OperationContract]
		[WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
			RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		public List<OneCContract> GetContractInfo(Search contract)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var result = new List<OneCContract>();
			var oneCContract = new OneCContract();
			result = oneCContract.GetItem(contract);
			return result;
		}
	}
	
	public class Directory
	{
		[IgnoreDataMember]
		private UserConnection _userConnection;
		[IgnoreDataMember]
		public UserConnection UserConnection {
			get =>
				_userConnection ??
				(_userConnection = HttpContext.Current.Session["UserConnection"] as UserConnection);
			set => _userConnection = value;
		}
		
		public Guid GetId(string schemaName, string name, string columnName = "Name")
		{
			var result = Guid.Empty;
			var selEntity = new Select(UserConnection)
				.Column("Id").Top(1)
				.From(schemaName)
				.Where(columnName).IsLike(Column.Parameter("%" + name + "%"))
			as Select;
			
			result = selEntity.ExecuteScalar<Guid>();
			
			if (result == Guid.Empty && columnName == "Name")
			{
				var entity = UserConnection.EntitySchemaManager
				.GetInstanceByName(schemaName).CreateEntity(UserConnection);
				var now = DateTime.Now;
				
				entity.SetDefColumnValues();
				
				entity.SetColumnValue("Name", name);
				entity.SetColumnValue("ModifiedOn", now);
				entity.Save(true);
			
				result = (Guid)entity.GetColumnValue("Id");
			}
			
			return result;
		}
		
		public bool СhekId(string schemaName, string id = "", string name = "")
		{
			var result = false;
			var guidId = Guid.Empty;
			var selEntity = new Select(UserConnection)
					.Column("Id").Top(1)
					.From(schemaName)
				as Select;
			
			if (!string.IsNullOrEmpty(id))
				selEntity = selEntity.Where("Id").IsEqual(Column.Parameter(new Guid(id))) as Select;
			else if (!string.IsNullOrEmpty(name))
				selEntity = selEntity.Where("Name").IsLike(Column.Parameter("%" + name + "%")) as Select;
			else
				return false;
			
			guidId = selEntity.ExecuteScalar<Guid>();
			
			if (guidId != Guid.Empty)
			{
				result = true;
			}
			
			return result;
		}
		
		public Guid GetSerialId(string productId, string serialName)
		{
			var result = Guid.Empty;
			var selEntity = new Select(UserConnection)
				.Column("Id").Top(1)
				.From("GenSerial")
				.Where("GenProductId").IsEqual(Column.Parameter(new Guid(productId)))
				.And("Name").IsLike(Column.Parameter("%" + serialName + "%"))
			as Select;
			
			result = selEntity.ExecuteScalar<Guid>();

			if (result != Guid.Empty) return result;
			var entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("GenSerial").CreateEntity(UserConnection);
			var now = DateTime.Now;
				
			entity.SetDefColumnValues();
				
			entity.SetColumnValue("Name", serialName);
			entity.SetColumnValue("GenProductId", new Guid(productId));
			entity.SetColumnValue("ModifiedOn", now);
			entity.Save(true);
			
			result = (Guid)entity.GetColumnValue("Id");

			return result;
		}
		
		public Guid GetCultureId(string langCode)
		{
			var result = Guid.Empty;
			var selEntity = new Select(UserConnection)
				.Column("Id").Top(1)
				.From("SysCulture")
				.Where("Name").IsLike(Column.Parameter("%"+langCode+"%"))
			as Select;
			
			result = selEntity.ExecuteScalar<Guid>();
			return result;
		}
	}
	
	[DataContract]
	public class Search
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
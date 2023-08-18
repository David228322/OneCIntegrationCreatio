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
			Stopwatch _stopwatch = new Stopwatch();
			_stopwatch.Start();
			InfoResult result = new InfoResult();
			
			try
			{
				result.Result = "OK";
				result.LocalId = account.ProcessRemoteItem();
			}
			catch (Exception ex)
			{
				result.Result = "ERROR";
				result.Error = ex.Message;
			
				LogHelper.Log(UserConnection, LogHelper.LogResult.ERROR, "SetOneCAccount"+ex.Message, _stopwatch, LogHelper.IntegrationDirection.IMPORT, account);
				throw new Exception(ex.Message);
			}
			LogHelper.Log(UserConnection, LogHelper.LogResult.OK, "SetOneCAccount", _stopwatch, LogHelper.IntegrationDirection.IMPORT, account);
			
			return result;
		}
		
		[OperationContract]
		[WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
			RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		public List<OneCAccount> GetAccountInfo(Search account)
		{
			OneCAccount _Account = new OneCAccount();
			List<OneCAccount> result = new List<OneCAccount>();
			result = _Account.getItem(account);
			return result;
		}
		
		[OperationContract]
		[WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
			RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		public InfoResult SetContactInfo(OneCContact contact)
		{
			Stopwatch _stopwatch = new Stopwatch();
			_stopwatch.Start();
			InfoResult result = new InfoResult();
			
			try
			{
				result.Result = "OK";
				result.LocalId = contact.ProcessRemoteItem();
			}
			catch (Exception ex)
			{
				result.Result = "ERROR";
				result.Error = ex.Message;
			
				LogHelper.Log(UserConnection, LogHelper.LogResult.ERROR, "SetOneCContact"+ex.Message, _stopwatch, LogHelper.IntegrationDirection.IMPORT, contact);
				throw new Exception(ex.Message);
			}
			LogHelper.Log(UserConnection, LogHelper.LogResult.OK, "SetOneCContact", _stopwatch, LogHelper.IntegrationDirection.IMPORT, contact);
			
			return result;
		}
		
		[OperationContract]
		[WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
			RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		public List<OneCContact> GetContactInfo(Search contact)
		{
			Stopwatch _stopwatch = new Stopwatch();
			_stopwatch.Start();
			OneCContact _Contact = new OneCContact();
			List<OneCContact> result = new List<OneCContact>();
			result = _Contact.getItem(contact);
			return result;
		}
		
		[OperationContract]
		[WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
			RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		public InfoResult SetContractInfo(OneCContract contract)
		{
			Stopwatch _stopwatch = new Stopwatch();
			_stopwatch.Start();
			InfoResult result = new InfoResult();
			
			try
			{
				result.Result = "OK";
				result.LocalId = contract.ProcessRemoteItem();
			}
			catch (Exception ex)
			{
				result.Result = "ERROR";
				result.Error = ex.Message;
			
				LogHelper.Log(UserConnection, LogHelper.LogResult.ERROR, "SetOneCContract"+ex.Message, _stopwatch, LogHelper.IntegrationDirection.IMPORT, contract);
				throw new Exception(ex.Message);
			}
			LogHelper.Log(UserConnection, LogHelper.LogResult.OK, "SetOneCContract", _stopwatch, LogHelper.IntegrationDirection.IMPORT, contract);
			
			return result;
		}
		
		[OperationContract]
		[WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare,
			RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
		public List<OneCContract> GetContractInfo(Search contract)
		{
			Stopwatch _stopwatch = new Stopwatch();
			_stopwatch.Start();
			List<OneCContract> result = new List<OneCContract>();
			OneCContract _Contract = new OneCContract();
			result = _Contract.getItem(contract);
			return result;
		}
	}
	
	public class Directory
	{
		[IgnoreDataMember]
		private UserConnection _userConnection;
		[IgnoreDataMember]
		public UserConnection UserConnection {
			get {
				return _userConnection ??
					(_userConnection = HttpContext.Current.Session["UserConnection"] as UserConnection);
			}
			set {
				_userConnection = value;
			}
		}
		
		public Guid GetId(string _SchemaName, string _Name, string _ColumnName = "Name")
		{
			Guid result = Guid.Empty;
			Select _selEntity = new Select(UserConnection)
				.Column("Id").Top(1)
				.From(_SchemaName)
				.Where(_ColumnName).IsLike(Column.Parameter("%" + _Name + "%"))
			as Select;
			
			result = _selEntity.ExecuteScalar<Guid>();
			
			if (result == Guid.Empty && _ColumnName == "Name")
			{
				var _entity = UserConnection.EntitySchemaManager
				.GetInstanceByName(_SchemaName).CreateEntity(UserConnection);
				var _now = DateTime.Now;
				
				_entity.SetDefColumnValues();
				
				_entity.SetColumnValue("Name", _Name);
				_entity.SetColumnValue("ModifiedOn", _now);
				_entity.Save(true);
			
				result = (Guid)_entity.GetColumnValue("Id");
			}
			
			return result;
		}
		
		public bool СhekId(string _SchemaName, string _Id = "", string _Name = "")
		{
			bool result = false;
			Guid Id = Guid.Empty;
			Select _selEntity = new Select(UserConnection)
				.Column("Id").Top(1)
				.From(_SchemaName)
			as Select;
			
			if (!string.IsNullOrEmpty(_Id))
				_selEntity = _selEntity.Where("Id").IsEqual(Column.Parameter(new Guid(_Id))) as Select;
			else if (!string.IsNullOrEmpty(_Name))
				_selEntity = _selEntity.Where("Name").IsLike(Column.Parameter("%" + _Name + "%")) as Select;
			else
				return result;
			
			Id = _selEntity.ExecuteScalar<Guid>();
			
			if (Id != Guid.Empty)
			{
				result = true;
			}
			
			return result;
		}
		
		public Guid GetSerialId(string _ProductId, string _SerialName)
		{
			Guid result = Guid.Empty;
			Select _selEntity = new Select(UserConnection)
				.Column("Id").Top(1)
				.From("GenSerial")
				.Where("GenProductId").IsEqual(Column.Parameter(new Guid(_ProductId)))
				.And("Name").IsLike(Column.Parameter("%" + _SerialName + "%"))
			as Select;
			
			result = _selEntity.ExecuteScalar<Guid>();
			
			if (result == Guid.Empty)
			{
				var _entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("GenSerial").CreateEntity(UserConnection);
				var _now = DateTime.Now;
				
				_entity.SetDefColumnValues();
				
				_entity.SetColumnValue("Name", _SerialName);
				_entity.SetColumnValue("GenProductId", new Guid(_ProductId));
				_entity.SetColumnValue("ModifiedOn", _now);
				_entity.Save(true);
			
				result = (Guid)_entity.GetColumnValue("Id");
			}
			
			return result;
		}
		
		public Guid getCultureId(string _LangCode)
		{
			Guid result = Guid.Empty;
			Select _selEntity = new Select(UserConnection)
				.Column("Id").Top(1)
				.From("SysCulture")
				.Where("Name").IsLike(Column.Parameter("%"+_LangCode+"%"))
			as Select;
			
			result = _selEntity.ExecuteScalar<Guid>();
			return result;
		}
	}
	
	[DataContract]
	public class Search
	{
		[DataMember(Name = "Id")]
		public string ID1C { get; set; }
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
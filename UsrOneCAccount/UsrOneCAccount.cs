namespace Terrasoft.Configuration.UsrOneCAccount
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
	using Terrasoft.Configuration.UsrOneCSvcIntegration;
 
	[DataContract]
	public class OneCAccount
	{
		[DataMember(Name = "Id")]
		public string ID1C { get; set; }
		[DataMember(Name = "BPMId")]
		public string LocalId { get; set; }
		[IgnoreDataMember]
		public Guid BPMId { get; set; }
		
		[DataMember(Name = "Name")]
		public string Name { get; set; }
		[DataMember(Name = "Code")]
		public string Code { get; set; }
		[DataMember(Name = "OwnerLocalId")]
		public string OwnerLocalId { get; set; }
		
		[DataMember(Name = "Addresses")]
		public List<AccountAddres> Addresses { get; set; }
		
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
		
		public string ProcessRemoteItem(bool IsFull = true)
		{
			if ((!string.IsNullOrEmpty(this.LocalId) && this.LocalId != "00000000-0000-0000-0000-000000000000") ||
				(!string.IsNullOrEmpty(this.ID1C) && this.ID1C != "00000000-0000-0000-0000-000000000000"))
			{
				if (this.BPMId == Guid.Empty)
				{
					this.ResolveRemoteItem();
				}
				if (this.BPMId == Guid.Empty || IsFull == true)
				{
					this.SaveRemoteItem();
				}
			}
			return this.BPMId.ToString();
		}
		
		public bool ResolveRemoteItem()
		{
			bool success = false;
			
			if (string.IsNullOrEmpty(this.LocalId) && string.IsNullOrEmpty(this.ID1C))
				return success;
			Select _selEntity = new Select(UserConnection)
				.Column("Account", "Id").Top(1)
				.From("Account").As("Account")
			as Select;
			if (!string.IsNullOrEmpty(this.LocalId))
				_selEntity = _selEntity.Where("Account", "Id").IsEqual(Column.Parameter(new Guid(this.LocalId))) as Select;
			else if (!string.IsNullOrEmpty(this.ID1C))
				_selEntity = _selEntity.Where("Account", "GenID1C").IsEqual(Column.Parameter(this.ID1C)) as Select;
			else
				return false;
			
			Guid _entityId = _selEntity.ExecuteScalar<Guid>();
			if (_entityId != Guid.Empty)
			{
				this.BPMId = _entityId;
				success = true;
			}
			return success;
		}
		
		private bool SaveRemoteItem()
		{
			bool success = false;
			Directory directory = new Directory();
		
			Guid _Owner = Guid.Empty;
			
			if (directory.СhekId("Contact", this.OwnerLocalId))
			{
				_Owner = new Guid(this.OwnerLocalId);
			}
			
			var _entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("Account").CreateEntity(UserConnection);
			var _now = DateTime.Now;
			
			if (this.BPMId == Guid.Empty)
			{
				_entity.SetDefColumnValues();
			}
			else if (!_entity.FetchFromDB(_entity.Schema.PrimaryColumn.Name, this.BPMId)) {
				_entity.SetDefColumnValues();
			}
			
			if (!string.IsNullOrEmpty(this.ID1C))
			{
				_entity.SetColumnValue("GenID1C", this.ID1C);
			}
			
			if (!string.IsNullOrEmpty(this.Name))
			{
				_entity.SetColumnValue("Name", this.Name);
			}
			
			if (!string.IsNullOrEmpty(this.Code))
			{
				_entity.SetColumnValue("Code", this.Code);
			}
			
			if (_Owner != Guid.Empty)
			{
				_entity.SetColumnValue("OwnerId", _Owner);
			}
			
			_entity.SetColumnValue("ModifiedOn", _now);
		
			if (_entity.StoringState == StoringObjectState.Changed || this.BPMId == Guid.Empty)
			{
				success = _entity.Save(true);
			}
			else
			{
				success = true;
			}
			this.BPMId = (Guid)_entity.GetColumnValue("Id");
			
			if (this.BPMId != Guid.Empty)
			{
				if (this.Addresses != null && this.Addresses.Count > 0)
				{
					foreach (var address in this.Addresses)
					{
						address.Account = this.BPMId.ToString();
						address.ProcessRemoteItem();
					}	
				}
			}
			return success;
		}
		
		public List<OneCAccount> getItem(Search _data)
		{
			List<OneCAccount> result = new List<OneCAccount>();
			AccountAddres _addres = new AccountAddres();
			Guid LocalId = Guid.Empty;
			Select selAcc = new Select(UserConnection)
				.Column("Account", "Id")
				.Column("Account", "GenID1C")
				.Column("Account", "Name")
				.Column("Account", "Code")
				.Column("Account", "OwnerId")
				.From("Account").As("Account")
				//.LeftOuterJoin("Contact").As("ContactOwner")
				//	.On("ContactOwner", "Id").IsEqual("Account", "OwnerId")
			as Select;
			
			if (!string.IsNullOrEmpty(_data.ID1C) || !string.IsNullOrEmpty(_data.LocalId))
			{
				if (!string.IsNullOrEmpty(_data.LocalId))
					selAcc = selAcc.Where("Account", "Id").IsEqual(Column.Parameter(new Guid(_data.LocalId))) as Select;
				else if (!string.IsNullOrEmpty(_data.ID1C))
					selAcc = selAcc.Where("Account", "GenID1C").IsEqual(Column.Parameter(_data.ID1C)) as Select;
				
			}
			else if (!string.IsNullOrEmpty(_data.CreatedFrom) || !string.IsNullOrEmpty(_data.CreatedTo))
			{
				if (!string.IsNullOrEmpty(_data.CreatedFrom))
					selAcc = selAcc.Where("Account", "CreatedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(_data.CreatedFrom))) as Select;
				else if (!string.IsNullOrEmpty(_data.CreatedFrom) && !string.IsNullOrEmpty(_data.CreatedTo))
					selAcc = selAcc.And("Account", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(_data.CreatedTo))) as Select;
				else if (!string.IsNullOrEmpty(_data.CreatedTo)) 
					selAcc = selAcc.Where("Account", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(_data.CreatedTo))) as Select;
				
			}
			else if (!string.IsNullOrEmpty(_data.ModifiedFrom) || !string.IsNullOrEmpty(_data.ModifiedTo))
			{
				if (!string.IsNullOrEmpty(_data.ModifiedFrom))
					selAcc = selAcc.Where("Account", "ModifiedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(_data.ModifiedFrom))) as Select;
				else if (!string.IsNullOrEmpty(_data.ModifiedFrom) && !string.IsNullOrEmpty(_data.ModifiedTo))
					selAcc = selAcc.And("Account", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(_data.ModifiedTo))) as Select;
				else if (!string.IsNullOrEmpty(_data.ModifiedTo)) 
					selAcc = selAcc.Where("Account", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(_data.ModifiedTo))) as Select;
				
			}
			else
			{
				return result;
			}
			
			using (var dbExecutor = UserConnection.EnsureDBConnection())
			{
				using (var reader = selAcc.ExecuteReader(dbExecutor))
				{
					while (reader.Read())
					{
						result.Add(new OneCAccount() {
							LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
							ID1C = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
							Name = (reader.GetValue(2) != System.DBNull.Value) ? (string)reader.GetValue(2) : "",
							Code = (reader.GetValue(3) != System.DBNull.Value) ? (string)reader.GetValue(3) : "",
							OwnerLocalId = (reader.GetValue(14) != System.DBNull.Value) ? (string)reader.GetValue(14).ToString() : ""
						});
					}
				}
			}
			
			foreach (var account in result)
			{
				account.Addresses = _addres.getItem(account.LocalId);
			}
			
			return result;
		}
	}
	
	[DataContract]
	public class AccountAddres
	{
		[DataMember(Name = "Id")]
		public string ID1C { get; set; }
		[DataMember(Name = "BPMId")]
		public string LocalId { get; set; }
		[IgnoreDataMember]
		public Guid BPMId { get; set; }
		
		[DataMember(Name = "Address")]
		public string Address { get; set; }
		[DataMember(Name = "FullAddress")]
		public string FullAddress { get; set; }
		[DataMember(Name = "Primary")]
		public bool Primary { get; set; }
		[DataMember (Name = "Account")]
		public string Account { get; set; }
		
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
		
		public string ProcessRemoteItem(bool IsFull = true)
		{
			if ((!string.IsNullOrEmpty(this.LocalId) && this.LocalId != "00000000-0000-0000-0000-000000000000") ||
				(!string.IsNullOrEmpty(this.ID1C) && this.ID1C != "00000000-0000-0000-0000-000000000000"))
			{
				if (this.BPMId == Guid.Empty)
				{
					this.ResolveRemoteItem();
				}
				if (this.BPMId == Guid.Empty || IsFull == true)
				{
					this.SaveRemoteItem();
				}
			}
			return this.BPMId.ToString();
		}
		
		public bool ResolveRemoteItem()
		{
			bool success = false;
			
			if (string.IsNullOrEmpty(this.LocalId) && string.IsNullOrEmpty(this.ID1C))
				return success;
			Select _selEntity = new Select(UserConnection)
				.Column("AccountAddress", "Id").Top(1)
				.From("AccountAddress").As("AccountAddress")
			as Select;
			if (!string.IsNullOrEmpty(this.LocalId))
				_selEntity = _selEntity.Where("AccountAddress", "Id").IsEqual(Column.Parameter(new Guid(this.LocalId))) as Select;
			else if (!string.IsNullOrEmpty(this.ID1C))
				_selEntity = _selEntity.Where("AccountAddress", "GenID1C").IsEqual(Column.Parameter(this.ID1C)) as Select;
			else
				return false;
			
			Guid _entityId = _selEntity.ExecuteScalar<Guid>();
			if (_entityId != Guid.Empty)
			{
				this.BPMId = _entityId;
				success = true;
			}
			return success;
		}
		
		private bool SaveRemoteItem()
		{
			bool success = false;
			var _entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("AccountAddress").CreateEntity(UserConnection);
			var _now = DateTime.Now;
			
			if (this.BPMId == Guid.Empty)
			{
				_entity.SetDefColumnValues();
			}
			else if (!_entity.FetchFromDB(_entity.Schema.PrimaryColumn.Name, this.BPMId)) {
				_entity.SetDefColumnValues();
			}
			
			if (!string.IsNullOrEmpty(this.ID1C))
			{
				_entity.SetColumnValue("GenID1C", this.ID1C);
			}
			
			if (!string.IsNullOrEmpty(this.Address))
			{
				_entity.SetColumnValue("Address", this.Address);
			}
			
			if (!string.IsNullOrEmpty(this.FullAddress))
			{
				_entity.SetColumnValue("FullAddress", this.FullAddress);
			}
			
			_entity.SetColumnValue("Primary", this.Primary);
			
			if (!string.IsNullOrEmpty(this.Account))
			{
				_entity.SetColumnValue("AccountId", new Guid(this.Account));
			}
		
			if (_entity.StoringState == StoringObjectState.Changed || this.BPMId == Guid.Empty)
			{
				_entity.SetColumnValue("ModifiedOn", _now);
				success = _entity.Save(true);
			}
			else
			{
				success = true;
			}
			this.BPMId = (Guid)_entity.GetColumnValue("Id");
			return success;
		}
		
		public List<AccountAddres> getItem(string _Id)
		{
			List<AccountAddres> result = new List<AccountAddres>();
			Select _selEntity = new Select(UserConnection)
				.Column("AA", "Id")
				.Column("AA", "GenID1C")
				.Column("AA", "Address")
				.Column("AA", "FullAddress")
				.Column("AA", "Primary")
				.From("AccountAddress").As("AA")
				.Where("AA", "AccountId").IsEqual(Column.Parameter(new Guid(_Id)))
			as Select;
			
			using (var dbExecutor = UserConnection.EnsureDBConnection())
			{
				using (var reader = _selEntity.ExecuteReader(dbExecutor))
				{
					while (reader.Read())
					{
						result.Add(new AccountAddres(){
							LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
							ID1C = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
							Address = (reader.GetValue(2) != System.DBNull.Value) ? (string)reader.GetValue(2) : "",
							FullAddress = (reader.GetValue(3) != System.DBNull.Value) ? (string)reader.GetValue(3) : "",
							Primary = (reader.GetValue(4) != System.DBNull.Value) ? (bool)reader.GetValue(4) : false
						});	
					}
				}
			}
			return result;
		}
	}
	
 }
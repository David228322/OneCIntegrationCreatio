 namespace Terrasoft.Configuration.GenOneCAccount
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

    [DataContract]
	public class OneCAccount
	{
		[DataMember(Name = "Id")]
		public string Id1C { get; set; }
		[DataMember(Name = "BPMId")]
		public string LocalId { get; set; }
		[IgnoreDataMember]
		public Guid BpmId { get; set; }
		
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
			get =>
				_userConnection ??
				(_userConnection = HttpContext.Current.Session["UserConnection"] as UserConnection);
			set => _userConnection = value;
		}
		
		public string ProcessRemoteItem(bool isFull = true)
		{
			if ((string.IsNullOrEmpty(LocalId) || LocalId == "00000000-0000-0000-0000-000000000000") &&
			    (string.IsNullOrEmpty(Id1C) || Id1C == "00000000-0000-0000-0000-000000000000"))
				return BpmId.ToString();
			if (BpmId == Guid.Empty)
			{
				ResolveRemoteItem();
			}
			if (BpmId == Guid.Empty || isFull == true)
			{
				SaveRemoteItem();
			}
			return BpmId.ToString();
		}
		
		public bool ResolveRemoteItem()
		{
			if (string.IsNullOrEmpty(LocalId) && string.IsNullOrEmpty(Id1C))
				return false;
			var selEntity = new Select(UserConnection)
				.Column("Account", "Id").Top(1)
				.From("Account").As("Account")
			as Select;
			if (!string.IsNullOrEmpty(LocalId))
				selEntity = selEntity.Where("Account", "Id").IsEqual(Column.Parameter(new Guid(LocalId))) as Select;
			else if (!string.IsNullOrEmpty(Id1C))
				selEntity = selEntity.Where("Account", "GenID1C").IsEqual(Column.Parameter(Id1C)) as Select;
			else
				return false;
			
			var entityId = selEntity.ExecuteScalar<Guid>();
			if (entityId == Guid.Empty)
			{
                return false;
            }

			BpmId = entityId;
			return true;
		}
		
		private bool SaveRemoteItem()
		{
			var success = false;
			var oneCHelper = new OneCIntegrationHelper();
		
			var owner = Guid.Empty;
			
			if (oneCHelper.СhekId("Contact", OwnerLocalId))
			{
				owner = new Guid(OwnerLocalId);
			}
			
			var entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("Account").CreateEntity(UserConnection);
			var now = DateTime.Now;
			
			if (BpmId == Guid.Empty)
			{
				entity.SetDefColumnValues();
			}
			else if (!entity.FetchFromDB(entity.Schema.PrimaryColumn.Name, BpmId)) {
				entity.SetDefColumnValues();
			}
			
			if (!string.IsNullOrEmpty(Id1C))
			{
				entity.SetColumnValue("GenID1C", Id1C);
			}
			
			if (!string.IsNullOrEmpty(Name))
			{
				entity.SetColumnValue("Name", Name);
			}
			
			if (!string.IsNullOrEmpty(Code))
			{
				entity.SetColumnValue("Code", Code);
			}
			
			if (owner != Guid.Empty)
			{
				entity.SetColumnValue("OwnerId", owner);
			}
			
			entity.SetColumnValue("ModifiedOn", now);
		
			if (entity.StoringState == StoringObjectState.Changed || BpmId == Guid.Empty)
			{
				success = entity.Save(true);
			}
			else
			{
				success = true;
			}
			BpmId = (Guid)entity.GetColumnValue("Id");

			if (BpmId == Guid.Empty) return success;
			if (Addresses == null || Addresses.Count <= 0) return success;
			foreach (var address in Addresses)
			{
				address.Account = BpmId.ToString();
				address.ProcessRemoteItem();
			}
			return success;
		}
		
		public List<OneCAccount> GetItem(SearchFilter data)
		{
			var result = new List<OneCAccount>();
			var addres = new AccountAddres();
			var localId = Guid.Empty;
			var selAcc = new Select(UserConnection)
				.Column("Account", "Id")
				.Column("Account", "GenID1C")
				.Column("Account", "Name")
				.Column("Account", "Code")
				.Column("Account", "OwnerId")
				.From("Account").As("Account")
				//.LeftOuterJoin("Contact").As("ContactOwner")
				//	.On("ContactOwner", "Id").IsEqual("Account", "OwnerId")
			as Select;
			
			if (!string.IsNullOrEmpty(data.Id1C) || !string.IsNullOrEmpty(data.LocalId))
			{
				if (!string.IsNullOrEmpty(data.LocalId))
					selAcc = selAcc.Where("Account", "Id").IsEqual(Column.Parameter(new Guid(data.LocalId))) as Select;
				else if (!string.IsNullOrEmpty(data.Id1C))
					selAcc = selAcc.Where("Account", "GenID1C").IsEqual(Column.Parameter(data.Id1C)) as Select;
				
			}
			else if (!string.IsNullOrEmpty(data.CreatedFrom) || !string.IsNullOrEmpty(data.CreatedTo))
			{
				if (!string.IsNullOrEmpty(data.CreatedFrom))
					selAcc = selAcc.Where("Account", "CreatedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(data.CreatedFrom))) as Select;
				else if (!string.IsNullOrEmpty(data.CreatedFrom) && !string.IsNullOrEmpty(data.CreatedTo))
					selAcc = selAcc.And("Account", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.CreatedTo))) as Select;
				else if (!string.IsNullOrEmpty(data.CreatedTo)) 
					selAcc = selAcc.Where("Account", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.CreatedTo))) as Select;
				
			}
			else if (!string.IsNullOrEmpty(data.ModifiedFrom) || !string.IsNullOrEmpty(data.ModifiedTo))
			{
				if (!string.IsNullOrEmpty(data.ModifiedFrom))
					selAcc = selAcc.Where("Account", "ModifiedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedFrom))) as Select;
				else if (!string.IsNullOrEmpty(data.ModifiedFrom) && !string.IsNullOrEmpty(data.ModifiedTo))
					selAcc = selAcc.And("Account", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedTo))) as Select;
				else if (!string.IsNullOrEmpty(data.ModifiedTo)) 
					selAcc = selAcc.Where("Account", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedTo))) as Select;
				
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
							LocalId = (reader.GetValue(0) != DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
							Id1C = (reader.GetValue(1) != DBNull.Value) ? (string)reader.GetValue(1) : "",
							Name = (reader.GetValue(2) != DBNull.Value) ? (string)reader.GetValue(2) : "",
							Code = (reader.GetValue(3) != DBNull.Value) ? (string)reader.GetValue(3) : "",
							OwnerLocalId = (reader.GetValue(14) != DBNull.Value) ? (string)reader.GetValue(14).ToString() : ""
						});
					}
				}
			}
			
			foreach (var account in result)
			{
				account.Addresses = addres.GetItem(account.LocalId);
			}
			
			return result;
		}
	}
	
	[DataContract]
	public class AccountAddres
	{
		[DataMember(Name = "Id")]
		public string Id1C { get; set; }
		[DataMember(Name = "BPMId")]
		public string LocalId { get; set; }
		[IgnoreDataMember]
		public Guid BpmId { get; set; }
		
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
			get =>
				_userConnection ??
				(_userConnection = HttpContext.Current.Session["UserConnection"] as UserConnection);
			set => _userConnection = value;
		}
		
		public string ProcessRemoteItem(bool isFull = true)
		{
			if ((!string.IsNullOrEmpty(LocalId) && LocalId != "00000000-0000-0000-0000-000000000000") ||
				(!string.IsNullOrEmpty(Id1C) && Id1C != "00000000-0000-0000-0000-000000000000"))
			{
				if (BpmId == Guid.Empty)
				{
					ResolveRemoteItem();
				}
				if (BpmId == Guid.Empty || isFull == true)
				{
					SaveRemoteItem();
				}
			}
			return BpmId.ToString();
		}
		
		public bool ResolveRemoteItem()
		{
			if (string.IsNullOrEmpty(LocalId) && string.IsNullOrEmpty(Id1C))
				return false;
			var selEntity = new Select(UserConnection)
				.Column("AccountAddress", "Id").Top(1)
				.From("AccountAddress").As("AccountAddress")
			as Select;
			if (!string.IsNullOrEmpty(LocalId))
				selEntity = selEntity.Where("AccountAddress", "Id").IsEqual(Column.Parameter(new Guid(LocalId))) as Select;
			else if (!string.IsNullOrEmpty(Id1C))
				selEntity = selEntity.Where("AccountAddress", "GenID1C").IsEqual(Column.Parameter(Id1C)) as Select;
			else
				return false;
			
			var entityId = selEntity.ExecuteScalar<Guid>();
			if (entityId == Guid.Empty) return false;
			BpmId = entityId;
			return true;
		}
		
		private bool SaveRemoteItem()
		{
			var success = false;
			var entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("AccountAddress").CreateEntity(UserConnection);
			var now = DateTime.Now;
			
			if (BpmId == Guid.Empty)
			{
				entity.SetDefColumnValues();
			}
			else if (!entity.FetchFromDB(entity.Schema.PrimaryColumn.Name, BpmId)) {
				entity.SetDefColumnValues();
			}
			
			if (!string.IsNullOrEmpty(Id1C))
			{
				entity.SetColumnValue("GenID1C", Id1C);
			}
			
			if (!string.IsNullOrEmpty(Address))
			{
				entity.SetColumnValue("Address", Address);
			}
			
			if (!string.IsNullOrEmpty(FullAddress))
			{
				entity.SetColumnValue("FullAddress", FullAddress);
			}
			
			entity.SetColumnValue("Primary", Primary);
			
			if (!string.IsNullOrEmpty(Account))
			{
				entity.SetColumnValue("AccountId", new Guid(Account));
			}
		
			if (entity.StoringState == StoringObjectState.Changed || BpmId == Guid.Empty)
			{
				entity.SetColumnValue("ModifiedOn", now);
				success = entity.Save(true);
			}
			else
			{
				success = true;
			}
			BpmId = (Guid)entity.GetColumnValue("Id");
			return success;
		}
		
		public List<AccountAddres> GetItem(string id)
		{
			var result = new List<AccountAddres>();
			var selEntity = new Select(UserConnection)
				.Column("AA", "Id")
				.Column("AA", "GenID1C")
				.Column("AA", "Address")
				.Column("AA", "FullAddress")
				.Column("AA", "Primary")
				.From("AccountAddress").As("AA")
				.Where("AA", "AccountId").IsEqual(Column.Parameter(new Guid(id)))
			as Select;
			
			using (var dbExecutor = UserConnection.EnsureDBConnection())
			{
				using (var reader = selEntity.ExecuteReader(dbExecutor))
				{
					while (reader.Read())
					{
						result.Add(new AccountAddres(){
							LocalId = (reader.GetValue(0) != DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
							Id1C = (reader.GetValue(1) != DBNull.Value) ? (string)reader.GetValue(1) : "",
							Address = (reader.GetValue(2) != DBNull.Value) ? (string)reader.GetValue(2) : "",
							FullAddress = (reader.GetValue(3) != DBNull.Value) ? (string)reader.GetValue(3) : "",
							Primary = (reader.GetValue(4) != DBNull.Value) && (bool)reader.GetValue(4)
						});	
					}
				}
			}
			return result;
		}
	}
}
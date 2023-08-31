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
	using Terrasoft.Configuration.OneCBaseEntity;

    [DataContract]
	public sealed class OneCAccount : OneCBaseEntity<OneCAccount>
	{	
		[DataMember(Name = "Name")]
		public string Name { get; set; }
		[DataMember(Name = "Code")]
		public string Code { get; set; }
		[DataMember(Name = "OwnerLocalId")]
		public string OwnerLocalId { get; set; }
		
		[DataMember(Name = "Addresses")]
		public List<OneCAccountAddres> Addresses { get; set; }
		
		public OneCBaseEntity<OneCAccount> ProcessRemoteItem(bool isFull = true)
		{
            return base.ProcessRemoteItem(isFull);
        }
		
		public override bool ResolveRemoteItem()
		{			
			var selEntity = new Select(UserConnection)
				.Column("Account", "Id").Top(1)
				.From("Account").As("Account")
			as Select;

            return base.ResolveRemoteItemByQuery(selEntity);
        }

        public override bool SaveRemoteItem()
        {
            var success = false;
            var oneCHelper = new OneCIntegrationHelper();

            Guid owner = Guid.Empty;

            if (oneCHelper.CheckId("Contact", OwnerLocalId))
            {
                owner = new Guid(OwnerLocalId);
            }

            var entity = UserConnection.EntitySchemaManager
                                   .GetInstanceByName("Account")
                                   .CreateEntity(UserConnection);

            if (BpmId == Guid.Empty || !entity.FetchFromDB(entity.Schema.PrimaryColumn.Name, BpmId))
            {
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

            entity.SetColumnValue("ModifiedOn", DateTime.Now);

            if (entity.StoringState == StoringObjectState.Changed || BpmId == Guid.Empty)
            {
                success = entity.Save(true);
            }
            else
            {
                success = true;
            }

            BpmId = (Guid)entity.GetColumnValue("Id");

            if (BpmId == Guid.Empty || Addresses == null || Addresses.Count <= 0)
            {
                return success;
            }

            foreach (var address in Addresses)
            {
                address.AccountId = BpmId.ToString();
                address.ProcessRemoteItem();
            }

            return success;
        }

        public override List<OneCAccount> GetItem(SearchFilter searchFilter)
		{
			var result = new List<OneCAccount>();
			var selCol = new Select(UserConnection)
				.Column("Account", "Id")
				.Column("Account", "GenID1C")
				.Column("Account", "Name")
				.Column("Account", "Code")
				.Column("Account", "OwnerId")
				.From("Account").As("Account")
				as Select;

            selCol = base.GetItemByFilters(selCol, searchFilter);

            using (var dbExecutor = UserConnection.EnsureDBConnection())
			{
				using (var reader = selCol.ExecuteReader(dbExecutor))
				{
					while (reader.Read())
					{
						result.Add(new OneCAccount() {
							LocalId = (reader.GetValue(0) != DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
							Id1C = (reader.GetValue(1) != DBNull.Value) ? (string)reader.GetValue(1) : "",
							Name = (reader.GetValue(2) != DBNull.Value) ? (string)reader.GetValue(2) : "",
							Code = (reader.GetValue(3) != DBNull.Value) ? (string)reader.GetValue(3) : "",
							OwnerLocalId = (reader.GetValue(4) != DBNull.Value) ? (string)reader.GetValue(4).ToString() : ""
						});
					}
				}
			}

            var addres = new OneCAccountAddres();
            foreach (var account in result)
			{
				account.Addresses = addres.GetItem(account.LocalId);
			}
			
			return result;
		}
	}
	
	[DataContract]
	public class OneCAccountAddres
	{
		[DataMember(Name = "Id")]
		public string Id1C { get; set; }
		[DataMember(Name = "BPMId")]
		public string LocalId { get; set; }
		[IgnoreDataMember]
		public Guid BpmId { get; set; }
		
		[DataMember(Name = "Address")]
		public string Address { get; set; }
		[DataMember(Name = "Primary")]
		public bool Primary { get; set; }
		[DataMember (Name = "Account")]
		public string AccountId { get; set; }
		
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
			
			entity.SetColumnValue("Primary", Primary);
			
			if (!string.IsNullOrEmpty(AccountId))
			{
				entity.SetColumnValue("AccountId", new Guid(AccountId));
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
		
		public List<OneCAccountAddres> GetItem(string id)
		{
			var result = new List<OneCAccountAddres>();
			var selEntity = new Select(UserConnection)
				.Column("AccountAddress", "Id")
				.Column("AccountAddress", "GenID1C")
				.Column("AccountAddress", "Address")
				.Column("AccountAddress", "Primary")
                .Column("AccountAddress", "AccountId")
                .From("AccountAddress")
				.Where("AccountAddress", "AccountId").IsEqual(Column.Parameter(new Guid(id)))
			as Select;
			
			using (var dbExecutor = UserConnection.EnsureDBConnection())
			{
				using (var reader = selEntity.ExecuteReader(dbExecutor))
				{
					while (reader.Read())
					{
						result.Add(new OneCAccountAddres(){
							LocalId = (reader.GetValue(0) != DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
							Id1C = (reader.GetValue(1) != DBNull.Value) ? (string)reader.GetValue(1) : "",
							Address = (reader.GetValue(2) != DBNull.Value) ? (string)reader.GetValue(2) : "",
							Primary = (reader.GetValue(3) != DBNull.Value) && (bool)reader.GetValue(3),
							AccountId = (reader.GetValue(4) != DBNull.Value) ? reader.GetValue(4).ToString() : "",
						});	
					}
				}
			}
			return result;
		}
	}
}
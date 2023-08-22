 using System.IO;

namespace Terrasoft.Configuration.GenOneCContract
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
	
	[DataContract]
	public class OneCContract
	{
		[DataMember(Name = "Id")]
		public string Id1C { get; set; }
		[DataMember(Name = "BPMId")]
		public string LocalId { get; set; }
		[IgnoreDataMember]
		public Guid BpmId { get; set; }
		
		[DataMember(Name = "Number")]
		public string Number { get; set; }
		[DataMember(Name = "Type")]
		public string Type { get; set; }
		[DataMember(Name = "CounterpartyLocalId")]
		public string CounterpartyLocalId { get; set; }
		
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
			if ((string.IsNullOrEmpty(this.LocalId) || this.LocalId == "00000000-0000-0000-0000-000000000000") &&
			    (string.IsNullOrEmpty(this.Id1C) || this.Id1C == "00000000-0000-0000-0000-000000000000"))
				return this.BpmId.ToString();
			if (this.BpmId == Guid.Empty)
			{
				this.ResolveRemoteItem();
			}
			if (this.BpmId == Guid.Empty || isFull == true)
			{
				this.SaveRemoteItem();
			}
			return this.BpmId.ToString();
		}
		
		public bool ResolveRemoteItem()
		{
			if (string.IsNullOrEmpty(this.LocalId) && string.IsNullOrEmpty(this.Id1C))
				return false;
			var selEntity = new Select(UserConnection)
				.Column("Contract", "Id").Top(1)
				.From("Contract").As("Contract")
			as Select;
			if (!string.IsNullOrEmpty(this.LocalId))
				selEntity = selEntity.Where("Contract", "Id").IsEqual(Column.Parameter(new Guid(this.LocalId))) as Select;
			else if (!string.IsNullOrEmpty(this.Id1C))
				selEntity = selEntity.Where("Contract", "GenID1C").IsEqual(Column.Parameter(this.Id1C)) as Select;
			else
				return false;
			
			var entityId = selEntity.ExecuteScalar<Guid>();
			if (entityId == Guid.Empty) return false;
			this.BpmId = entityId;
			return true;
		}
		
		private bool SaveRemoteItem()
		{
			var success = false;
			Directory directory = new Directory();
			var type = Guid.Empty;
			var counterparty = Guid.Empty;
			
			if (!string.IsNullOrEmpty(this.Type))
			{
				type = directory.GetId("ContractType", this.Type);
			}
			
			if (!string.IsNullOrEmpty(this.CounterpartyLocalId) && directory.СhekId("Account", this.CounterpartyLocalId))
			{
				counterparty = new Guid(this.CounterpartyLocalId);
			}

			var entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("Contract").CreateEntity(UserConnection);
			var now = DateTime.Now;
			
			if (this.BpmId == Guid.Empty)
			{
				entity.SetDefColumnValues();
			}
			else if (!entity.FetchFromDB(entity.Schema.PrimaryColumn.Name, this.BpmId)) {
				entity.SetDefColumnValues();
			}
			
			if (!string.IsNullOrEmpty(this.Id1C))
			{
				entity.SetColumnValue("GenID1C", this.Id1C);
			}
			
			if (!string.IsNullOrEmpty(this.Number))
			{
				entity.SetColumnValue("Number", this.Number);
			}
			
			if (type != Guid.Empty)
			{
				entity.SetColumnValue("TypeId", type);
			}
			
			if (counterparty != Guid.Empty)
			{
				entity.SetColumnValue("AccountId", counterparty);
			}
			
			entity.SetColumnValue("ModifiedOn", now);
		
			if (entity.StoringState == StoringObjectState.Changed || this.BpmId == Guid.Empty)
			{
				success = entity.Save(true);
			}
			else
			{
				success = true;
			}
			this.BpmId = (Guid)entity.GetColumnValue("Id");
			return success;
		}
		
		public List<OneCContract> GetItem(Search data)
		{
			var result = new List<OneCContract>();
			var date = DateTime.Now;
			
			var selCon = new Select(UserConnection)
				.Column("Contract", "Id")
				.Column("Contract", "GenID1C")
				.Column("Contract", "Number")
				.Column("ContractType", "Name")
				.Column("Contract", "AccountId")
				.From("Contract").As("Contract")
				.LeftOuterJoin("ContractType").As("ContractType")
					.On("ContractType", "Id").IsEqual("Contract", "TypeId")
			as Select;
			
			if (!string.IsNullOrEmpty(data.Id1C) || !string.IsNullOrEmpty(data.LocalId))
			{
				if (!string.IsNullOrEmpty(data.LocalId))
					selCon = selCon.Where("Contract", "Id").IsEqual(Column.Parameter(new Guid(data.LocalId))) as Select;
				else if (!string.IsNullOrEmpty(data.Id1C))
					selCon = selCon.Where("Contract", "GenID1C").IsEqual(Column.Parameter(data.Id1C)) as Select;
				
			}
			else if (!string.IsNullOrEmpty(data.CreatedFrom) || !string.IsNullOrEmpty(data.CreatedTo))
			{
				if (!string.IsNullOrEmpty(data.CreatedFrom))
					selCon = selCon.Where("Contract", "CreatedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(data.CreatedFrom))) as Select;
				else if (!string.IsNullOrEmpty(data.CreatedFrom) && !string.IsNullOrEmpty(data.CreatedTo))
					selCon = selCon.And("Contract", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.CreatedTo))) as Select;
				else if (!string.IsNullOrEmpty(data.CreatedTo)) 
					selCon = selCon.Where("Contract", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.CreatedTo))) as Select;
				
			}
			else if (!string.IsNullOrEmpty(data.ModifiedFrom) || !string.IsNullOrEmpty(data.ModifiedTo))
			{
				if (!string.IsNullOrEmpty(data.ModifiedFrom))
					selCon = selCon.Where("Contract", "ModifiedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedFrom))) as Select;
				else if (!string.IsNullOrEmpty(data.ModifiedFrom) && !string.IsNullOrEmpty(data.ModifiedTo))
					selCon = selCon.And("Contract", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedTo))) as Select;
				else if (!string.IsNullOrEmpty(data.ModifiedTo)) 
					selCon = selCon.Where("Contract", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedTo))) as Select;
				
			}	
			//else if (!string.IsNullOrEmpty(_AccountId)) 
			//{
			//	selCon = selCon.Where("Contract", "AccountId").IsEqual(Column.Parameter(new Guid(_AccountId))) as Select;
			//}	
			else 
			{
				return result;	
			}
			
			using (var dbExecutor = UserConnection.EnsureDBConnection())
			{
				using (var reader = selCon.ExecuteReader(dbExecutor))
				{
					while (reader.Read())
					{
						result.Add(new OneCContract(){
							LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
							Id1C = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
							Number = (reader.GetValue(2) != System.DBNull.Value) ? (string)reader.GetValue(2) : "",
							Type = (reader.GetValue(3) != System.DBNull.Value) ? (string)reader.GetValue(3) : "",
							CounterpartyLocalId = (reader.GetValue(4) != System.DBNull.Value) ? (string)reader.GetValue(4).ToString() : "",
						});
					}
				}
			}
			return result;
		}
	}
}
namespace Terrasoft.Configuration.UsrOneCContract
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
	public class OneCContract
	{
		[DataMember(Name = "Id")]
		public string ID1C { get; set; }
		[DataMember(Name = "BPMId")]
		public string LocalId { get; set; }
		[IgnoreDataMember]
		public Guid BPMId { get; set; }
		
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
				.Column("Contract", "Id").Top(1)
				.From("Contract").As("Contract")
			as Select;
			if (!string.IsNullOrEmpty(this.LocalId))
				_selEntity = _selEntity.Where("Contract", "Id").IsEqual(Column.Parameter(new Guid(this.LocalId))) as Select;
			else if (!string.IsNullOrEmpty(this.ID1C))
				_selEntity = _selEntity.Where("Contract", "GenID1C").IsEqual(Column.Parameter(this.ID1C)) as Select;
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
			Guid _Type = Guid.Empty;
			Guid _Counterparty = Guid.Empty;
			
			if (!string.IsNullOrEmpty(this.Type))
			{
				_Type = directory.GetId("ContractType", this.Type);
			}
			
			if (!string.IsNullOrEmpty(this.CounterpartyLocalId) && directory.СhekId("Account", this.CounterpartyLocalId))
			{
				_Counterparty = new Guid(this.CounterpartyLocalId);
			}

			var _entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("Contract").CreateEntity(UserConnection);
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
			
			if (!string.IsNullOrEmpty(this.Number))
			{
				_entity.SetColumnValue("Number", this.Number);
			}
			
			if (_Type != Guid.Empty)
			{
				_entity.SetColumnValue("TypeId", _Type);
			}
			
			if (_Counterparty != Guid.Empty)
			{
				_entity.SetColumnValue("AccountId", _Counterparty);
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
			return success;
		}
		
		public List<OneCContract> getItem(Search _data)
		{
			List<OneCContract> result = new List<OneCContract>();
			DateTime date = DateTime.Now;
			
			Select selCon = new Select(UserConnection)
				.Column("Contract", "Id")
				.Column("Contract", "GenID1C")
				.Column("Contract", "Number")
				.Column("ContractType", "Name")
				.Column("Contract", "AccountId")
				.From("Contract").As("Contract")
				.LeftOuterJoin("ContractType").As("ContractType")
					.On("ContractType", "Id").IsEqual("Contract", "TypeId")
			as Select;
			
			if (!string.IsNullOrEmpty(_data.ID1C) || !string.IsNullOrEmpty(_data.LocalId))
			{
				if (!string.IsNullOrEmpty(_data.LocalId))
					selCon = selCon.Where("Contract", "Id").IsEqual(Column.Parameter(new Guid(_data.LocalId))) as Select;
				else if (!string.IsNullOrEmpty(_data.ID1C))
					selCon = selCon.Where("Contract", "GenID1C").IsEqual(Column.Parameter(_data.ID1C)) as Select;
				
			}
			else if (!string.IsNullOrEmpty(_data.CreatedFrom) || !string.IsNullOrEmpty(_data.CreatedTo))
			{
				if (!string.IsNullOrEmpty(_data.CreatedFrom))
					selCon = selCon.Where("Contract", "CreatedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(_data.CreatedFrom))) as Select;
				else if (!string.IsNullOrEmpty(_data.CreatedFrom) && !string.IsNullOrEmpty(_data.CreatedTo))
					selCon = selCon.And("Contract", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(_data.CreatedTo))) as Select;
				else if (!string.IsNullOrEmpty(_data.CreatedTo)) 
					selCon = selCon.Where("Contract", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(_data.CreatedTo))) as Select;
				
			}
			else if (!string.IsNullOrEmpty(_data.ModifiedFrom) || !string.IsNullOrEmpty(_data.ModifiedTo))
			{
				if (!string.IsNullOrEmpty(_data.ModifiedFrom))
					selCon = selCon.Where("Contract", "ModifiedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(_data.ModifiedFrom))) as Select;
				else if (!string.IsNullOrEmpty(_data.ModifiedFrom) && !string.IsNullOrEmpty(_data.ModifiedTo))
					selCon = selCon.And("Contract", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(_data.ModifiedTo))) as Select;
				else if (!string.IsNullOrEmpty(_data.ModifiedTo)) 
					selCon = selCon.Where("Contract", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(_data.ModifiedTo))) as Select;
				
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
							ID1C = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
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
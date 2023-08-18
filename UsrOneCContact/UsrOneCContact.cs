namespace Terrasoft.Configuration.UsrOneCContact
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
	public class OneCContact
	{
		[DataMember(Name = "Id")]
		public string ID1C { get; set; }
		[DataMember(Name = "BPMId")]
		public string LocalId { get; set; }
		[IgnoreDataMember]
		public Guid BPMId { get; set; }
		
		[DataMember(Name = "Name")]
		public string Name { get; set; }
		[DataMember(Name = "Job")]
		public string Job { get; set; }
		[DataMember(Name = "DecisionRole")]
		public string DecisionRole { get; set; }
		
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
				.Column("Contact", "Id").Top(1)
				.From("Contact").As("Contact")
			as Select;
			if (!string.IsNullOrEmpty(this.LocalId))
				_selEntity = _selEntity.Where("Contact", "Id").IsEqual(Column.Parameter(new Guid(this.LocalId))) as Select;
			else if (!string.IsNullOrEmpty(this.ID1C))
				_selEntity = _selEntity.Where("Contact", "GenID1C").IsEqual(Column.Parameter(this.ID1C)) as Select;
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
			Guid _Job = Guid.Empty;
			Guid _DecisionRole = Guid.Empty;
			
			if (!string.IsNullOrEmpty(this.Job))
			{
				_Job = directory.GetId("Job", this.Job);
			}
			
			if (!string.IsNullOrEmpty(this.DecisionRole))
			{
				_DecisionRole = directory.GetId("ContactDecisionRole", this.DecisionRole);
			}

			var _entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("Contact").CreateEntity(UserConnection);
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
			
			if (_Job != Guid.Empty)
			{
				_entity.SetColumnValue("JobId", _Job);
			}
			
			if (_DecisionRole != Guid.Empty)
			{
				_entity.SetColumnValue("DecisionRoleId", _DecisionRole);
			}
			
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
		
		public List<OneCContact> getItem(Search _data)
		{
			List<OneCContact> result = new List<OneCContact>();
			
			Select selCon = new Select(UserConnection)
				.Column("Contact", "Id")
				.Column("Contact", "GenID1C")
				.Column("Contact", "Name")
				.Column("Job", "Name") //
				.Column("ContactDecisionRole", "Name") //
				.Column("Contact", "ModifiedOn")
				.From("Contact").As("Contact")
				.LeftOuterJoin("Job").As("Job")
					.On("Job", "Id").IsEqual("Contact", "JobId")
				.LeftOuterJoin("Country").As("Country")
					.On("Country", "Id").IsEqual("Contact", "CountryId")
				.LeftOuterJoin("ContactDecisionRole").As("ContactDecisionRole")
					.On("ContactDecisionRole", "Id").IsEqual("Contact", "DecisionRoleId")
			as Select;

			if (!string.IsNullOrEmpty(_data.ID1C) || !string.IsNullOrEmpty(_data.LocalId))
			{
				if (!string.IsNullOrEmpty(_data.LocalId))
					selCon = selCon.Where("Contact", "Id").IsEqual(Column.Parameter(new Guid(_data.LocalId))) as Select;
				else if (!string.IsNullOrEmpty(_data.ID1C))
					selCon = selCon.Where("Contact", "GenID1C").IsEqual(Column.Parameter(_data.ID1C)) as Select;
				
			}
			else if (!string.IsNullOrEmpty(_data.CreatedFrom) || !string.IsNullOrEmpty(_data.CreatedTo))
			{
				if (!string.IsNullOrEmpty(_data.CreatedFrom))
					selCon = selCon.Where("Contact", "CreatedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(_data.CreatedFrom))) as Select;
				else if (!string.IsNullOrEmpty(_data.CreatedFrom) && !string.IsNullOrEmpty(_data.CreatedTo))
					selCon = selCon.And("Contact", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(_data.CreatedTo))) as Select;
				else if (!string.IsNullOrEmpty(_data.CreatedTo)) 
					selCon = selCon.Where("Contact", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(_data.CreatedTo))) as Select;
				
			}
			else if (!string.IsNullOrEmpty(_data.ModifiedFrom) || !string.IsNullOrEmpty(_data.ModifiedTo))
			{
				if (!string.IsNullOrEmpty(_data.ModifiedFrom))
					selCon = selCon.Where("Contact", "ModifiedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(_data.ModifiedFrom))) as Select;
				else if (!string.IsNullOrEmpty(_data.ModifiedFrom) && !string.IsNullOrEmpty(_data.ModifiedTo))
					selCon = selCon.And("Contact", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(_data.ModifiedTo))) as Select;
				else if (!string.IsNullOrEmpty(_data.ModifiedTo)) 
					selCon = selCon.Where("Contact", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(_data.ModifiedTo))) as Select;
				
			}	
			//else if (!string.IsNullOrEmpty(_AccountId)) 
			//{
			//	selCon = selCon.Where("Contact", "AccountId").IsEqual(Column.Parameter(new Guid(_AccountId))) as Select;
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
						result.Add(new OneCContact(){
							LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
							ID1C = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
							Name = (reader.GetValue(2) != System.DBNull.Value) ? (string)reader.GetValue(2) : "",
							Job = (reader.GetValue(3) != System.DBNull.Value) ? (string)reader.GetValue(3) : "",
							DecisionRole = (reader.GetValue(4) != System.DBNull.Value) ? (string)reader.GetValue(4) : ""
						});	
					}
				}
			}

			return result;
		}
	}
}
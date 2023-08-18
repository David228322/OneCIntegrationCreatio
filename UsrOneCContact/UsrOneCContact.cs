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
		public string Id1C { get; set; }
		[DataMember(Name = "BPMId")]
		public string LocalId { get; set; }
		[IgnoreDataMember]
		public Guid BpmId { get; set; }
		
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
			get =>
				_userConnection ??
				(_userConnection = HttpContext.Current.Session["UserConnection"] as UserConnection);
			set => _userConnection = value;
		}
		
		public string ProcessRemoteItem(bool isFull = true)
		{
			if ((!string.IsNullOrEmpty(this.LocalId) && this.LocalId != "00000000-0000-0000-0000-000000000000") ||
				(!string.IsNullOrEmpty(this.Id1C) && this.Id1C != "00000000-0000-0000-0000-000000000000"))
			{
				if (this.BpmId == Guid.Empty)
				{
					this.ResolveRemoteItem();
				}
				if (this.BpmId == Guid.Empty || isFull == true)
				{
					this.SaveRemoteItem();
				}
			}
			return this.BpmId.ToString();
		}
		
		public bool ResolveRemoteItem()
		{
			if (string.IsNullOrEmpty(this.LocalId) && string.IsNullOrEmpty(this.Id1C))
				return false;
			var selEntity = new Select(UserConnection)
				.Column("Contact", "Id").Top(1)
				.From("Contact").As("Contact")
			as Select;
			if (!string.IsNullOrEmpty(this.LocalId))
				selEntity = selEntity.Where("Contact", "Id").IsEqual(Column.Parameter(new Guid(this.LocalId))) as Select;
			else if (!string.IsNullOrEmpty(this.Id1C))
				selEntity = selEntity.Where("Contact", "GenID1C").IsEqual(Column.Parameter(this.Id1C)) as Select;
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
			var directory = new Directory();
			var job = Guid.Empty;
			var decisionRole = Guid.Empty;
			
			if (!string.IsNullOrEmpty(this.Job))
			{
				job = directory.GetId("Job", this.Job);
			}
			
			if (!string.IsNullOrEmpty(this.DecisionRole))
			{
				decisionRole = directory.GetId("ContactDecisionRole", this.DecisionRole);
			}

			var entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("Contact").CreateEntity(UserConnection);
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
			
			if (!string.IsNullOrEmpty(this.Name))
			{
				entity.SetColumnValue("Name", this.Name);
			}
			
			if (job != Guid.Empty)
			{
				entity.SetColumnValue("JobId", job);
			}
			
			if (decisionRole != Guid.Empty)
			{
				entity.SetColumnValue("DecisionRoleId", decisionRole);
			}
			
			if (!string.IsNullOrEmpty(this.Account))
			{
				entity.SetColumnValue("AccountId", new Guid(this.Account));
			}
		
			if (entity.StoringState == StoringObjectState.Changed || this.BpmId == Guid.Empty)
			{
				entity.SetColumnValue("ModifiedOn", now);
				success = entity.Save(true);
			}
			else
			{
				success = true;
			}
			this.BpmId = (Guid)entity.GetColumnValue("Id");
			return success;
		}
		
		public List<OneCContact> GetItem(Search data)
		{
			var result = new List<OneCContact>();
			
			var selCon = new Select(UserConnection)
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

			if (!string.IsNullOrEmpty(data.Id1C) || !string.IsNullOrEmpty(data.LocalId))
			{
				if (!string.IsNullOrEmpty(data.LocalId))
					selCon = selCon.Where("Contact", "Id").IsEqual(Column.Parameter(new Guid(data.LocalId))) as Select;
				else if (!string.IsNullOrEmpty(data.Id1C))
					selCon = selCon.Where("Contact", "GenID1C").IsEqual(Column.Parameter(data.Id1C)) as Select;
				
			}
			else if (!string.IsNullOrEmpty(data.CreatedFrom) || !string.IsNullOrEmpty(data.CreatedTo))
			{
				if (!string.IsNullOrEmpty(data.CreatedFrom))
					selCon = selCon.Where("Contact", "CreatedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(data.CreatedFrom))) as Select;
				else if (!string.IsNullOrEmpty(data.CreatedFrom) && !string.IsNullOrEmpty(data.CreatedTo))
					selCon = selCon.And("Contact", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.CreatedTo))) as Select;
				else if (!string.IsNullOrEmpty(data.CreatedTo)) 
					selCon = selCon.Where("Contact", "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.CreatedTo))) as Select;
				
			}
			else if (!string.IsNullOrEmpty(data.ModifiedFrom) || !string.IsNullOrEmpty(data.ModifiedTo))
			{
				if (!string.IsNullOrEmpty(data.ModifiedFrom))
					selCon = selCon.Where("Contact", "ModifiedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedFrom))) as Select;
				else if (!string.IsNullOrEmpty(data.ModifiedFrom) && !string.IsNullOrEmpty(data.ModifiedTo))
					selCon = selCon.And("Contact", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedTo))) as Select;
				else if (!string.IsNullOrEmpty(data.ModifiedTo)) 
					selCon = selCon.Where("Contact", "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(data.ModifiedTo))) as Select;
				
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
							Id1C = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
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
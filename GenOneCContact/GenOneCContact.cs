namespace Terrasoft.Configuration.GenOneCContact
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
	public sealed class OneCContact : OneCBaseEntity<OneCContact>
    {	
		[DataMember(Name = "Name")]
		public string Name { get; set; }
		[DataMember(Name = "Job")]
		public string Job { get; set; }
		[DataMember(Name = "DecisionRole")]
		public string DecisionRole { get; set; }
		
		[DataMember (Name = "Account")]
		public string AccountId { get; set; }
		
		public string ProcessRemoteItem(bool isFull = true)
		{
            return base.ProcessRemoteItem(isFull);
        }
		
		public override bool ResolveRemoteItem()
		{
			var selEntity = new Select(UserConnection)
				.Column("Contact", "Id").Top(1)
				.From("Contact").As("Contact")
			as Select;

            return base.ResolveRemoteItemByQuery(selEntity);
        }
		
		public override bool SaveRemoteItem()
		{
			var success = false;
			var oneCHelper = new OneCIntegrationHelper();
			var job = Guid.Empty;
			var decisionRole = Guid.Empty;
			
			if (!string.IsNullOrEmpty(this.Job))
			{
				job = oneCHelper.GetId("Job", this.Job);
			}
			
			if (!string.IsNullOrEmpty(this.DecisionRole))
			{
				decisionRole = oneCHelper.GetId("ContactDecisionRole", this.DecisionRole);
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
			
			if (!string.IsNullOrEmpty(this.AccountId))
			{
				entity.SetColumnValue("AccountId", new Guid(this.AccountId));
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
		
		public override List<OneCContact> GetItem(SearchFilter searchFilter)
		{
			var result = new List<OneCContact>();
			
			var selCon = new Select(UserConnection)
				.Column("Contact", "Id")
				.Column("Contact", "GenID1C")
				.Column("Contact", "Name")
				.Column("Job", "Name")
				.Column("ContactDecisionRole", "Name")
				.Column("Contact", "AccountId")
				.From("Contact").As("Contact")
				.LeftOuterJoin("Job").As("Job")
					.On("Job", "Id").IsEqual("Contact", "JobId")
				.LeftOuterJoin("Country").As("Country")
					.On("Country", "Id").IsEqual("Contact", "CountryId")
				.LeftOuterJoin("ContactDecisionRole").As("ContactDecisionRole")
					.On("ContactDecisionRole", "Id").IsEqual("Contact", "DecisionRoleId")
			as Select;

            selCon = base.GetItemByFilters(selCon, searchFilter);

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
							DecisionRole = (reader.GetValue(4) != System.DBNull.Value) ? (string)reader.GetValue(4) : "",
                            AccountId = (reader.GetValue(5) != System.DBNull.Value) ? reader.GetValue(5).ToString() : ""
                        });	
					}
				}
			}

			return result;
		}
	}
}
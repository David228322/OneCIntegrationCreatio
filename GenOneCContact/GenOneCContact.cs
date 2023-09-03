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
		[DatabaseColumn("Contact", nameof(Name))]
		public string Name { get; set; }

        [DatabaseColumn(nameof(Job), "Name", "JobId")]
        [DataMember(Name = "Job")]
		public string Job { get; set; }

		[DataMember(Name = "DecisionRole")]
        [DatabaseColumn("ContactDecisionRole", "Name", "DecisionRoleId")]
        public string DecisionRole { get; set; }
		
		[DataMember (Name = "Account")]
        [DatabaseColumn("Contact", nameof(AccountId))]
        public Guid AccountId { get; set; }
		
		public OneCBaseEntity<OneCContact> ProcessRemoteItem(bool isFull = true)
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
			return base.SaveToDatabase();
		}
		
		public override List<OneCContact> GetItem(SearchFilter searchFilter)
		{
			return base.GetFromDatabase(searchFilter);
        }
	}
}
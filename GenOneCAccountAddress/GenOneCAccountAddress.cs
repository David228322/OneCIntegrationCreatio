 namespace Terrasoft.Configuration.GenOneCAccountAddress
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
    public sealed class OneCAccountAddress : OneCBaseEntity<OneCAccountAddress>
    {
        [DataMember(Name = "Address")]
        [DatabaseColumn("AccountAddress", nameof(Address))]
        public string Address { get; set; }

        [DataMember(Name = "Primary")]
        [DatabaseColumn("AccountAddress", nameof(Primary))]
        public bool Primary { get; set; }

        [DataMember(Name = "AccountId")]
        [DatabaseColumn("AccountAddress", nameof(AccountId))]
        public Guid AccountId { get; set; }

        public OneCBaseEntity<OneCAccountAddress> ProcessRemoteItem(bool isFull = true)
        {
            return base.ProcessRemoteItem(isFull);
        }
        public List<OneCAccountAddress> GetItem(SearchFilter searchFilter, string accountId)
        {
            return base.GetFromDatabase(searchFilter, new Dictionary<string, string> { { "AccountId", accountId } });
        }

        public override List<OneCAccountAddress> GetItem(SearchFilter searchFilter)
        {
            return base.GetFromDatabase(searchFilter);
        }
    }
 }
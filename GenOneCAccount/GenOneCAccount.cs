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
    using Terrasoft.Configuration.GenOneCAccountAddress;

    [DataContract]
    public sealed class OneCAccount : OneCBaseEntity<OneCAccount>
    {
        [DataMember(Name = "Name")]
        [DatabaseColumn("Account", nameof(Name))]
        public string Name { get; set; }

        [DataMember(Name = "Code")]
        [DatabaseColumn("Account", nameof(Code))]
        public string Code { get; set; }

        [DataMember(Name = "OwnerLocalId")]
        [DatabaseColumn("Account", "OwnerId")]
        public Guid OwnerId { get; set; }

        [DataMember(Name = "Addresses")]
        public List<OneCAccountAddress> Addresses { get; set; }

        public OneCBaseEntity<OneCAccount> ProcessRemoteItem(bool isFull = true)
        {
            return base.ProcessRemoteItem(isFull);
        }

        public bool SaveRemoteItem()
        {
            var accounts = base.SaveRemoteItem();

            foreach (var address in Addresses)
            {
                address.AccountId = BpmId;
                address.ProcessRemoteItem();
            }

            return accounts;
        }

        public override List<OneCAccount> GetItem(SearchFilter searchFilter)
        {
            var result = base.GetFromDatabase(searchFilter);

            var address = new OneCAccountAddress();
              foreach (var account in result)
              {
                  account.Addresses = address.GetItem(null, account.LocalId);
              }
          
            return result;
        }
    }
}
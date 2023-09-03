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
            var accounts = base.SaveToDatabase();

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

        public override bool ResolveRemoteItem()
        {
            var selEntity = new Select(UserConnection)
                .Column("AccountAddress", "Id").Top(1)
                .From("AccountAddress").As("AccountAddress")
            as Select;

            return base.ResolveRemoteItemByQuery(selEntity);
        }

        public override bool SaveRemoteItem()
        {
            return base.SaveToDatabase();
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
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
    using System.IO;

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
    public sealed class OneCContract : OneCBaseEntity<OneCContract>
    {
        [DataMember(Name = "Number")]
        [DatabaseColumn("Contract", nameof(Number))]
        public string Number { get; set; }

        [DataMember(Name = "Type")]
        [DatabaseColumn("ContractType", "Name", "TypeId")]
        public string Type { get; set; }

        [DataMember(Name = "ContactId")]
        [DatabaseColumn("Contract", nameof(ContactId))]
        public Guid ContactId { get; set; }

        public OneCBaseEntity<OneCContract> ProcessRemoteItem(bool isFull = true)
        {
            return base.ProcessRemoteItem(isFull);
        }

        public override List<OneCContract> GetItem(SearchFilter searchFilter)
        {
            var result = base.GetFromDatabase(searchFilter);
            return result;
        }
    }
}
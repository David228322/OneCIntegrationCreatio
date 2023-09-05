namespace Terrasoft.Configuration.GenOneCSpecificationInProduct
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

    using Terrasoft.Configuration.GenOneCSvcIntegration;
    using Terrasoft.Configuration.GenOneCIntegrationHelper;
    using Terrasoft.Configuration.OneCBaseEntity;

    [DataContract]
    public class OneCSpecificationInProduct : OneCBaseEntity<OneCSpecificationInProduct>
    {
        [DataMember(Name = "StringValue")]
        [DatabaseColumn("SpecificationInProduct", nameof(StringValue))]
        public string StringValue { get; set; }

        [DataMember(Name = "ProductId")]
        [DatabaseColumn("SpecificationInProduct", nameof(ProductId))]
        public Guid ProductId { get; set; }

        public OneCBaseEntity<OneCSpecificationInProduct> ProcessRemoteItem(bool isFull = true)
        {
            return base.ProcessRemoteItem(isFull);
        }

        public override bool ResolveRemoteItem()
        {
            Select selEntity = new Select(UserConnection)
                .Column("SpecificationInProduct", "Id").Top(1)
                .From("SpecificationInProduct").As("SpecificationInProduct")
            as Select;

            return base.ResolveRemoteItemByQuery(selEntity);
        }

        public override bool SaveRemoteItem()
        {
            base.SaveToDatabase();
            return true;
        }

        public override List<OneCSpecificationInProduct> GetItem(SearchFilter searchFilter)
        {
            var result = base.GetFromDatabase(searchFilter);
            return result;
        }

        public List<OneCSpecificationInProduct> GetItem(string productId)
        {
            return base.GetFromDatabase(null, new Dictionary<string, string>() { { "ProductId", productId } });
        }
    }
}

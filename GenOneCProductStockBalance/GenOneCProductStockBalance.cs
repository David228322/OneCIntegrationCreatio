namespace Terrasoft.Configuration.GenOneCProductStockBalance
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

    using Terrasoft.Configuration.GenOneCIntegrationHelper;
    using Terrasoft.Configuration.OneCBaseEntity;
    using Terrasoft.Configuration.GenOneCSvcIntegration;

    [DataContract]
    public class OneCProductStockBalance : OneCBaseEntity<OneCProductStockBalance>
    {
        [DataMember(Name = "ProductId")]
        [DatabaseColumn("ProductStockBalance", nameof(ProductId))]
        public Guid ProductId { get; set; }

        [DataMember(Name = "WarehouseId")]
        [DatabaseColumn("ProductStockBalance", nameof(WarehouseId))]
        public Guid WarehouseId { get; set; }

        [DataMember(Name = "AvailableQuantity")]
        [DatabaseColumn("ProductStockBalance", nameof(AvailableQuantity))]
        public decimal AvailableQuantity { get; set; }

        [DataMember(Name = "ReserveQuantity")]
        [DatabaseColumn("ProductStockBalance", nameof(ReserveQuantity))]
        public decimal ReserveQuantity { get; set; }

        [DataMember(Name = "TotalQuantity")]
        [DatabaseColumn("ProductStockBalance", nameof(TotalQuantity))]
        public decimal TotalQuantity { get; set; }

        public OneCBaseEntity<OneCProductStockBalance> ProcessRemoteItem(bool isFull = true)
        {
            return base.ProcessRemoteItem(isFull);
        }

        public override List<OneCProductStockBalance> GetItem(SearchFilter searchFilter)
        {
            var result = base.GetFromDatabase(searchFilter);
            return result;
        }

        public List<OneCProductStockBalance> GetItem(string productId)
        {
            return base.GetFromDatabase(null, new Dictionary<string, string>() { { "ProductId", productId } });
        }
    }
}

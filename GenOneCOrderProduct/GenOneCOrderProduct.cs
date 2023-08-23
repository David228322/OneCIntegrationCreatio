 namespace Terrasoft.Configuration.GenCOrderProduct
{
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.ServiceModel.Activation;
    using System.Linq;
    using System.Runtime.Serialization;
    using Terrasoft.Web.Common;
    using Web.Http.Abstractions;

    using System.Diagnostics;
    using System;
    using Core;
    using Core.DB;
    using Core.Entities;
    using Terrasoft.Core.Configuration;
    using Common;
    using System.Globalization;

    using Terrasoft.Configuration.GenIntegrationLogHelper;
    using Terrasoft.Configuration.GenOneCSvcIntegration;
    using Terrasoft.Configuration.OneCBaseEntity;
	
	[DataContract]
    public class OneCOrderProduct
    {
        [DataMember(Name = "Id")]
        public string Id1C { get; set; }
        [DataMember(Name = "BPMId")]
        public string LocalId { get; set; }
        [IgnoreDataMember]
        public Guid BpmId { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }
        [DataMember(Name = "Price")]
        public decimal Price { get; set; }
        [DataMember(Name = "Quantity")]
        public decimal Quantity { get; set; }
        [DataMember(Name = "DiscountPercent")]
        public decimal DiscountPercent { get; set; }
        [DataMember(Name = "TotalAmount")]
        public decimal TotalAmount { get; set; }
        [DataMember(Name = "Unit")]
        public string Unit { get; set; }

        [DataMember(Name = "ProductId")]
        public Guid ProductId { get; set; }
        //public OneCProduct OneCProduct { get; set; }
        
        [IgnoreDataMember]
        private UserConnection _userConnection;
        [IgnoreDataMember]
        public UserConnection UserConnection
        {
            get =>
                _userConnection ??
                (_userConnection = HttpContext.Current.Session["UserConnection"] as UserConnection);
            set => _userConnection = value;
        }

        public string ProcessRemoteItem(bool isFull = true)
        {
            throw new NotImplementedException();
        }

        public bool ResolveRemoteItem()
        {
            if (string.IsNullOrEmpty(LocalId) && string.IsNullOrEmpty(Id1C))
                return false;
            var selEntity = new Select(UserConnection)
                .Column("OrderProduct", "Id").Top(1)
                .From("OrderProduct")
            as Select;
            if (!string.IsNullOrEmpty(LocalId))
                selEntity = selEntity.Where("OrderProduct", "Id").IsEqual(Column.Parameter(new Guid(LocalId))) as Select;
            else
                return false;

            var entityId = selEntity.ExecuteScalar<Guid>();
            if (entityId == Guid.Empty) return false;
            BpmId = entityId;
            return true;
        }

        public List<OneCOrderProduct> GetItem(string localId)
        {
            var result = new List<OneCOrderProduct>();
            var date = DateTime.Now;

            var selCon = new Select(UserConnection)
                    .Column("OrderProduct", "Id")
                    .Column("OrderProduct", "Name")
                    .Column("OrderProduct", "Price")
                    .Column("OrderProduct", "Quantity")
                    .Column("OrderProduct", "DiscountPercent")
                    .Column("OrderProduct", "TotalAmount")
                    .Column("Unit", "Name").As("Unit")
                    .Column("OrderProduct", "ProductId")
                    .From("OrderProduct")
                    .LeftOuterJoin("Unit").On("OrderProduct", "UnitId").IsEqual("Unit", "Id")
                    .Where("OrderProduct", "OrderId").IsEqual(Column.Parameter(new Guid(localId)))
                as Select;

            using (var dbExecutor = UserConnection.EnsureDBConnection())
            {
                using (var reader = selCon.ExecuteReader(dbExecutor))
                {
                    while (reader.Read())
                    {
                        result.Add(new OneCOrderProduct()
                        {
                            LocalId = (reader.GetValue(0) != System.DBNull.Value) ? (string)reader.GetValue(0).ToString() : "",
                            Name = (reader.GetValue(1) != System.DBNull.Value) ? (string)reader.GetValue(1) : "",
                            Price = (reader.GetValue(2) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(2).ToString()) : 0,
                            Quantity = (reader.GetValue(3) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(3).ToString()) : 0,
                            DiscountPercent = (reader.GetValue(4) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(4).ToString()) : 0,
                            TotalAmount = (reader.GetValue(5) != System.DBNull.Value) ? decimal.Parse(reader.GetValue(5).ToString()) : 0,
                            Unit = (reader.GetValue(6) != System.DBNull.Value) ? (string)reader.GetValue(6).ToString() : "",
                            ProductId = (reader.GetValue(7) != System.DBNull.Value) ? Guid.Parse(reader.GetValue(7).ToString()) : Guid.Empty,
                        });
                    }
                }
            }
            return result;
        }
    }
 }
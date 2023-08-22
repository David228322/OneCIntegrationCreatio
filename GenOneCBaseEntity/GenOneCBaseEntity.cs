namespace Terrasoft.Configuration.OneCBaseEntity
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

    using Configuration.GenIntegrationLogHelper;
    using Configuration.GenOneCSvcIntegration;

    [DataContract]
    public abstract class OneCBaseEntity<T>
    {
        [DataMember(Name = "BPMId")]
        public string LocalId { get; set; }
        [DataMember(Name = "Id")]
        public string Id1C { get; set; }
        [IgnoreDataMember]
        public Guid BpmId { get; set; }


        [DataMember(Name = "CreatedOn")]
        public string CreatedOn { get; set; }
        [DataMember(Name = "ModifiedOn")]
        public string ModifiedOn { get; set; }


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
            if (string.IsNullOrEmpty(LocalId) || LocalId == "00000000-0000-0000-0000-000000000000")
            {
                return BpmId.ToString();
            }
            if (BpmId == Guid.Empty)
            {
                ResolveRemoteItem();
            }
            if (BpmId == Guid.Empty || isFull)
            {
                SaveRemoteItem();
            }
            return BpmId.ToString();
        }

        public abstract bool ResolveRemoteItem();

        public abstract bool SaveRemoteItem();

        public abstract List<T> GetItem(Search data);
    }
}
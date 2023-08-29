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
    using Terrasoft.Configuration.OneCBaseEntity;

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
        public string Entity => typeof(T).Name.Replace("OneC", "");

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

        public abstract bool SaveRemoteItem();
        public abstract List<T> GetItem(SearchFilter searchFilter);
        public abstract bool ResolveRemoteItem();

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

        protected Select GetItemByFilters(Select selectQuery, SearchFilter searchFilters)
        {
            if (!string.IsNullOrEmpty(searchFilters.Id1C))
            {
                selectQuery = selectQuery.Where(Entity, "GenID1C").IsEqual(Column.Parameter(searchFilters.Id1C)) as Select;
            }
            else if (!string.IsNullOrEmpty(searchFilters.LocalId))
            {
                selectQuery = selectQuery.Where(Entity, "Id").IsEqual(Column.Parameter(new Guid(searchFilters.LocalId))) as Select;
            }
            else if (!string.IsNullOrEmpty(searchFilters.CreatedFrom) || !string.IsNullOrEmpty(searchFilters.CreatedTo))
            {
                if (!string.IsNullOrEmpty(searchFilters.CreatedFrom))
                {
                    selectQuery = selectQuery.Where(Entity, "CreatedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(searchFilters.CreatedFrom))) as Select;
                }
                if (!string.IsNullOrEmpty(searchFilters.CreatedTo))
                {
                    selectQuery = selectQuery.And(Entity, "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(searchFilters.CreatedTo))) as Select;
                }
            }
            else if (!string.IsNullOrEmpty(searchFilters.ModifiedFrom) || !string.IsNullOrEmpty(searchFilters.ModifiedTo))
            {
                if (!string.IsNullOrEmpty(searchFilters.ModifiedFrom))
                {
                    selectQuery = selectQuery.Where(Entity, "ModifiedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(searchFilters.ModifiedFrom))) as Select;
                }
                if (!string.IsNullOrEmpty(searchFilters.ModifiedTo))
                {
                    selectQuery = selectQuery.And(Entity, "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(searchFilters.ModifiedTo))) as Select;
                }
            }

            return selectQuery;
        }

        protected bool ResolveRemoteItemByQuery(Select selectQuery)
        {
            if (string.IsNullOrEmpty(LocalId) && string.IsNullOrEmpty(Id1C))
            {
                return false;
            }

            var success = false;

            if (!string.IsNullOrEmpty(LocalId))
            {
                selectQuery.And(Entity, "Id").IsEqual(Column.Parameter(new Guid(LocalId)));
            }
            else if (!string.IsNullOrEmpty(Id1C))
            {
                selectQuery.And(Entity, "GenID1C").IsEqual(Column.Parameter(Id1C));
            }
            else
            {
                return false;
            }

            var entityId = selectQuery.ExecuteScalar<Guid>();
            if (entityId != Guid.Empty)
            {
                BpmId = entityId;
                success = true;
            }

            return success;
        }
    }
}
namespace Terrasoft.Configuration.UsrOneCProduct
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

	using Configuration.UsrIntegrationLogHelper;
	using Configuration.UsrOneCSvcIntegration;
	
	[DataContract]
	public class OneCProduct
	{
		[DataMember(Name = "Id")]
		public string Id1C { get; set; }
		[DataMember(Name = "BPMId")]
		public string LocalId { get; set; }
		[IgnoreDataMember]
		public Guid BpmId { get; set; }
		
		[DataMember(Name = "LangCode")]
		public string LangCode { get; set; }
		
		[DataMember(Name = "Name")] //lcz
		public string Name { get; set; }	
		[DataMember(Name = "Unit")]
		public string Unit { get; set; }
		[DataMember(Name = "Type")]
		public string Type { get; set; }
		[DataMember(Name = "Description")]
		public string Description { get; set; }
		[DataMember(Name = "ShortDescription")]
		public string ShortDescription { get; set; }
		[DataMember(Name = "Currency")]
		public string Currency { get; set; }
		[DataMember(Name = "Price")]
		public decimal Price { get; set; }
		
		[DataMember(Name = "CreatedOn")]
		public string CreatedOn { get; set; }
		[DataMember(Name = "ModifiedOn")]
		public string ModifiedOn { get; set; }
		

		[IgnoreDataMember]
		private UserConnection _userConnection;
		[IgnoreDataMember]
		public UserConnection UserConnection {
			get =>
				_userConnection ??
				(_userConnection = HttpContext.Current.Session["UserConnection"] as UserConnection);
			set => _userConnection = value;
		}
		
		public string ProcessRemoteItem(bool isFull = true)
		{
			if ((string.IsNullOrEmpty(LocalId) || LocalId == "00000000-0000-0000-0000-000000000000") 
			     && string.IsNullOrEmpty(LangCode))
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
		
		public bool ResolveRemoteItem()
		{
			if (string.IsNullOrEmpty(LocalId) && string.IsNullOrEmpty(Id1C))
				return false;
			var selEntity = new Select(UserConnection)
				.Column("Product", "Id").Top(1)
				.From("Product").As("Product")
			as Select;
			if (!string.IsNullOrEmpty(LocalId))
				selEntity = selEntity.Where("Product", "Id").IsEqual(Column.Parameter(new Guid(LocalId))) as Select;
			else
				return false;
			
			var entityId = selEntity.ExecuteScalar<Guid>();
			if (entityId == Guid.Empty) return false;
			BpmId = entityId;
			return true;
		}
		
		private bool SaveRemoteItem()
		{
			var success = false;
			var directory = new Directory();
			var unit = Guid.Empty;
			var residueStorageUnit = Guid.Empty;
			var reportingUnit = Guid.Empty;
			var unitOfSeats = Guid.Empty;
			var type = Guid.Empty;
			var nomenclatureGroup = Guid.Empty;
			var responsibleForPurchases = Guid.Empty;

			if (!string.IsNullOrEmpty(Unit))
			{
				unit = directory.GetId("Unit", Unit);
			}

			if (!string.IsNullOrEmpty(Type))
			{
				type = directory.GetId("ProductType", Type);
			}

			var entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("Product").CreateEntity(UserConnection);
			var now = DateTime.Now;
			
			if (BpmId == Guid.Empty)
			{
				entity.SetDefColumnValues();
			}
			else if (!entity.FetchFromDB(entity.Schema.PrimaryColumn.Name, BpmId)) {
				entity.SetDefColumnValues();
			}
			
			if (!string.IsNullOrEmpty(Id1C))
			{
				var localizableId1C = new LocalizableString();
				localizableId1C.SetCultureValue(new CultureInfo(LangCode), Id1C);
				entity.SetColumnValue("GenID1C", localizableId1C);
			}
			
			if (!string.IsNullOrEmpty(Name))
			{
				var localizableString = new LocalizableString();
				localizableString.SetCultureValue(new CultureInfo(LangCode), Name);
				entity.SetColumnValue("Name", localizableString);
			}

			if (unit != Guid.Empty)
			{
				entity.SetColumnValue("UnitId", unit);
			}
			
			if (residueStorageUnit != Guid.Empty)
			{
				entity.SetColumnValue("GenResidueStorageUnitId", residueStorageUnit);
			}
			
			if (reportingUnit != Guid.Empty)
			{
				entity.SetColumnValue("GenReportingUnitId", reportingUnit);
			}
			
			if (unitOfSeats != Guid.Empty)
			{
				entity.SetColumnValue("GenUnitOfSeatsId", unitOfSeats);
			}

			if (type != Guid.Empty)
			{
				entity.SetColumnValue("TypeId", type);
			}
			
			if (nomenclatureGroup != Guid.Empty)
			{
				entity.SetColumnValue("GenNomenclatureGroupId", nomenclatureGroup);
			}
			
			if (responsibleForPurchases != Guid.Empty)
			{
				entity.SetColumnValue("GenResponsibleForPurchasesId", responsibleForPurchases);
			}

			entity.SetColumnValue("ModifiedOn", now);
		
			if (entity.StoringState == StoringObjectState.Changed || BpmId == Guid.Empty)
			{
				success = entity.Save(true);
			}
			else
			{
				success = true;
			}
			BpmId = (Guid)entity.GetColumnValue("Id");

			return success;
		}
		
		public void SetAssociatedProduct(Guid productId)
		{
			var selEntity = new Select(UserConnection)
				.Column("Id").Top(1)
				.From("GenAssociateProduct")
				.Where("GenProductId").IsEqual(Column.Parameter(productId))
				.And("GenAssociatedProductId").IsEqual(Column.Parameter(BpmId))
			as Select;
			
			var ap = selEntity.ExecuteScalar<Guid>();

			if (ap != Guid.Empty) return;
			var entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("GenAssociateProduct").CreateEntity(UserConnection);

			entity.SetDefColumnValues();
				
			entity.SetColumnValue("GenProductId", productId);
			entity.SetColumnValue("GenAssociatedProductId", BpmId);
			entity.Save(true);
		}
	}
}
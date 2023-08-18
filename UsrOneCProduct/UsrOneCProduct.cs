namespace Terrasoft.Configuration.UsrOneCProduct
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

	using Terrasoft.Configuration.UsrIntegrationLogHelper;
	using Terrasoft.Configuration.UsrOneCSvcIntegration;
	
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
		
		
		//[DataMember (Name = "StockBalance")]
		//public List<OneCOneProductStockBalance> StockBalance { get; set; }
		//[DataMember (Name = "PriceList")]
		//public List<OneCOneProductPrice> PriceList { get; set; }
		//[DataMember (Name = "Units")]
		//public List<OneCOneProductUnits> Units { get; set; }
		//[DataMember (Name = "Specification")]
		//public List<OneCOneSpecificationInProduct> Specification { get; set; }
		//[DataMember (Name = "Barcodes")]
		//public List<OneCOneProductBarcodes> Barcodes { get; set; }
		//[DataMember (Name = "AssociatedProducts")]
		//public List<OneCProduct> AssociatedProducts { get; set; }
		//[DataMember (Name = "Properties")]
		//public List<OneCOneProductProperties> Properties { get; set; }
		
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
			if (((string.IsNullOrEmpty(this.LocalId) || this.LocalId == "00000000-0000-0000-0000-000000000000") &&
			     string.IsNullOrEmpty(this.VendorCode)) ||
			    string.IsNullOrEmpty(this.LangCode)) return this.BpmId.ToString();
			if (this.BpmId == Guid.Empty)
			{
				this.ResolveRemoteItem();
			}
			if (this.BpmId == Guid.Empty || isFull == true)
			{
				this.SaveRemoteItem();
			}
			return this.BpmId.ToString();
		}
		
		public bool ResolveRemoteItem()
		{
			if (string.IsNullOrEmpty(this.LocalId) && string.IsNullOrEmpty(this.Id1C))
				return false;
			var selEntity = new Select(UserConnection)
				.Column("Product", "Id").Top(1)
				.From("Product").As("Product")
			as Select;
			if (!string.IsNullOrEmpty(this.LocalId))
				selEntity = selEntity.Where("Product", "Id").IsEqual(Column.Parameter(new Guid(this.LocalId))) as Select;
			else if (!string.IsNullOrEmpty(this.Id1C))
				selEntity = selEntity.Where("Product", "Code").IsEqual(Column.Parameter(this.VendorCode)) as Select; // він один для усіх товарів в усіх 1Сках має бути 
			else
				return false;
			
			var entityId = selEntity.ExecuteScalar<Guid>();
			if (entityId == Guid.Empty) return false;
			this.BpmId = entityId;
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
			var mainSupplier = Guid.Empty;
			
			if (!string.IsNullOrEmpty(this.Unit))
			{
				unit = directory.GetId("Unit", this.Unit);
			}
			
			if (!string.IsNullOrEmpty(this.ResidueStorageUnit))
			{
				residueStorageUnit = directory.GetId("Unit", this.ResidueStorageUnit);
			}
			
			if (!string.IsNullOrEmpty(this.ReportingUnit))
			{
				reportingUnit = directory.GetId("Unit", this.ReportingUnit);
			}
			
			if (!string.IsNullOrEmpty(this.UnitOfSeats))
			{
				unitOfSeats = directory.GetId("Unit", this.UnitOfSeats);
			}
			
			if (!string.IsNullOrEmpty(this.Type))
			{
				type = directory.GetId("ProductType", this.Type);
			}
			
			if (!string.IsNullOrEmpty(this.NomenclatureGroup))
			{
				nomenclatureGroup = directory.GetId("GenNomenclatureGroup", this.NomenclatureGroup);
			}
			
			if (!string.IsNullOrEmpty(this.ResponsibleForPurchasesLocalId) && directory.СhekId("Contact", this.ResponsibleForPurchasesLocalId))
			{
				responsibleForPurchases = new Guid(this.ResponsibleForPurchasesLocalId);
			}
			
			if (!string.IsNullOrEmpty(this.MainSupplier))
			{
				//_MainSupplier = directory.GetId("Contact", this.MainSupplier);
			}
			
			

			var entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("Product").CreateEntity(UserConnection);
			var now = DateTime.Now;
			
			if (this.BpmId == Guid.Empty)
			{
				entity.SetDefColumnValues();
			}
			else if (!entity.FetchFromDB(entity.Schema.PrimaryColumn.Name, this.BpmId)) {
				entity.SetDefColumnValues();
			}
			
			if (!string.IsNullOrEmpty(this.Id1C))
			{
				var localizableId1C = new LocalizableString();
				localizableId1C.SetCultureValue(new CultureInfo(this.LangCode), this.Id1C);
				entity.SetColumnValue("GenID1C", localizableId1C);
			}
			
			if (!string.IsNullOrEmpty(this.Name))
			{
				var localizableString = new LocalizableString();
				localizableString.SetCultureValue(new CultureInfo(this.LangCode), this.Name);
				entity.SetColumnValue("Name", localizableString);
			}
			
			if (!string.IsNullOrEmpty(this.NameForInvoices))
			{
				var localizableNameForInvoices = new LocalizableString();
				localizableNameForInvoices.SetCultureValue(new CultureInfo(this.LangCode), this.NameForInvoices);
				entity.SetColumnValue("GenNameForInvoices", localizableNameForInvoices);
			}
			
			if (!string.IsNullOrEmpty(this.FullName))
			{
				var localizableFullName = new LocalizableString();
				localizableFullName.SetCultureValue(new CultureInfo(this.LangCode), this.FullName);
				entity.SetColumnValue("GenFullName", localizableFullName);
			}
			
			if (!string.IsNullOrEmpty(this.VendorCode))
			{
				entity.SetColumnValue("Code", this.VendorCode);
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
			
			entity.SetColumnValue("GenKeepTrackAdditionalCharacteristics", this.KeepTrackAdditionalCharacteristics);
			
			entity.SetColumnValue("GenKeepRecordsBySeries", this.KeepRecordsBySeries);
			
			entity.SetColumnValue("GenKeepPartyRecordsBySeries", this.KeepPartyRecordsBySeries);
			
			entity.SetColumnValue("GenHeavyGoods", this.HeavyGoods);
			
			entity.SetColumnValue("GenStrictAccountingForm", this.StrictAccountingForm);
			
			if (!string.IsNullOrEmpty(this.AtOKTZ))
			{
				entity.SetColumnValue("GenAtOKTZ", this.AtOKTZ);
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
			
			//if (_MainSupplier != Guid.Empty)
			//{
			//	_entity.SetColumnValue("GenMainSupplierId", _MainSupplier);
			//}
			
			if (!string.IsNullOrEmpty(this.ActionWith))
			{
				entity.SetColumnValue("GenActionWith", DateTime.Parse(this.ActionWith));
			}
			
			if (!string.IsNullOrEmpty(this.ActionOn))
			{
				entity.SetColumnValue("GenActionOn", DateTime.Parse(this.ActionOn));
			}
			
			if (!string.IsNullOrEmpty(this.SearchWords))
			{
				var localizableSearchWords = new LocalizableString();
				localizableSearchWords.SetCultureValue(new CultureInfo(this.LangCode), this.SearchWords);
				entity.SetColumnValue("GenSearchWords", localizableSearchWords);
			}
			
			if (!string.IsNullOrEmpty(this.LinkToTheSitePhoto))
			{
				var localizableLinkToTheSitePhoto = new LocalizableString();
				localizableLinkToTheSitePhoto.SetCultureValue(new CultureInfo(this.LangCode), this.LinkToTheSitePhoto);
				entity.SetColumnValue("GenLinkToTheSitePhoto", localizableLinkToTheSitePhoto);
			}
			
			if (!string.IsNullOrEmpty(this.Size))
			{
				entity.SetColumnValue("GenSize", this.Size);
			}
			
			if (this.Width > 0)
			{
				entity.SetColumnValue("GenWidth", this.Width);
			}
			
			if (this.Length > 0)
			{
				entity.SetColumnValue("GenLength", this.Length);
			}
			
			if (this.Thickness > 0)
			{
				entity.SetColumnValue("GenThickness", this.Thickness);
			}
			
			
			entity.SetColumnValue("ModifiedOn", now);
		
			if (entity.StoringState == StoringObjectState.Changed || this.BpmId == Guid.Empty)
			{
				success = entity.Save(true);
			}
			else
			{
				success = true;
			}
			this.BpmId = (Guid)entity.GetColumnValue("Id");
			
			//if (this.BPMId != Guid.Empty)
			//{
			//	if (this.StockBalance != null && this.StockBalance.Count > 0)
			//	{
			//		foreach (var stock in this.StockBalance)
			//		{
			//			stock.Product = this.BPMId.ToString();
			//			stock.ProcessRemoteItem();
			//		}	
			//	}
			//	
			//	if (this.PriceList != null && this.PriceList.Count > 0)
			//	{
			//		foreach (var price in this.PriceList)
			//		{
			//			price.Product = this.BPMId;
			//			price.ProcessRemoteItem();
			//		}	
			//	}
			//	
			//	if (this.Units != null && this.Units.Count > 0)
			//	{
			//		foreach (var unit in this.Units)
			//		{
			//			unit.Product = this.BPMId;
			//			unit.ProcessRemoteItem();
			//		}	
			//	}
			//	
			//	if (this.Specification != null && this.Specification.Count > 0)
			//	{
			//		foreach (var spec in this.Specification)
			//		{
			//			spec.Product = this.BPMId;
			//			spec.ProcessRemoteItem();
			//		}	
			//	}
			//	
			//	if (this.Barcodes != null && this.Barcodes.Count > 0)
			//	{
			//		foreach (var barcode in this.Barcodes)
			//		{
			//			barcode.Product = this.BPMId;
			//			barcode.ProcessRemoteItem();
			//		}	
			//	}
			//	
			//	if (this.AssociatedProducts != null && this.AssociatedProducts.Count > 0)
			//	{
			//		foreach (var associatedProduct in this.AssociatedProducts)
			//		{
			//			associatedProduct.ProcessRemoteItem();
			//			if (associatedProduct.BPMId != Guid.Empty)
			//			{
			//				associatedProduct.setAssociatedProduct(this.BPMId);
			//			}
			//		}	
			//	}
			//	
			//	if (this.Properties != null && this.Properties.Count > 0)
			//	{
			//		foreach (var properties in this.Properties)
			//		{
			//			properties.Product = this.BPMId;
			//			properties.ProcessRemoteItem();
			//		}	
			//	}
			//}
				
			return success;
		}
		
		public void SetAssociatedProduct(Guid productId)
		{
			var selEntity = new Select(UserConnection)
				.Column("Id").Top(1)
				.From("GenAssociateProduct")
				.Where("GenProductId").IsEqual(Column.Parameter(productId))
				.And("GenAssociatedProductId").IsEqual(Column.Parameter(this.BpmId))
			as Select;
			
			var ap = selEntity.ExecuteScalar<Guid>();

			if (ap != Guid.Empty) return;
			var entity = UserConnection.EntitySchemaManager
				.GetInstanceByName("GenAssociateProduct").CreateEntity(UserConnection);
			var now = DateTime.Now;
				
			entity.SetDefColumnValues();
				
			entity.SetColumnValue("GenProductId", productId);
			entity.SetColumnValue("GenAssociatedProductId", this.BpmId);
			entity.Save(true);
		}
	}
}
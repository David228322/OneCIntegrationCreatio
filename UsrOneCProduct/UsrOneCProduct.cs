//namespace Terrasoft.Configuration.UsrOneCProduct
//{
//	using System.Collections.Generic;
//	using System.ServiceModel;
//	using System.ServiceModel.Web;
//	using System.ServiceModel.Activation;
//	using System.Linq;
//	using System.Runtime.Serialization;
//	using Terrasoft.Web.Common;
//	using Terrasoft.Web.Http.Abstractions;
//
//	using System.Diagnostics;
//	using System;
//	using Terrasoft.Core;
//	using Terrasoft.Core.DB;
//	using Terrasoft.Core.Entities;
//	using Terrasoft.Core.Configuration;
//	using Terrasoft.Common;
//	using System.Globalization;
//
//	using Terrasoft.Configuration.UsrIntegrationLogHelper;
//	using Terrasoft.Configuration.UsrOneCSvcIntegration;
//	
//	[DataContract]
//	public class OneCProduct
//	{
//		[DataMember(Name = "Id")]
//		public string ID1C { get; set; }
//		[DataMember(Name = "BPMId")]
//		public string LocalId { get; set; }
//		[IgnoreDataMember]
//		public Guid BPMId { get; set; }
//		
//		[DataMember(Name = "LangCode")]
//		public string LangCode { get; set; }
//		
//		[DataMember(Name = "Name")] //lcz
//		public string Name { get; set; }	
//		[DataMember(Name = "Unit")]
//		public string Unit { get; set; }
//		[DataMember(Name = "Type")]
//		public string Type { get; set; }
//		[DataMember(Name = "Description")]
//		public string Description { get; set; }
//		[DataMember(Name = "ShortDescription")]
//		public string ShortDescription { get; set; }
//		[DataMember(Name = "Currency")]
//		public string Currency { get; set; }
//		[DataMember(Name = "Price")]
//		public decimal Price { get; set; }
//		
//		[DataMember(Name = "CreatedOn")]
//		public string CreatedOn { get; set; }
//		[DataMember(Name = "ModifiedOn")]
//		public string ModifiedOn { get; set; }
//		
//		
//		//[DataMember (Name = "StockBalance")]
//		//public List<OneCOneProductStockBalance> StockBalance { get; set; }
//		//[DataMember (Name = "PriceList")]
//		//public List<OneCOneProductPrice> PriceList { get; set; }
//		//[DataMember (Name = "Units")]
//		//public List<OneCOneProductUnits> Units { get; set; }
//		//[DataMember (Name = "Specification")]
//		//public List<OneCOneSpecificationInProduct> Specification { get; set; }
//		//[DataMember (Name = "Barcodes")]
//		//public List<OneCOneProductBarcodes> Barcodes { get; set; }
//		//[DataMember (Name = "AssociatedProducts")]
//		//public List<OneCProduct> AssociatedProducts { get; set; }
//		//[DataMember (Name = "Properties")]
//		//public List<OneCOneProductProperties> Properties { get; set; }
//		
//		[IgnoreDataMember]
//		private UserConnection _userConnection;
//		[IgnoreDataMember]
//		public UserConnection UserConnection {
//			get {
//				return _userConnection ??
//					(_userConnection = HttpContext.Current.Session["UserConnection"] as UserConnection);
//			}
//			set {
//				_userConnection = value;
//			}
//		}
//		
//		public string ProcessRemoteItem(bool IsFull = true)
//		{
//			if (((!string.IsNullOrEmpty(this.LocalId) && this.LocalId != "00000000-0000-0000-0000-000000000000") ||
//				!string.IsNullOrEmpty(this.VendorCode)) &&
//				!string.IsNullOrEmpty(this.LangCode))
//			{
//				if (this.BPMId == Guid.Empty)
//				{
//					this.ResolveRemoteItem();
//				}
//				if (this.BPMId == Guid.Empty || IsFull == true)
//				{
//					this.SaveRemoteItem();
//				}
//			}
//			return this.BPMId.ToString();
//		}
//		
//		public bool ResolveRemoteItem()
//		{
//			bool success = false;
//			
//			if (string.IsNullOrEmpty(this.LocalId) && string.IsNullOrEmpty(this.ID1C))
//				return success;
//			Select _selEntity = new Select(UserConnection)
//				.Column("Product", "Id").Top(1)
//				.From("Product").As("Product")
//			as Select;
//			if (!string.IsNullOrEmpty(this.LocalId))
//				_selEntity = _selEntity.Where("Product", "Id").IsEqual(Column.Parameter(new Guid(this.LocalId))) as Select;
//			else if (!string.IsNullOrEmpty(this.ID1C))
//				_selEntity = _selEntity.Where("Product", "Code").IsEqual(Column.Parameter(this.VendorCode)) as Select; // він один для усіх товарів в усіх 1Сках має бути 
//			else
//				return false;
//			
//			Guid _entityId = _selEntity.ExecuteScalar<Guid>();
//			if (_entityId != Guid.Empty)
//			{
//				this.BPMId = _entityId;
//				success = true;
//			}
//			return success;
//		}
//		
//		private bool SaveRemoteItem()
//		{
//			bool success = false;
//			Directory directory = new Directory();
//			Guid _Unit = Guid.Empty;
//			Guid _ResidueStorageUnit = Guid.Empty;
//			Guid _ReportingUnit = Guid.Empty;
//			Guid _UnitOfSeats = Guid.Empty;
//			Guid _Type = Guid.Empty;
//			Guid _NomenclatureGroup = Guid.Empty;
//			Guid _ResponsibleForPurchases = Guid.Empty;
//			Guid _MainSupplier = Guid.Empty;
//			
//			if (!string.IsNullOrEmpty(this.Unit))
//			{
//				_Unit = directory.GetId("Unit", this.Unit);
//			}
//			
//			if (!string.IsNullOrEmpty(this.ResidueStorageUnit))
//			{
//				_ResidueStorageUnit = directory.GetId("Unit", this.ResidueStorageUnit);
//			}
//			
//			if (!string.IsNullOrEmpty(this.ReportingUnit))
//			{
//				_ReportingUnit = directory.GetId("Unit", this.ReportingUnit);
//			}
//			
//			if (!string.IsNullOrEmpty(this.UnitOfSeats))
//			{
//				_UnitOfSeats = directory.GetId("Unit", this.UnitOfSeats);
//			}
//			
//			if (!string.IsNullOrEmpty(this.Type))
//			{
//				_Type = directory.GetId("ProductType", this.Type);
//			}
//			
//			if (!string.IsNullOrEmpty(this.NomenclatureGroup))
//			{
//				_NomenclatureGroup = directory.GetId("GenNomenclatureGroup", this.NomenclatureGroup);
//			}
//			
//			if (!string.IsNullOrEmpty(this.ResponsibleForPurchasesLocalId) && directory.СhekId("Contact", this.ResponsibleForPurchasesLocalId))
//			{
//				_ResponsibleForPurchases = new Guid(this.ResponsibleForPurchasesLocalId);
//			}
//			
//			if (!string.IsNullOrEmpty(this.MainSupplier))
//			{
//				//_MainSupplier = directory.GetId("Contact", this.MainSupplier);
//			}
//			
//			
//
//			var _entity = UserConnection.EntitySchemaManager
//				.GetInstanceByName("Product").CreateEntity(UserConnection);
//			var _now = DateTime.Now;
//			
//			if (this.BPMId == Guid.Empty)
//			{
//				_entity.SetDefColumnValues();
//			}
//			else if (!_entity.FetchFromDB(_entity.Schema.PrimaryColumn.Name, this.BPMId)) {
//				_entity.SetDefColumnValues();
//			}
//			
//			if (!string.IsNullOrEmpty(this.ID1C))
//			{
//				var localizableID1C = new LocalizableString();
//				localizableID1C.SetCultureValue(new CultureInfo(this.LangCode), this.ID1C);
//				_entity.SetColumnValue("GenID1C", localizableID1C);
//			}
//			
//			if (!string.IsNullOrEmpty(this.Name))
//			{
//				var localizableString = new LocalizableString();
//				localizableString.SetCultureValue(new CultureInfo(this.LangCode), this.Name);
//				_entity.SetColumnValue("Name", localizableString);
//			}
//			
//			if (!string.IsNullOrEmpty(this.NameForInvoices))
//			{
//				var localizableNameForInvoices = new LocalizableString();
//				localizableNameForInvoices.SetCultureValue(new CultureInfo(this.LangCode), this.NameForInvoices);
//				_entity.SetColumnValue("GenNameForInvoices", localizableNameForInvoices);
//			}
//			
//			if (!string.IsNullOrEmpty(this.FullName))
//			{
//				var localizableFullName = new LocalizableString();
//				localizableFullName.SetCultureValue(new CultureInfo(this.LangCode), this.FullName);
//				_entity.SetColumnValue("GenFullName", localizableFullName);
//			}
//			
//			if (!string.IsNullOrEmpty(this.VendorCode))
//			{
//				_entity.SetColumnValue("Code", this.VendorCode);
//			}
//			
//			if (_Unit != Guid.Empty)
//			{
//				_entity.SetColumnValue("UnitId", _Unit);
//			}
//			
//			if (_ResidueStorageUnit != Guid.Empty)
//			{
//				_entity.SetColumnValue("GenResidueStorageUnitId", _ResidueStorageUnit);
//			}
//			
//			if (_ReportingUnit != Guid.Empty)
//			{
//				_entity.SetColumnValue("GenReportingUnitId", _ReportingUnit);
//			}
//			
//			if (_UnitOfSeats != Guid.Empty)
//			{
//				_entity.SetColumnValue("GenUnitOfSeatsId", _UnitOfSeats);
//			}
//			
//			_entity.SetColumnValue("GenKeepTrackAdditionalCharacteristics", this.KeepTrackAdditionalCharacteristics);
//			
//			_entity.SetColumnValue("GenKeepRecordsBySeries", this.KeepRecordsBySeries);
//			
//			_entity.SetColumnValue("GenKeepPartyRecordsBySeries", this.KeepPartyRecordsBySeries);
//			
//			_entity.SetColumnValue("GenHeavyGoods", this.HeavyGoods);
//			
//			_entity.SetColumnValue("GenStrictAccountingForm", this.StrictAccountingForm);
//			
//			if (!string.IsNullOrEmpty(this.AtOKTZ))
//			{
//				_entity.SetColumnValue("GenAtOKTZ", this.AtOKTZ);
//			}
//			
//			if (_Type != Guid.Empty)
//			{
//				_entity.SetColumnValue("TypeId", _Type);
//			}
//			
//			if (_NomenclatureGroup != Guid.Empty)
//			{
//				_entity.SetColumnValue("GenNomenclatureGroupId", _NomenclatureGroup);
//			}
//			
//			if (_ResponsibleForPurchases != Guid.Empty)
//			{
//				_entity.SetColumnValue("GenResponsibleForPurchasesId", _ResponsibleForPurchases);
//			}
//			
//			//if (_MainSupplier != Guid.Empty)
//			//{
//			//	_entity.SetColumnValue("GenMainSupplierId", _MainSupplier);
//			//}
//			
//			if (!string.IsNullOrEmpty(this.ActionWith))
//			{
//				_entity.SetColumnValue("GenActionWith", DateTime.Parse(this.ActionWith));
//			}
//			
//			if (!string.IsNullOrEmpty(this.ActionOn))
//			{
//				_entity.SetColumnValue("GenActionOn", DateTime.Parse(this.ActionOn));
//			}
//			
//			if (!string.IsNullOrEmpty(this.SearchWords))
//			{
//				var localizableSearchWords = new LocalizableString();
//				localizableSearchWords.SetCultureValue(new CultureInfo(this.LangCode), this.SearchWords);
//				_entity.SetColumnValue("GenSearchWords", localizableSearchWords);
//			}
//			
//			if (!string.IsNullOrEmpty(this.LinkToTheSitePhoto))
//			{
//				var localizableLinkToTheSitePhoto = new LocalizableString();
//				localizableLinkToTheSitePhoto.SetCultureValue(new CultureInfo(this.LangCode), this.LinkToTheSitePhoto);
//				_entity.SetColumnValue("GenLinkToTheSitePhoto", localizableLinkToTheSitePhoto);
//			}
//			
//			if (!string.IsNullOrEmpty(this.Size))
//			{
//				_entity.SetColumnValue("GenSize", this.Size);
//			}
//			
//			if (this.Width > 0)
//			{
//				_entity.SetColumnValue("GenWidth", this.Width);
//			}
//			
//			if (this.Length > 0)
//			{
//				_entity.SetColumnValue("GenLength", this.Length);
//			}
//			
//			if (this.Thickness > 0)
//			{
//				_entity.SetColumnValue("GenThickness", this.Thickness);
//			}
//			
//			
//			_entity.SetColumnValue("ModifiedOn", _now);
//		
//			if (_entity.StoringState == StoringObjectState.Changed || this.BPMId == Guid.Empty)
//			{
//				success = _entity.Save(true);
//			}
//			else
//			{
//				success = true;
//			}
//			this.BPMId = (Guid)_entity.GetColumnValue("Id");
//			
//			//if (this.BPMId != Guid.Empty)
//			//{
//			//	if (this.StockBalance != null && this.StockBalance.Count > 0)
//			//	{
//			//		foreach (var stock in this.StockBalance)
//			//		{
//			//			stock.Product = this.BPMId.ToString();
//			//			stock.ProcessRemoteItem();
//			//		}	
//			//	}
//			//	
//			//	if (this.PriceList != null && this.PriceList.Count > 0)
//			//	{
//			//		foreach (var price in this.PriceList)
//			//		{
//			//			price.Product = this.BPMId;
//			//			price.ProcessRemoteItem();
//			//		}	
//			//	}
//			//	
//			//	if (this.Units != null && this.Units.Count > 0)
//			//	{
//			//		foreach (var unit in this.Units)
//			//		{
//			//			unit.Product = this.BPMId;
//			//			unit.ProcessRemoteItem();
//			//		}	
//			//	}
//			//	
//			//	if (this.Specification != null && this.Specification.Count > 0)
//			//	{
//			//		foreach (var spec in this.Specification)
//			//		{
//			//			spec.Product = this.BPMId;
//			//			spec.ProcessRemoteItem();
//			//		}	
//			//	}
//			//	
//			//	if (this.Barcodes != null && this.Barcodes.Count > 0)
//			//	{
//			//		foreach (var barcode in this.Barcodes)
//			//		{
//			//			barcode.Product = this.BPMId;
//			//			barcode.ProcessRemoteItem();
//			//		}	
//			//	}
//			//	
//			//	if (this.AssociatedProducts != null && this.AssociatedProducts.Count > 0)
//			//	{
//			//		foreach (var associatedProduct in this.AssociatedProducts)
//			//		{
//			//			associatedProduct.ProcessRemoteItem();
//			//			if (associatedProduct.BPMId != Guid.Empty)
//			//			{
//			//				associatedProduct.setAssociatedProduct(this.BPMId);
//			//			}
//			//		}	
//			//	}
//			//	
//			//	if (this.Properties != null && this.Properties.Count > 0)
//			//	{
//			//		foreach (var properties in this.Properties)
//			//		{
//			//			properties.Product = this.BPMId;
//			//			properties.ProcessRemoteItem();
//			//		}	
//			//	}
//			//}
//				
//			return success;
//		}
//		
//		public void setAssociatedProduct(Guid _ProductId)
//		{
//			Guid _ap = Guid.Empty;
//			Select _selEntity = new Select(UserConnection)
//				.Column("Id").Top(1)
//				.From("GenAssociateProduct")
//				.Where("GenProductId").IsEqual(Column.Parameter(_ProductId))
//				.And("GenAssociatedProductId").IsEqual(Column.Parameter(this.BPMId))
//			as Select;
//			
//			_ap = _selEntity.ExecuteScalar<Guid>();
//			
//			if (_ap == Guid.Empty)
//			{
//				var _entity = UserConnection.EntitySchemaManager
//				.GetInstanceByName("GenAssociateProduct").CreateEntity(UserConnection);
//				var _now = DateTime.Now;
//				
//				_entity.SetDefColumnValues();
//				
//				_entity.SetColumnValue("GenProductId", _ProductId);
//				_entity.SetColumnValue("GenAssociatedProductId", this.BPMId);
//				_entity.Save(true);
//			}
//		}
//	}
//}
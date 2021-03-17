using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xbim.CobieExpress;
using Xbim.Ifc;
using Xbim.IO.Table;
using Xbim.IO.CobieExpress;
using XbimExchanger.IfcToCOBieExpress;
using XbimExchanger.IfcHelpers;

namespace libal.Services
{
    public class CobieConverterService
    {

        IS3Service _s3 = new S3Service();

        public async Task<String> ConvertAsync(string source, string destination)
        {
            var content = await _s3.Get(source);
            using (var ifcStream = new MemoryStream(content))
            {
                var ifcStoreGenerator = new IfcStoreGenerator(ifcStream);
                using (var ifcStore = await ifcStoreGenerator.GetIfcStoreAsync())
                {

                    if (ifcStore.SchemaVersion == Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3)
                    {
                        Ifc2x3IfcEntityLabelGenerator.enrichModelWithIfcEntityLabel(ifcStore);
                    }
                    else
                    {
                        Ifc4x1IfcEntityLabelGenerator.enrichModelWithIfcEntityLabel(ifcStore);
                    }

                    return await ConvertIfcToCobieAsync(ifcStore, destination);
                }
            }
        }

        private Task<String> ConvertIfcToCobieAsync(IfcStore ifcStore, string fileName)
        {
            return Task.Run<String>(() =>
            {

                var cobies = GetCobieModels(ifcStore);
                string report = string.Empty;
                var stream = new MemoryStream();

                var cobie = cobies[0];

                using (var txn = cobie.BeginTransaction("COBie Changes"))
                {

                    // add default contact
                    var adminContact = cobie.Instances.FirstOrDefault<CobieContact>(c => c.Email == "admin@libal-tech.de") ?? cobie.Instances.New<CobieContact>(c =>
                    {
                        c.StateRegion = "Baden-Württemberg";
                        c.Street = "Tettnangerstraße 5";
                        c.Town = "Meckenbeuren";
                        c.PostalCode = "88074";
                        c.Country = "Germany";
                        c.Company = "LIBAL Deutschland GmbH";
                        c.Department = "IT";
                        c.Email = "admin@libal-tech.de";
                        c.GivenName = "admin";
                        c.FamilyName = "admin";
                        c.Created = cobie.Instances.New<CobieCreatedInfo>(created => {
                            created.CreatedBy = cobie.Instances.FirstOrDefault<CobieContact>(contact => contact.Email == "admin@libal-tech.de") ?? cobie.Instances.New<CobieContact>(contact => { contact.Email = "admin@libal-tech.de"; });
                            created.CreatedOn = new DateTimeValue(DateTime.Now.ToString());
                        });
                    });


                    var contacts = cobie.Instances.OfType<CobieContact>();
                    foreach (var c in contacts)
                    {
                        if (c.ExternalObject == null) {
                            c.ExternalObject = cobie.Instances.New<CobieExternalObject>(extObject => extObject.Name = "IfcPersonAndOrganization");
                        }
                    }
                        
                    // set default contact to every created by
                    var objects = cobie.Instances.OfType<CobieReferencedObject>();
                    foreach (var o in objects)
                    {
                        o.Created = cobie.Instances.New<CobieCreatedInfo>(created => {
                            created.CreatedBy = cobie.Instances.FirstOrDefault<CobieContact>(contact => contact.Email == "admin@libal-tech.de") ?? cobie.Instances.New<CobieContact>(contact => { contact.Email = "admin@libal-tech.de"; });
                            created.CreatedOn = new DateTimeValue(DateTime.Now.ToString());
                        });
                    }

                    var areaUnit = cobie.Instances.FirstOrDefault<CobieAreaUnit>();
                    var currencyUnit = cobie.Instances.FirstOrDefault<CobieCurrencyUnit>();
                    var durationUnit = cobie.Instances.FirstOrDefault<CobieDurationUnit>();
                    var impactUnit = cobie.Instances.FirstOrDefault<CobieImpactUnit>();
                    var linearUnit = cobie.Instances.FirstOrDefault<CobieLinearUnit>();
                    var volumeUnit = cobie.Instances.FirstOrDefault<CobieVolumeUnit>();

                    var facility = cobie.Instances.FirstOrDefault<CobieFacility>();
                    if (facility.Name == null) { facility.Name = "Default";}
                    if (facility.AreaUnits == null) { facility.AreaUnits = areaUnit; }
                    if (facility.CurrencyUnit == null) { facility.CurrencyUnit = currencyUnit; }
                    if (facility.LinearUnits == null) { facility.LinearUnits = linearUnit; }
                    if (facility.VolumeUnits == null) { facility.VolumeUnits = volumeUnit; }

                    facility.Attributes.Add(cobie.Instances.New<CobieAttribute>(a =>
                    {
                        a.Name = "IfcVersion";
                        a.Value = new StringValue("" + ifcStore.SchemaVersion);
                        a.Created = cobie.Instances.New<CobieCreatedInfo>(created => {
                            created.CreatedBy = cobie.Instances.FirstOrDefault<CobieContact>(c => c.Email == "admin@libal-tech.de") ?? cobie.Instances.New<CobieContact>(c => { c.Email = "admin@libal-tech.de"; });
                            created.CreatedOn = new DateTimeValue(DateTime.Now.ToString());
                        });
                        a.ExternalObject = cobie.Instances.New<CobieExternalObject>(extObject => extObject.Name = "Pset_LibalCommon");
                    }));

                    // make unique names
                    MakeUniqueNames<CobieFacility>(cobie);
                    MakeUniqueNames<CobieFloor>(cobie);
                    MakeUniqueNames<CobieSpace>(cobie);
                    MakeUniqueNames<CobieType>(cobie);
                    MakeUniqueNames<CobieComponent>(cobie);
                    MakeUniqueNames<CobieSystem>(cobie);
                    MakeUniqueNames<CobieZone>(cobie);

                    UpdateCategoryValue<CobieFacility>(cobie);
                    UpdateCategoryValue<CobieFloor>(cobie);
                    UpdateCategoryValue<CobieComponent>(cobie);
                    UpdateCategoryValue<CobieSpace>(cobie);
                    UpdateCategoryValue<CobieSystem>(cobie);
                    UpdateCategoryValue<CobieZone>(cobie);
                    UpdateCategoryValue<CobieType>(cobie);

                    // Add missing external object to attributes
                    var cobieAttributes = cobie.Instances.OfType<CobieAttribute>();
                    foreach (var attribute in cobieAttributes) {
                        if (attribute.ExternalObject == null) {
                            attribute.ExternalObject = cobie.Instances.New<CobieExternalObject>(extObject => extObject.Name = "Pset_LibalUndefined");
                        }

                        if (attribute.Created == null) {
                            attribute.Created = cobie.Instances.New<CobieCreatedInfo>(created => {
                                created.CreatedBy = cobie.Instances.FirstOrDefault<CobieContact>(c => c.Email == "admin@libal-tech.de") ?? cobie.Instances.New<CobieContact>(c => { c.Email = "admin@libal-tech.de"; });
                                created.CreatedOn = new DateTimeValue(DateTime.Now.ToString());
                            });
                        }
                    }

                    ResolveContacts(cobie);

                    txn.Commit();
                }

                cobie.ExportToTable(stream, Xbim.IO.Table.ExcelTypeEnum.XLSX, out report, CobieConverterService.GetMapping(), null);

                _s3.Create(fileName, new MemoryStream(stream.ToArray()));

                return "Processed";
            });
        }

        private static void MakeUniqueNames<T>(CobieModel model) where T : CobieAsset
        {
            var groups = model.Instances.OfType<T>().GroupBy(a => a.Name);
            foreach (var @group in groups)
            {
                if (group.Count() == 1)
                {
                    var item = group.First();
                    if (string.IsNullOrEmpty(item.Name))
                        item.Name = item.ExternalObject.Name;
                    continue;
                }

                var counter = 1;
                foreach (var item in group)
                {
                    if (string.IsNullOrEmpty(item.Name))
                        item.Name = item.ExternalObject.Name;
                    item.Name = string.Format("{0} ({1})", item.Name, counter++);
                }
            }
        }

        private static void UpdateCategoryValue<T>(CobieModel model) where T : CobieAsset
        {
            var assets = model.Instances.OfType<CobieAsset>();
            foreach (var asset in assets)
            {
                var category = asset.Categories.FirstOrDefault<CobieCategory>();
                var newCategoryValue = !String.IsNullOrEmpty(category.Value) && !String.IsNullOrEmpty(category.Description) ? category.Value + ": " + category.Description : !String.IsNullOrEmpty(category.Description) ? category.Description : category.Value;
                var cobieCategory = model.Instances.New<CobieCategory>(c => { c.Value = newCategoryValue; });
                asset.Categories.Clear();
                asset.Categories.Add(cobieCategory);
            }
        }

        private static List<CobieModel> GetCobieModels(IfcStore model)
        {
            List<CobieModel> cobieModels = new List<CobieModel>();

            if (model.IsFederation == false)
            {
                var cobie = new CobieModel();
                using (var txn = cobie.BeginTransaction("begin conversion"))
                {
                    var exchanger = new IfcToCoBieExpressExchanger(model, cobie, null, null, null, EntityIdentifierMode.GloballyUniqueIds, SystemExtractionMode.System);
                    exchanger.Convert();
                    cobieModels.Add(cobie);
                    txn.Commit();
                }

            }
            else
            {
                throw new NotImplementedException("Work to do on COBie Federated");
            }
            return cobieModels;
        }

        private static void ResolveContacts(CobieModel cobie)
        {
            var contacts = cobie.Instances.OfType<CobieContact>();
            foreach (var o in contacts)
            {
                var resolvedEmail = EmailFallbackGenerator.email(o.Email, o.Company, (o.GivenName != null ? o.GivenName : "") + (o.FamilyName != null ? o.FamilyName : ""));
                o.Email = resolvedEmail;
            }
        }

        private static ModelMapping GetMapping()
        {
            return ModelMapping.Load(GetCobieConfigurationXml());
        }

        private static string GetCobieConfigurationXml()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <ModelMapping xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://www.openbim.org/mapping/table/1.0""
                                  Name=""COBie2.4 UK2012""
                                  ListSeparator="",""
                                  PickTableName=""PickLists""
                                  >
                      <StatusRepresentations>
                        <StatusRepresentation Colour=""#CCC"" FontWeight=""Bold"" Border=""true"" Status=""Header"" />
                        <StatusRepresentation Colour=""#FFFF99"" FontWeight=""Normal"" Border=""true"" Status=""Required"" />
                        <StatusRepresentation Colour=""#FFCC99"" FontWeight=""Normal"" Border=""true"" Status=""Reference"" />
                        <StatusRepresentation Colour=""#FFCC99"" FontWeight=""Normal"" Border=""true"" Status=""PickValue"" />
                        <StatusRepresentation Colour=""#CC99FF"" FontWeight=""Normal"" Border=""true"" Status=""ExternalReference""/>
                        <StatusRepresentation Colour=""#CCFFCC"" FontWeight=""Normal"" Border=""true"" Status=""Optional"" />
                      </StatusRepresentations>
                      <ClassMappings>
                        <ClassMapping Class=""Contact"" TableName=""Contact"" TableOrder=""0"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Email"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Email"" IsKey=""true"" DataType =""Email"" LookUp="""" />
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""Category"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""Category.Value"" DataType =""Text"" LookUp=""PickLists.Category-Role""/>
                            <PropertyMapping Header=""Company"" Column=""E"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Company"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Phone"" Column=""F"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Phone"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""G"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""H"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objContact""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""I"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Department"" Column=""J"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Department"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""OrganizationCode"" Column=""K"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""OrganizationCode"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""GivenName"" Column=""L"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""GivenName"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""FamilyName"" Column=""M"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""FamilyName"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Street"" Column=""N"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Street"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""PostalBox"" Column=""O"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""PostalBox"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Town"" Column=""P"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Town"" DataType =""Text"" />
                            <PropertyMapping Header=""StateRegion"" Column=""Q"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""StateRegion"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""PostalCode"" Column=""R"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""PostalCode"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Country"" Column=""S"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Country"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Facility"" TableName=""Facility"" TableOrder=""1"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name""  IsKey=""true""  DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""Category"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""Categories.Value"" DataType =""Text"" LookUp=""PickLists.Category-Facility""/>
                            <PropertyMapping Header=""ProjectName"" Column=""E"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Project.Name"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""SiteName"" Column=""F"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Site.Name"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""LinearUnits"" Column=""G"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""LinearUnits.Value"" DataType =""Text"" LookUp=""PickLists.LinearUnit""/>
                            <PropertyMapping Header=""AreaUnits"" Column=""H"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""AreaUnits.Value"" DataType =""Text"" LookUp=""PickLists.AreaUnit""/>
                            <PropertyMapping Header=""VolumeUnits"" Column=""I"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""VolumeUnits.Value"" DataType =""Text"" LookUp=""PickLists.VolumeUnit""/>
                            <PropertyMapping Header=""CurrencyUnit"" Column=""J"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""CurrencyUnit.Value"" DataType =""Text"" LookUp=""PickLists.CurrencyUnit""/>
                            <PropertyMapping Header=""AreaMeasurement"" Column=""K"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""AreaMeasurement"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""ExternalSystem"" Column=""L"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExternalProjectObject"" Column=""M"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Project.ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objProject""/>
                            <PropertyMapping Header=""ExternalProjectIdentifier"" Column=""N"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Project.ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExternalSiteObject"" Column=""O"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Site.ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objSite""/>
                            <PropertyMapping Header=""ExternalSiteIdentifier"" Column=""P"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Site.ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExternalFacilityObject"" Column=""Q"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objFacility""/>
                            <PropertyMapping Header=""ExternalFacilityIdentifier"" Column=""R"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Description"" Column=""S"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ProjectDescription"" Column=""T"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Project.Description"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""SiteDescription"" Column=""U"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Site.Description"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Phase"" Column=""V"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Phase.Name"" DataType =""Text"" LookUp=""PickLists.Category-Phase""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Floor"" TableName=""Floor"" TableOrder=""2"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name"" IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""Category"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""Categories.Value"" DataType =""Text"" LookUp=""PickLists.Category-Floor""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""E"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""F"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objFloor""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""G"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Description"" Column=""H"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Elevation"" Column=""I"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Elevation"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""Height"" Column=""J"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Height"" DataType =""Numeric"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Space"" TableName=""Space"" TableOrder=""3"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name"" IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""Category"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""Categories.Value"" DataType =""Text"" LookUp=""PickLists.Category-Space""/>
                            <PropertyMapping Header=""FloorName"" Column=""E"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Floor.Name"" DataType =""AlphaNumeric"" LookUp=""Floor.Name""/>
                            <PropertyMapping Header=""Description"" Column=""F"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""G"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""H"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objSpace""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""I"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""RoomTag"" Column=""J"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""RoomTag"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""UsableHeight"" Column=""K"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""UsableHeight"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""GrossArea"" Column=""L"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""GrossArea"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""NetArea"" Column=""M"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""NetArea"" DataType =""Numeric"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Zone"" TableName=""Zone"" TableOrder=""4"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name"" IsKey=""true"" IsMultiRowIdentity=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email""  LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""Category"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""Categories.Value"" IsKey=""true""  DataType =""Text"" LookUp=""PickLists.Category-Zone""/>
                            <PropertyMapping Header=""SpaceNames"" Column=""E"" Status=""Reference"" MultiRow=""Always"" DefaultValue=""n/a"" Paths=""Spaces.Name""  IsKey=""true"" DataType =""Text"" LookUp=""Space.Name""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""F"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""G"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objZone""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""H"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Description"" Column=""I"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Type"" TableName=""Type"" TableOrder=""5"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""Category"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""Categories.Value"" DataType =""Text"" LookUp=""PickLists.Category-Type""/>
                            <PropertyMapping Header=""Description"" Column=""E"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""AssetType"" Column=""F"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""AssetType.Value"" DataType =""Text"" LookUp=""PickLists.AssetType""/>
                            <PropertyMapping Header=""Manufacturer"" Column=""G"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Manufacturer.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""ModelNumber"" Column=""H"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ModelNumber"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""WarrantyGuarantorParts"" Column=""I"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""WarrantyGuarantorParts.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""WarrantyDurationParts"" Column=""J"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""WarrantyDurationParts"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""WarrantyGuarantorLabor"" Column=""K"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""WarrantyGuarantorLabor.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""WarrantyDurationLabor"" Column=""L"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""WarrantyDurationLabor"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""WarrantyDurationUnit"" Column=""M"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""WarrantyDurationUnit.Value"" DataType =""Text"" LookUp=""PickLists.DurationUnit""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""N"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""O"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objType""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""P"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ReplacementCost"" Column=""Q"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ReplacementCost"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""ExpectedLife"" Column=""R"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExpectedLife"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""DurationUnit"" Column=""S"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""DurationUnit.Value"" DataType =""Text"" LookUp=""PickLists.DurationUnit""/>
                            <PropertyMapping Header=""WarrantyDescription"" Column=""T"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""WarrantyDescription"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""NominalLength"" Column=""U"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""NominalLength"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""NominalWidth"" Column=""V"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""NominalWidth"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""NominalHeight"" Column=""W"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""NominalHeight"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""ModelReference"" Column=""X"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ModelReference"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Shape"" Column=""Y"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Shape"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Size"" Column=""Z"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Size"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Color"" Column=""AA"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Color"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Finish"" Column=""AB"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Finish"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Grade"" Column=""AC"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Grade"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Material"" Column=""AD"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Material"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Constituents"" Column=""AE"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Constituents"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Features"" Column=""AF"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Features"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""AccessibilityPerformance"" Column=""AG"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""AccessibilityPerformance"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""CodePerformance"" Column=""AH"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""CodePerformance"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""SustainabilityPerformance"" Column=""AI"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""SustainabilityPerformance"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Component"" TableName=""Component"" TableOrder=""6"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""TypeName"" Column=""D"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Type.Name"" DataType =""AlphaNumeric"" LookUp=""Type.Name""/>
                            <PropertyMapping Header=""Space"" Column=""E"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Spaces.Name"" DataType =""Text"" LookUp=""Space.Name""/>
                            <PropertyMapping Header=""Description"" Column=""F"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""G"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""H"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objComponent""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""I"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""SerialNumber"" Column=""J"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""SerialNumber"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""InstallationDate"" Column=""K"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""InstallationDate"" DataType =""ISODate"" LookUp=""""/>
                            <PropertyMapping Header=""WarrantyStartDate"" Column=""L"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""WarrantyStartDate"" DataType =""ISODate"" LookUp=""""/>
                            <PropertyMapping Header=""TagNumber"" Column=""M"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""TagNumber"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""BarCode"" Column=""N"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""BarCode"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""AssetIdentifier"" Column=""O"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""AssetIdentifier"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""System"" TableName=""System"" TableOrder=""7"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name""  IsKey=""true"" IsMultiRowIdentity=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""Category"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Categories.Value"" IsKey=""true""  DataType =""Text"" LookUp=""PickLists.Category-System""/>
                            <PropertyMapping Header=""ComponentNames"" Column=""E"" Status=""Reference"" MultiRow=""Always"" DefaultValue=""n/a"" Paths=""Components.Name""  IsKey=""true"" DataType =""Text"" LookUp=""Component.Name""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""F"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""G"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objSystem""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""H"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Description"" Column=""I"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""TypeOrComponent"" TableName=""Assembly"" TableOrder=""8"" ParentClass=""TypeOrComponent"" ParentPath=""AssemblyOf"" IsPartial=""true"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""parent.Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""SheetName"" Column=""D"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.[table]""  IsKey=""true"" DataType =""Text"" LookUp=""PickLists.SheetType""/>
                            <PropertyMapping Header=""ParentName"" Column=""E"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""[SheetName].Name""/>
                            <PropertyMapping Header=""ChildNames"" Column=""F"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name"" IsKey=""true"" DataType =""Text"" LookUp=""[SheetName].Name""/>
                            <PropertyMapping Header=""AssemblyType"" Column=""G"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""Fixed"" Paths="""" DataType =""Text"" LookUp=""PickLists.AssemblyType""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""H"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""I"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objAssembly""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""J"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Description"" Column=""K"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.Description"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Connection"" TableName=""Connection"" TableOrder=""9"" ParentClass=""TypeOrComponent"" ParentPath=""ConnectedBefore"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""ConnectionType"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""ConnectionType.Value"" IsKey=""true"" DataType =""Text"" LookUp=""PickLists.ConnectionType""/>
                            <PropertyMapping Header=""SheetName"" Column=""E"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.[table]"" DataType =""Text"" LookUp=""PickLists.SheetType""/>
                            <PropertyMapping Header=""RowName1"" Column=""F"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ComponentA.Name""  IsKey=""true"" DataType =""Text"" LookUp=""[SheetName].Name""/>
                            <PropertyMapping Header=""RowName2"" Column=""G"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ComponentB.Name""  IsKey=""true"" DataType =""Text"" LookUp=""[SheetName].Name""/>
                            <PropertyMapping Header=""RealizingElement"" Column=""H"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""RealizingComponent.Name"" DataType =""Text"" LookUp=""[SheetName].Name""/>
                            <PropertyMapping Header=""PortName1"" Column=""I"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""PortNameA"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""PortName2"" Column=""J"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""PortNameB"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""K"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""L"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objConnection""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""M"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Description"" Column=""N"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Spare"" TableName=""Spare"" TableOrder=""10"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""Category"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""SpareType.Value"" DataType =""Text"" LookUp=""PickLists.SpareType""/>
                            <PropertyMapping Header=""TypeName"" Column=""E"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Type.Name"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Suppliers"" Column=""F"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Suppliers.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""G"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""H"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objSpare""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""I"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Description"" Column=""J"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""SetNumber"" Column=""K"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""SetNumber"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""PartNumber"" Column=""L"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""PartNumber"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Resource"" TableName=""Resource"" TableOrder=""11"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""Category"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""ResourceType.Value"" DataType =""Text"" LookUp=""PickLists.ResourceType""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""E"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""F"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objResource""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""G"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Description"" Column=""H"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Job"" TableName=""Job"" TableOrder=""12"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""Category"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""JobType.Value"" DataType =""Text"" LookUp=""PickLists.JobType""/>
                            <PropertyMapping Header=""Status"" Column=""E"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""JobStatusType.Value"" DataType =""Text"" LookUp=""PickLists.JobStatusType""/>
                            <PropertyMapping Header=""TypeName"" Column=""F"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Type.Name"" IsKey=""true"" DataType =""Text"" LookUp=""Type.Name""/>
                            <PropertyMapping Header=""Description"" Column=""G"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Duration"" Column=""H"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Duration"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""DurationUnit"" Column=""I"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""DurationUnit.Value"" DataType =""Text"" LookUp=""PickLists.DurationUnit""/>
                            <PropertyMapping Header=""Start"" Column=""J"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Start"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""TaskStartUnit"" Column=""K"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""StartUnit.Value"" DataType =""Text"" LookUp=""PickLists.DurationUnit""/>
                            <PropertyMapping Header=""Frequency"" Column=""L"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Frequency"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""FrequencyUnit"" Column=""M"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""FrequencyUnit.Value"" DataType =""Text"" LookUp=""PickLists.DurationUnit""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""N"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""O"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objJob""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""P"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""TaskNumber"" Column=""Q"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""TaskNumber""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""Priors"" Column=""R"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Priors.TaskNumber"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ResourceNames"" Column=""S"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Resources.Name"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Impact"" TableName=""Impact"" TableOrder=""13"" ParentClass=""Asset"" ParentPath=""Impacts"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""ImpactType"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""ImpactType.Value"" IsKey=""true"" DataType =""Text"" LookUp=""PickLists.ImpactType""/>
                            <PropertyMapping Header=""ImpactStage"" Column=""E"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""ImpactStage.Value"" IsKey=""true"" DataType =""Text"" LookUp=""PickLists.ImpactStage""/>
                            <PropertyMapping Header=""SheetName"" Column=""F"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.[table]""  IsKey=""true"" DataType =""Text"" LookUp=""PickLists.SheetType""/>
                            <PropertyMapping Header=""RowName"" Column=""G"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""[SheetName].Name""/>
                            <PropertyMapping Header=""Value"" Column=""H"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Value"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""ImpactUnit"" Column=""I"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""ImpactUnit.Value"" DataType =""Text"" LookUp=""PickLists.ImpactUnit""/>
                            <PropertyMapping Header=""LeadInTime"" Column=""J"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""LeadInTime"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""Duration"" Column=""K"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Duration"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""LeadOutTime"" Column=""L"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""LeadOutTime"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""M"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""N"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objImpact""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""O"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Description"" Column=""P"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Document"" TableName=""Document"" TableOrder=""14"" ParentClass=""Asset"" ParentPath=""Documents"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name"" IsKey=""true""  DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email""  DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""Category"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""DocumentType.Value""  DataType =""Text"" LookUp=""PickLists.DocumentType""/>
                            <PropertyMapping Header=""ApprovalBy"" Column=""E"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""ApprovalType.Value"" DataType =""Text"" LookUp=""PickLists.ApprovalType""/>
                            <PropertyMapping Header=""Stage"" Column=""F"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""Stage.Value"" IsKey=""true""  DataType =""Text"" LookUp=""PickLists.StageType""/>
                            <PropertyMapping Header=""SheetName"" Column=""G"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.[table]"" IsKey=""true""  DataType =""Text"" LookUp=""PickLists.SheetType""/>
                            <PropertyMapping Header=""RowName"" Column=""H"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.Name"" IsKey=""true""  DataType =""AlphaNumeric"" LookUp=""[SheetName].Name""/>
                            <PropertyMapping Header=""Directory"" Column=""I"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Directory"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""File"" Column=""J"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""File"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""K"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""L"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name""  Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objDocument""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""M"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId""  Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Description"" Column=""N"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Reference"" Column=""O"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Reference"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Attribute"" TableName=""Attribute"" TableOrder=""15"" ParentClass=""Asset"" ParentPath=""Attributes"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
		                    <PropertyMapping Header=""Category"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""Stage.Value"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""SheetName"" Column=""E"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.[table]""  IsKey=""true"" DataType =""Text"" LookUp=""PickLists.SheetType""/>
                            <PropertyMapping Header=""RowName"" Column=""F"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""[SheetName].Name""/>
                            <PropertyMapping Header=""Value"" Column=""G"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Value"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Unit"" Column=""H"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Unit"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""I"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name"" Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""J"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name"" DataType =""Text"" LookUp=""PickLists.objAttribute""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""K"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId"" Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Description"" Column=""L"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""AllowedValues"" Column=""M"" Status=""Optional"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""AllowedValues"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Coordinate"" TableName=""Coordinate"" TableOrder=""16""  ParentClass=""Asset"" ParentPath=""Representations"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""Category"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""CoordinateType"" IsKey=""true""  DataType =""Text"" LookUp=""PickLists.StageType""/>
                            <PropertyMapping Header=""SheetName"" Column=""E"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.[table]""  IsKey=""true"" DataType =""Text"" LookUp=""PickLists.SheetType""/>
                            <PropertyMapping Header=""RowName"" Column=""F"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""parent.Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""[SheetName].Name""/>
                            <PropertyMapping Header=""CoordinateXAxis"" Column=""G"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""X"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""CoordinateYAxis"" Column=""H"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Y"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""CoordinateZAxis"" Column=""I"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Z"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""J"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name"" Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""K"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name"" Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objCoordinate""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""L"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId"" Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ClockwiseRotation"" Column=""M"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""RotationZ"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""ElevationalRotation"" Column=""N"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""RotationX"" DataType =""Numeric"" LookUp=""""/>
                            <PropertyMapping Header=""YawRotation"" Column=""O"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""RotationY"" DataType =""Numeric"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                        <ClassMapping Class=""Issue"" TableName=""Issue"" TableOrder=""17"">
                          <PropertyMappings>
                            <PropertyMapping Header=""Name"" Column=""A"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""""/>
                            <PropertyMapping Header=""CreatedBy"" Column=""B"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Created.CreatedBy.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""CreatedOn"" Column=""C"" Status=""Required"" MultiRow=""None"" DefaultValue=""1900-12-31T23:59:59"" Paths=""Created.CreatedOn"" DataType =""ISODateTime"" LookUp=""""/>
                            <PropertyMapping Header=""Type"" Column=""D"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""IssueType.Value"" DataType =""Text"" LookUp=""PickLists.IssueType""/>
                            <PropertyMapping Header=""Risk"" Column=""E"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""Risk.Value"" DataType =""Text"" LookUp=""PickLists.IssueRisk""/>
                            <PropertyMapping Header=""Chance"" Column=""F"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""Chance.Value"" DataType =""Text"" LookUp=""PickLists.IssueChance""/>
                            <PropertyMapping Header=""Impact"" Column=""G"" Status=""PickValue"" MultiRow=""None"" DefaultValue=""unknown"" Paths=""Impact.Value"" DataType =""Text"" LookUp=""PickLists.IssueImpact""/>
                            <PropertyMapping Header=""SheetName1"" Column=""H"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Causing.[table]""  IsKey=""true"" DataType =""Text"" LookUp=""PickLists.SheetType""/>
                            <PropertyMapping Header=""RowName1"" Column=""I"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Causing.Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""[SheetName1].Name""/>
                            <PropertyMapping Header=""SheetName2"" Column=""J"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Affected.[table]""  IsKey=""true"" DataType =""Text"" LookUp=""PickLists.SheetType""/>
                            <PropertyMapping Header=""RowName2"" Column=""K"" Status=""Reference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Affected.Name""  IsKey=""true"" DataType =""AlphaNumeric"" LookUp=""[SheetName2].Name""/>
                            <PropertyMapping Header=""Description"" Column=""L"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Description"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""Owner"" Column=""M"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Owner.Email"" DataType =""Email"" LookUp=""Contact.Email""/>
                            <PropertyMapping Header=""Mitigation"" Column=""N"" Status=""Required"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""Mitigation"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtSystem"" Column=""O"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalSystem.Name"" Hidden=""true"" DataType =""Text"" LookUp=""""/>
                            <PropertyMapping Header=""ExtObject"" Column=""P"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalObject.Name"" Hidden=""true"" DataType =""Text"" LookUp=""PickLists.objIssue""/>
                            <PropertyMapping Header=""ExtIdentifier"" Column=""Q"" Status=""ExternalReference"" MultiRow=""None"" DefaultValue=""n/a"" Paths=""ExternalId"" Hidden=""true"" DataType =""Text"" LookUp=""""/>
                          </PropertyMappings>
                        </ClassMapping>
                      </ClassMappings>
                      <PickClassMappings>
                        <PickClassMapping Header=""Categories"" />
                      </PickClassMappings>
                      <EnumerationMappings>
                        <EnumerationMapping Enumeration=""CoordinateTypeEnum"">
                          <Aliases>
                            <Alias EnumMember=""point"" Alias=""point""/>
                            <Alias EnumMember=""line_end_one"" Alias=""line-end-one""/>
                            <Alias EnumMember=""line_end_two"" Alias=""line-end-two""/>
                            <Alias EnumMember=""box_lowerleft"" Alias=""box-lowerleft""/>
                            <Alias EnumMember=""box_upperright"" Alias=""box-upperright""/>
                          </Aliases>
                        </EnumerationMapping>
                      </EnumerationMappings>
                      <Scopes>
                        <Scope Class=""PickValue"" Scope=""Model""/>
                        <Scope Class=""ExternalObject"" Scope=""Model""/>
                        <Scope Class=""ExternalSystem"" Scope=""Model""/>
                        <Scope Class=""CreatedInfo"" Scope=""Model""/>
                      </Scopes>
                    </ModelMapping>";
        }

    }
}

using libal.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.CobieLiteUk;

namespace libal.Services
{
    internal class CobieLiteUkAttributeResolver
    {
        private const string ASSETDESCRIPTION = "ASSETDESCRIPTION";
        private const string SERIALNUMBER = "SERIALNUMBER";
        private const string INSTALLTIONDATE = "INSTALLATIONDATE";
        private const string WARRANTYSTARTDATE = "WARRANTYSTARTDATE";
        private const string HEIGHT = "HEIGHT";
        private const string GROSSAREA = "GROSSAREA";
        private const string NETAREA = "NETAREA";
        private const string USEABLEHEIGHT = "USEABLEHEIGHT";
        private const string ROOMTAG = "ROOMTAG";
        private const string TAGNUMBER = "TAGNUMBER";
        private const string BARCODE = "BARCODE";
        private const string ASSETIDENTIFIER = "ASSETIDENTIFIER";
        private const string DURATIONUNIT = "DURATIONUNIT";
        private const string MODELNUMBER = "MODELNUMBER";
        private const string MODELREFERENCE = "MODELREFERENCE";
        private const string REPLACEMENTCOST = "REPLACEMENTCOST";
        private const string EXPECTEDLIFE = "EXPECTEDLIFE";
        private const string NOMINALLENGTH = "NOMINALLENGTH";
        private const string NOMINALWIDTH = "NOMINALWIDTH";
        private const string NOMINALHEIGHT = "NOMINALHEIGHT";
        private const string ACCESSIBILITYTEXT = "ACCESSIBILITYTEXT";
        private const string COLOR = "COLOR";
        private const string CONSTITUENTS = "CONSTITUENTS";
        private const string FEATURES = "FEATURES";
        private const string FINISH = "FINISH";
        private const string GRADE = "GRADE";
        private const string MATERIAL = "MATERIAL";
        private const string SHAPE = "SHAPE";
        private const string SIZE = "SIZE";
        private const string SUSTAINABILITYPERFORMANCE = "SUSTAINABILITYPERFORMANCE";
        private const string MANUFACTURER = "MANUFACTURER";
        private const string CODEPERFORMANCE = "CodePerformance";
        private const string WARRANTYDURATIONLABOR = "WARRANTYDURATIONLABOR";
        private const string WARRANTYDURATIONPARTS = "WARRANTYDURATIONPARTS";
        private const string WARRANTYDURATIONUNIT = "WARRANTYDURATIONUNIT";
        private const string WARRANTYGUARANTORLABOR = "WARRANTYGUARANTORLABOR";
        private const string WARRANTYGUARANTORPARTS = "WARRANTYGUARANTORPARTS";
        private const string WARRANTYDESCRIPTION = "WARRANTYDESCRIPTION";
        private const string SUPPLIERS = "SUPPLIERS";
        private const string SETNUMBER = "SETNUMBER";
        private const string PARTNUMBER = "PARTNUMBER";

        public static void resolveCobieAttributes(Facility facility)
        {
            resolveSpaceCobieAttributes(facility);

            resolveFloorCobieAttributes(facility);

            resolveComponentCobieAttributes(facility);

            resolveTypeCobieAttributes(facility);

            resolveSpareCobieAttributes(facility);
        }

        private static void resolveSpareCobieAttributes(Facility facility)
        {
            var spares = facility.Get<Spare>(spare => spare.Attributes != null && spare.Attributes.Count() != 0);

            Dictionary<string, List<PsetPropertyPair>> sparePropertyMap = createSparePropertyMap();

            foreach (Spare spare in spares)
            {
                List<ContactKey> contacts = parseSupplierValue(spare.Attributes.Find(a => resolveAttribute(a, SUPPLIERS, sparePropertyMap)));
                spare.Suppliers = contacts != null && contacts.Any() ? contacts : spare.Suppliers;
                
                string parsedSetNumber = parseStringValue(spare.Attributes.Find(a => resolveAttribute(a, SETNUMBER, sparePropertyMap)));
                spare.SetNumber = parsedSetNumber != null && !parsedSetNumber.Equals(string.Empty) ? parsedSetNumber : spare.SetNumber;
                
                string parsedPartNumber = parseStringValue(spare.Attributes.Find(a => resolveAttribute(a, PARTNUMBER, sparePropertyMap)));
                spare.PartNumber = parsedPartNumber != null && !parsedPartNumber.Equals(string.Empty) ? parsedPartNumber : spare.PartNumber;
            }
        }

        private static void resolveTypeCobieAttributes(Facility facility)
        {
            var types = facility.Get<AssetType>(type => type.Attributes != null && type.Attributes.Count() != 0);

            Dictionary<string, List<PsetPropertyPair>> typePropertyMap = createTypePropertyMap();

            foreach (AssetType type in types)
            {
                DurationUnit parsedUnitValue = parseDurationUnitValue(type.Attributes.Find(a => resolveAttribute(a, DURATIONUNIT, typePropertyMap)));
                type.DurationUnit = parsedUnitValue;
                
                string parsedModelNumber = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, MODELNUMBER, typePropertyMap)));
                type.ModelNumber = parsedModelNumber != null && !parsedModelNumber.Equals(string.Empty) ? parsedModelNumber : type.ModelNumber;
                
                string parsedModelReference = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, MODELREFERENCE, typePropertyMap)));
                type.ModelReference = parsedModelReference != null && !parsedModelReference.Equals(string.Empty) ? parsedModelReference : type.ModelReference;
                
                double parsedReplacementCost = parseDoubleValue(type.Attributes.Find(a => resolveAttribute(a, REPLACEMENTCOST, typePropertyMap)));
                type.ReplacementCost = !Double.IsNaN(parsedReplacementCost) ? parsedReplacementCost : type.ReplacementCost;
                
                double parsedExpectedLife = parseDoubleValue(type.Attributes.Find(a => resolveAttribute(a, EXPECTEDLIFE, typePropertyMap)));
                type.ExpectedLife = !Double.IsNaN(parsedExpectedLife) ? parsedExpectedLife : type.ExpectedLife;
                
                double parsedNominalLength = parseDoubleValue(type.Attributes.Find(a => resolveAttribute(a, NOMINALLENGTH, typePropertyMap)));
                type.NominalLength = !Double.IsNaN(parsedNominalLength) ? parsedNominalLength : type.NominalLength;
                
                double parsedNominalWidth = parseDoubleValue(type.Attributes.Find(a => resolveAttribute(a, NOMINALWIDTH, typePropertyMap)));
                type.NominalWidth = !Double.IsNaN(parsedNominalWidth) ? parsedNominalWidth : type.NominalWidth;
                
                double parsedNominalHeight = parseDoubleValue(type.Attributes.Find(a => resolveAttribute(a, NOMINALHEIGHT, typePropertyMap)));
                type.NominalHeight = !Double.IsNaN(parsedNominalHeight) ? parsedNominalHeight : type.NominalHeight;
                
                string parsedAccessibilityPerformance = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, ACCESSIBILITYTEXT, typePropertyMap)));
                type.AccessibilityPerformance = parsedAccessibilityPerformance != null && !parsedAccessibilityPerformance.Equals(string.Empty) ? parsedAccessibilityPerformance : type.AccessibilityPerformance;
                
                string parsedColor = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, COLOR, typePropertyMap)));
                type.Color = parsedColor != null && !parsedColor.Equals(string.Empty) ? parsedColor : type.Color;
                
                string parsedConstituents = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, CONSTITUENTS, typePropertyMap)));
                type.Constituents = parsedConstituents != null && !parsedConstituents.Equals(string.Empty) ? parsedConstituents : type.Constituents;

                string parsedFeatures = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, FEATURES, typePropertyMap)));
                type.Features = parsedFeatures != null && !parsedFeatures.Equals(string.Empty) ? parsedFeatures : type.Features;
                
                string parsedFinish = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, FINISH, typePropertyMap)));
                type.Finish = parsedFinish != null && !parsedFinish.Equals(string.Empty) ? parsedFinish : type.Finish;
                
                string parsedGrade = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, GRADE, typePropertyMap)));
                type.Grade = parsedGrade != null && !parsedGrade.Equals(string.Empty) ? parsedGrade : type.Grade;
                                
                string parsedMaterial = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, MATERIAL, typePropertyMap)));
                type.Material = parsedMaterial != null && !parsedMaterial.Equals(string.Empty) ? parsedMaterial : type.Material;

                string parsedShape = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, SHAPE, typePropertyMap)));
                type.Shape = parsedShape != null && !parsedShape.Equals(string.Empty) ? parsedShape : type.Shape;
                
                string parsedSize = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, SIZE, typePropertyMap)));
                type.Size = parsedSize != null && !parsedSize.Equals(string.Empty) ? parsedSize : type.Size;
                
                string parsedSustainabilityPerformance = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, SUSTAINABILITYPERFORMANCE, typePropertyMap)));
                type.SustainabilityPerformance = parsedSustainabilityPerformance != null && !parsedSustainabilityPerformance.Equals(string.Empty) ? parsedSustainabilityPerformance : type.SustainabilityPerformance;
                
                string parsedManufacturer = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, MANUFACTURER, typePropertyMap)));
                type.Manufacturer = parsedManufacturer != null && !parsedManufacturer.Equals(string.Empty) ? createContactKey(parsedManufacturer) : type.Manufacturer;
                
                string parsedCodePerformance = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, CODEPERFORMANCE, typePropertyMap)));
                type.CodePerformance = parsedCodePerformance != null && !parsedCodePerformance.Equals(string.Empty) ? parsedCodePerformance : type.CodePerformance;
                
                Warranty warranty = type.Warranty != null ? type.Warranty : new Warranty();
               
                double parsedDurationLabor = parseDoubleValue(type.Attributes.Find(a => resolveAttribute(a, WARRANTYDURATIONLABOR, typePropertyMap)));
                warranty.DurationLabor = !Double.IsNaN(parsedDurationLabor) ? parsedDurationLabor : warranty.DurationLabor;
                
                double parsedDurationParts = parseDoubleValue(type.Attributes.Find(a => resolveAttribute(a, WARRANTYDURATIONPARTS, typePropertyMap)));
                warranty.DurationLabor = !Double.IsNaN(parsedDurationParts) ? parsedDurationParts : warranty.DurationLabor;
                
                DurationUnit parsedDurationUnit = parseDurationUnitValue(type.Attributes.Find(a => resolveAttribute(a, WARRANTYDURATIONUNIT, typePropertyMap)));
                warranty.DurationUnit = parsedDurationUnit;

                string parsedGuarantorLabor = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, WARRANTYGUARANTORLABOR, typePropertyMap)));
                warranty.GuarantorLabor = parsedGuarantorLabor != null && !parsedGuarantorLabor.Equals(string.Empty) ? createContactKey(parsedGuarantorLabor) : warranty.GuarantorLabor;
                
                string parsedGuarantorParts = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, WARRANTYGUARANTORPARTS, typePropertyMap)));
                warranty.GuarantorParts = parsedGuarantorParts != null && !parsedGuarantorParts.Equals(string.Empty) ? createContactKey(parsedGuarantorParts) : warranty.GuarantorParts;
                
                string parsedWarrantyDescription = parseStringValue(type.Attributes.Find(a => resolveAttribute(a, WARRANTYDESCRIPTION, typePropertyMap)));
                type.Description = parsedWarrantyDescription != null && !parsedWarrantyDescription.Equals(string.Empty) ? parsedWarrantyDescription : type.Description;

                type.Warranty = warranty;
            }
        }

        private static void resolveComponentCobieAttributes(Facility facility)
        {
            var components = facility.Get<Asset>(asset => asset.Attributes != null && asset.Attributes.Count() != 0);

            Dictionary<string, List<PsetPropertyPair>> assetPropertyMaps = createAssetPropertyMap();

            foreach (Asset component in components)
            {
                string parsedSerialNumber = parseStringValue(component.Attributes.Find(a => resolveAttribute(a, SERIALNUMBER, assetPropertyMaps)));
                component.SerialNumber = parsedSerialNumber != null && !parsedSerialNumber.Equals(string.Empty) ? parsedSerialNumber : component.SerialNumber;
                
                DateTime parsedInstallationDate = parseDateValue(component.Attributes.Find(a => resolveAttribute(a, INSTALLTIONDATE, assetPropertyMaps)));
                component.InstallationDate = parsedInstallationDate != null && !DateTime.MinValue.Equals(parsedInstallationDate) ? parsedInstallationDate : component.InstallationDate;
                
                DateTime parsedWarrantyStartDate = parseDateValue(component.Attributes.Find(a => resolveAttribute(a, WARRANTYSTARTDATE, assetPropertyMaps)));
                component.WarrantyStartDate = parsedWarrantyStartDate != null && !DateTime.MinValue.Equals(parsedWarrantyStartDate) ? parsedWarrantyStartDate : component.WarrantyStartDate;
                
                string parsedTagNumber = parseStringValue(component.Attributes.Find(a => resolveAttribute(a, TAGNUMBER, assetPropertyMaps)));
                component.TagNumber = parsedTagNumber != null && !parsedTagNumber.Equals(string.Empty) ? parsedTagNumber : component.TagNumber;
                
                string parsedBarCode = parseStringValue(component.Attributes.Find(a => resolveAttribute(a, BARCODE, assetPropertyMaps)));
                component.BarCode = parsedBarCode != null && !parsedBarCode.Equals(string.Empty) ? parsedBarCode : component.BarCode;
                
                string parsedAssetIdentifier = parseStringValue(component.Attributes.Find(a => resolveAttribute(a, ASSETIDENTIFIER, assetPropertyMaps)));
                component.AssetIdentifier = parsedAssetIdentifier != null && !parsedAssetIdentifier.Equals(string.Empty) ? parsedAssetIdentifier : component.AssetIdentifier;
            }
        }

        private static void resolveFloorCobieAttributes(Facility facility)
        {
            var floors = facility.Get<Floor>(floor => floor.Attributes != null && floor.Attributes.Count() != 0);

            Dictionary<string, List<PsetPropertyPair>> floorPropertyMaps = createFloorPropertyMap();

            foreach (Floor floor in floors)
            {
                double parsedHeight = parseDoubleValue(floor.Attributes.Find(a => resolveAttribute(a, HEIGHT, floorPropertyMaps)));
                floor.Height = !Double.IsNaN(parsedHeight) ? parsedHeight : floor.Height;        
            }
        }

        private static void resolveSpaceCobieAttributes(Facility facility)
        {
            var spaces = facility.Get<Space>(space => space.Attributes != null && space.Attributes.Count() != 0);

            Dictionary<string, List<PsetPropertyPair>> spacePropertyMaps = createSpacePropertyMap();

            foreach (Space space in spaces)
            {
                double parsedValueGross = parseDoubleValue(space.Attributes.Find(a => resolveAttribute(a, GROSSAREA, spacePropertyMaps)));
                space.GrossArea = !Double.IsNaN(parsedValueGross) ? parsedValueGross : space.GrossArea;
                
                double parsedNetArea = parseDoubleValue(space.Attributes.Find(a => resolveAttribute(a, NETAREA, spacePropertyMaps)));
                space.NetArea = !Double.IsNaN(parsedNetArea) ? parsedNetArea : space.NetArea;

                double parsedUsableHeight = parseDoubleValue(space.Attributes.Find(a => resolveAttribute(a, USEABLEHEIGHT, spacePropertyMaps)));
                space.UsableHeight = !Double.IsNaN(parsedUsableHeight) ? parsedUsableHeight : space.UsableHeight;

                string parsedRoomTag = parseStringValue(space.Attributes.Find(a => resolveAttribute(a, ROOMTAG, spacePropertyMaps)));
                space.RoomTag = parsedRoomTag != null && !parsedRoomTag.Equals(string.Empty) ? parsedRoomTag : space.RoomTag;
            }
        }

        private static double parseDoubleValue(Xbim.CobieLiteUk.Attribute attribute)
        {
            if(attribute != null)
            {
                double parsedValue = Double.NaN;
                Double.TryParse(attribute.Value.GetStringValue(), out parsedValue);
                return parsedValue;
            }

            return Double.NaN;
        }

        private static List<ContactKey> parseSupplierValue(Xbim.CobieLiteUk.Attribute attribute)
        {
            List<ContactKey> contactKeys = new List<ContactKey>();
            if (attribute != null && attribute.Value != null)
            {
                string[] emailList = attribute.Value.GetStringValue().Split(new[] { ':', ';', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string email in emailList)
                {
                    contactKeys.Add(createContactKey(email));
                }
            }

            return contactKeys;
        }

        private static string parseStringValue(Xbim.CobieLiteUk.Attribute attribute)
        {
            return attribute != null && attribute.Value != null
                ? attribute.Value.GetStringValue() 
                : string.Empty;
        }

        private static DateTime parseDateValue(Xbim.CobieLiteUk.Attribute attribute)
        {
            if(attribute != null && attribute.Value != null)
            {
                DateTime dt;
                DateTime.TryParseExact(attribute.Value.GetStringValue(),
                       "MM.dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out dt);
                return dt;
            }

            return DateTime.MinValue;
        }


        private static DurationUnit parseDurationUnitValue(Xbim.CobieLiteUk.Attribute attribute)
        {
            DurationUnit result = DurationUnit.asrequired;
            
            string asString = parseStringValue(attribute);

            if(asString != null && !asString.Equals(string.Empty))
            {
                result = (DurationUnit)Enum.Parse(typeof(DurationUnit), asString, true);
            }

            return result;
        }
        private static ContactKey createContactKey(string parsedStringValue)
        {
            return new ContactKey { Email = parsedStringValue };
        }
        private static bool resolveAttribute(Xbim.CobieLiteUk.Attribute attribute, string key, Dictionary<string, List<PsetPropertyPair>> map)
        {
            List<PsetPropertyPair> values;
            map.TryGetValue(key, out values);
            if (values != null && values.Count() > 0 && attribute != null && attribute.Value != null)
            {
                PsetPropertyPair psetPropertyPair = values.Find(v => attribute.Name.ToUpper().Equals(v.propertyName.ToUpper()) && attribute.PropertySetName.ToUpper().Equals(v.psetName.ToUpper()));
                if(psetPropertyPair != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static Dictionary<string, List<PsetPropertyPair>> createSpacePropertyMap()
        {
            Dictionary<string, List<PsetPropertyPair>> spacePropertyMaps = new Dictionary<string, List<PsetPropertyPair>>();

            List<PsetPropertyPair> grossAreaPairs = new List<PsetPropertyPair>();
            grossAreaPairs.Add(new PsetPropertyPair("LIBAL_QTO_SPACEBASEQUANTITIES", "GrossFloorArea"));
            grossAreaPairs.Add(new PsetPropertyPair("LIBAL_QTO_BASEQUANTITIES", "GrossFloorArea"));
            grossAreaPairs.Add(new PsetPropertyPair("PSET_BASEQUANTITIES", "GrossFloorArea"));
            grossAreaPairs.Add(new PsetPropertyPair("BASEQUANTITIES", "GrossFloorArea"));
            grossAreaPairs.Add(new PsetPropertyPair("PSET_SPACECOMMON", "GrossPlannedArea"));
            spacePropertyMaps.Add(GROSSAREA, grossAreaPairs);

            List<PsetPropertyPair> netAreaPairs = new List<PsetPropertyPair>();
            netAreaPairs.Add(new PsetPropertyPair("LIBAL_QTO_SPACEBASEQUANTITIES", "NetFloorArea"));
            netAreaPairs.Add(new PsetPropertyPair("LIBAL_QTO_BASEQUANTITIES", "NetFloorArea"));
            netAreaPairs.Add(new PsetPropertyPair("PSET_BASEQUANTITIES", "NetFloorArea"));
            netAreaPairs.Add(new PsetPropertyPair("BASEQUANTITIES", "NetFloorArea"));
            netAreaPairs.Add(new PsetPropertyPair("PSET_SPACECOMMON", "GrossPlannedArea"));
            spacePropertyMaps.Add(NETAREA, netAreaPairs);

            List<PsetPropertyPair> useableHeightPairs = new List<PsetPropertyPair>();
            useableHeightPairs.Add(new PsetPropertyPair("LIBAL_QTO_SPACEBASEQUANTITIES", "NetHeight"));
            useableHeightPairs.Add(new PsetPropertyPair("LIBAL_QTO_BASEQUANTITIES", "NetHeight"));
            useableHeightPairs.Add(new PsetPropertyPair("LIBAL_QTO_SPACEBASEQUANTITIES", "FinishCeilingHeight"));
            useableHeightPairs.Add(new PsetPropertyPair("LIBAL_QTO_BASEQUANTITIES", "FinishCeilingHeight"));
            useableHeightPairs.Add(new PsetPropertyPair("PSET_SPACECOMMON", "UsableHeight"));
            useableHeightPairs.Add(new PsetPropertyPair("DIMENSIONS", "UsableHeight"));
            useableHeightPairs.Add(new PsetPropertyPair("BASEQUANTITIES", "Height"));
            spacePropertyMaps.Add(USEABLEHEIGHT, useableHeightPairs);

            List<PsetPropertyPair> roomTagPairs = new List<PsetPropertyPair>();
            roomTagPairs.Add(new PsetPropertyPair("COBIE_SPACE", "RoomTag"));
            roomTagPairs.Add(new PsetPropertyPair("PSET_SPACECOMMON", "Reference"));
            roomTagPairs.Add(new PsetPropertyPair("PSET_SPACECOMMON", "RoomTag"));
            spacePropertyMaps.Add(ROOMTAG, roomTagPairs);

            return spacePropertyMaps;
        }
        private static Dictionary<string, List<PsetPropertyPair>> createFloorPropertyMap()
        {
            Dictionary<string, List<PsetPropertyPair>> floorPropertyMaps = new Dictionary<string, List<PsetPropertyPair>>();

            List<PsetPropertyPair> heightPairs = new List<PsetPropertyPair>();
            heightPairs.Add(new PsetPropertyPair("LIBAL_QTO_BUILDINGSTOREYBASEQUANTITIES", "NetHeight"));
            heightPairs.Add(new PsetPropertyPair("LIBAL_QTO_BASEQUANTITIES", "NetHeight"));
            heightPairs.Add(new PsetPropertyPair("BASEQUANTITIES", "NominalHeight"));
            heightPairs.Add(new PsetPropertyPair("BASEQUANTITIES", "Height"));
            floorPropertyMaps.Add(HEIGHT, heightPairs);

            return floorPropertyMaps;
        }

        private static Dictionary<string, List<PsetPropertyPair>> createAssetPropertyMap()
        {
            Dictionary<string, List<PsetPropertyPair>> assetPropertyMaps = new Dictionary<string, List<PsetPropertyPair>>();

            List<PsetPropertyPair> nbsDescriptionPairs = new List<PsetPropertyPair>();
            nbsDescriptionPairs.Add(new PsetPropertyPair("OTHER", "NBSDescription"));
            assetPropertyMaps.Add(ASSETDESCRIPTION, nbsDescriptionPairs);

            List<PsetPropertyPair> serialNumberPairs = new List<PsetPropertyPair>();
            serialNumberPairs.Add(new PsetPropertyPair("PSET_MANUFACTUREROCCURENCE", "SerialNumber"));
            serialNumberPairs.Add(new PsetPropertyPair("PSET_MANUFACTUREROCCURRENCE", "SerialNumber"));
            serialNumberPairs.Add(new PsetPropertyPair("PSET_COMPONENT", "SerialNumber"));
            serialNumberPairs.Add(new PsetPropertyPair("OTHER", "SerialNumber"));
            assetPropertyMaps.Add(SERIALNUMBER, serialNumberPairs);

            List<PsetPropertyPair> installtionDatePairs = new List<PsetPropertyPair>();
            installtionDatePairs.Add(new PsetPropertyPair("COBIE_COMPONENT", "InstallationDate"));
            installtionDatePairs.Add(new PsetPropertyPair("PSET_COMPONENT", "InstallationDate"));
            installtionDatePairs.Add(new PsetPropertyPair("OTHER", "InstallationDate"));
            assetPropertyMaps.Add(INSTALLTIONDATE, installtionDatePairs);

            List<PsetPropertyPair> warrantyStartDatePairs = new List<PsetPropertyPair>();
            warrantyStartDatePairs.Add(new PsetPropertyPair("COBIE_COMPONENT", "InstallationDate"));
            warrantyStartDatePairs.Add(new PsetPropertyPair("PSET_COMPONENT", "InstallationDate"));
            warrantyStartDatePairs.Add(new PsetPropertyPair("OTHER", "InstallationDate"));
            assetPropertyMaps.Add(WARRANTYSTARTDATE, installtionDatePairs);

            List<PsetPropertyPair> tagNumberPairs = new List<PsetPropertyPair>();
            tagNumberPairs.Add(new PsetPropertyPair("COBIE_COMPONENT", "TagNumber"));
            tagNumberPairs.Add(new PsetPropertyPair("PSET_COMPONENT", "TagNumber"));
            tagNumberPairs.Add(new PsetPropertyPair("OTHER", "TagNumber"));
            assetPropertyMaps.Add(TAGNUMBER, tagNumberPairs);

            List<PsetPropertyPair> barCodePairs = new List<PsetPropertyPair>();
            barCodePairs.Add(new PsetPropertyPair("PSET_MANUFACTUREROCCURENCE", "BarCode"));
            barCodePairs.Add(new PsetPropertyPair("PSET_MANUFACTUREROCCURRENCE", "BarCode"));
            barCodePairs.Add(new PsetPropertyPair("COBIE_COMPONENT", "BarCode"));
            barCodePairs.Add(new PsetPropertyPair("PSET_COMPONENT", "BarCode"));
            barCodePairs.Add(new PsetPropertyPair("OTHER", "BarCode"));
            assetPropertyMaps.Add(BARCODE, barCodePairs);

            List<PsetPropertyPair> assetIdentifierPairs = new List<PsetPropertyPair>();
            assetIdentifierPairs.Add(new PsetPropertyPair("COBIE_COMPONENT", "AssetIdentifier"));
            assetIdentifierPairs.Add(new PsetPropertyPair("PSET_COMPONENT", "AssetIdentifier"));
            assetIdentifierPairs.Add(new PsetPropertyPair("OTHER", "AssetIdentifier"));
            assetPropertyMaps.Add(ASSETIDENTIFIER, assetIdentifierPairs);

            return assetPropertyMaps;
        }
        private static Dictionary<string, List<PsetPropertyPair>> createTypePropertyMap()
        {
            Dictionary<string, List<PsetPropertyPair>> typePropertyMaps = new Dictionary<string, List<PsetPropertyPair>>();

            List<PsetPropertyPair> durationUnitPairs = new List<PsetPropertyPair>();
            durationUnitPairs.Add(new PsetPropertyPair("OTHER", "DURATIONUNIT"));
            typePropertyMaps.Add(DURATIONUNIT, durationUnitPairs);

            List<PsetPropertyPair> modelNumberPairs = new List<PsetPropertyPair>();
            modelNumberPairs.Add(new PsetPropertyPair("PSET_MANUFACTURERTYPEINFORMATION", "ModelLabel"));
            modelNumberPairs.Add(new PsetPropertyPair("OTHER", "ModelNumber"));
            typePropertyMaps.Add(MODELNUMBER, modelNumberPairs);

            List<PsetPropertyPair> modelReferencePairs = new List<PsetPropertyPair>();
            modelReferencePairs.Add(new PsetPropertyPair("PSET_MANUFACTURERTYPEINFORMATION", "ModelReference"));
            modelReferencePairs.Add(new PsetPropertyPair("OTHER", "ModelReference"));
            typePropertyMaps.Add(MODELREFERENCE, modelReferencePairs);

            List<PsetPropertyPair> replacementCostPairs = new List<PsetPropertyPair>();
            replacementCostPairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "ReplacementCost"));
            replacementCostPairs.Add(new PsetPropertyPair("COBIE_ECONOMICIMPACTVALUES", "ReplacementCost"));
            replacementCostPairs.Add(new PsetPropertyPair("PSET_ECONOMICIMPACTVALUES", "ReplacementCost"));
            replacementCostPairs.Add(new PsetPropertyPair("OTHER", "ReplacementCost"));
            typePropertyMaps.Add(REPLACEMENTCOST, replacementCostPairs);

            List<PsetPropertyPair> expectedLifePairs = new List<PsetPropertyPair>();
            expectedLifePairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "ReplacementCost"));
            expectedLifePairs.Add(new PsetPropertyPair("COBIE_ECONOMICIMPACTVALUES", "ReplacementCost"));
            expectedLifePairs.Add(new PsetPropertyPair("PSET_ECONOMICIMPACTVALUES", "ReplacementCost"));
            expectedLifePairs.Add(new PsetPropertyPair("OTHER", "ReplacementCost"));
            typePropertyMaps.Add(EXPECTEDLIFE, expectedLifePairs);

            List<PsetPropertyPair> nominalLengthPairs = new List<PsetPropertyPair>();
            nominalLengthPairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "NominalLength"));
            nominalLengthPairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "NominalLength"));
            nominalLengthPairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "NominalLength"));
            nominalLengthPairs.Add(new PsetPropertyPair("OTHER", "NominalLength"));
            nominalLengthPairs.Add(new PsetPropertyPair("BASEQUANTITIES", "Length"));
            typePropertyMaps.Add(NOMINALLENGTH, nominalLengthPairs);

            List<PsetPropertyPair> nominalWidthPairs = new List<PsetPropertyPair>();
            nominalWidthPairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "NominalWidth"));
            nominalWidthPairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "NominalWidth"));
            nominalWidthPairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "NominalWidth"));
            nominalWidthPairs.Add(new PsetPropertyPair("OTHER", "NominalWidth"));
            nominalWidthPairs.Add(new PsetPropertyPair("BASEQUANTITIES", "Width"));
            typePropertyMaps.Add(NOMINALWIDTH, nominalWidthPairs);

            List<PsetPropertyPair> nominalHeightPairs = new List<PsetPropertyPair>();
            nominalHeightPairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "NominalHeight"));
            nominalHeightPairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "NominalHeight"));
            nominalHeightPairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "NominalHeight"));
            nominalHeightPairs.Add(new PsetPropertyPair("OTHER", "NominalHeight"));
            nominalHeightPairs.Add(new PsetPropertyPair("BASEQUANTITIES", "Height"));
            typePropertyMaps.Add(NOMINALHEIGHT, nominalHeightPairs);

            List<PsetPropertyPair> accessibilityTextPairs = new List<PsetPropertyPair>();
            accessibilityTextPairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "AccessibilityPerformance"));
            accessibilityTextPairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "AccessibilityPerformance"));
            accessibilityTextPairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "AccessibilityPerformance"));
            accessibilityTextPairs.Add(new PsetPropertyPair("OTHER", "AccessibilityPerformance"));
            typePropertyMaps.Add(ACCESSIBILITYTEXT, accessibilityTextPairs);

            List<PsetPropertyPair> colorPairs = new List<PsetPropertyPair>();
            colorPairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "Color"));
            colorPairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "Colour"));
            colorPairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "Color"));
            colorPairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "Colour"));
            colorPairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "Color"));
            colorPairs.Add(new PsetPropertyPair("OTHER", "Colour"));
            colorPairs.Add(new PsetPropertyPair("OTHER", "Color"));
            typePropertyMaps.Add(COLOR, colorPairs);

            List<PsetPropertyPair> constituentsPairs = new List<PsetPropertyPair>();
            constituentsPairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "Constituents"));
            constituentsPairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "Constituents"));
            constituentsPairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "Constituents"));
            constituentsPairs.Add(new PsetPropertyPair("OTHER", "Constituents"));
            typePropertyMaps.Add(CONSTITUENTS, constituentsPairs);

            List<PsetPropertyPair> featuresPairs = new List<PsetPropertyPair>();
            featuresPairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "Features"));
            featuresPairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "Features"));
            featuresPairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "Features"));
            featuresPairs.Add(new PsetPropertyPair("OTHER", "Features"));
            typePropertyMaps.Add(FEATURES, featuresPairs);

            List<PsetPropertyPair> finishPairs = new List<PsetPropertyPair>();
            finishPairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "Finish"));
            finishPairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "Finish"));
            finishPairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "Finish"));
            finishPairs.Add(new PsetPropertyPair("OTHER", "Finish"));
            typePropertyMaps.Add(FINISH, finishPairs);

            List<PsetPropertyPair> gradePairs = new List<PsetPropertyPair>();
            gradePairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "Grade"));
            gradePairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "Grade"));
            gradePairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "Grade"));
            gradePairs.Add(new PsetPropertyPair("OTHER", "Grade"));
            typePropertyMaps.Add(GRADE, gradePairs);

            List<PsetPropertyPair> materialPairs = new List<PsetPropertyPair>();
            materialPairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "Material"));
            materialPairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "Material"));
            materialPairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "Material"));
            materialPairs.Add(new PsetPropertyPair("OTHER", "Material"));
            typePropertyMaps.Add(MATERIAL, materialPairs);

            List<PsetPropertyPair> shapePairs = new List<PsetPropertyPair>();
            shapePairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "Shape"));
            shapePairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "Shape"));
            shapePairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "Shape"));
            shapePairs.Add(new PsetPropertyPair("OTHER", "Shape"));
            typePropertyMaps.Add(SHAPE, shapePairs);

            List<PsetPropertyPair> sizePairs = new List<PsetPropertyPair>();
            sizePairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "Size"));
            sizePairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "Size"));
            sizePairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "Size"));
            sizePairs.Add(new PsetPropertyPair("OTHER", "Size"));
            typePropertyMaps.Add(SIZE, sizePairs);

            List<PsetPropertyPair> sustainabilityPerformancePairs = new List<PsetPropertyPair>();
            sustainabilityPerformancePairs.Add(new PsetPropertyPair("COBIE_ELEMENTTYPE", "SustainabilityPerformance"));
            sustainabilityPerformancePairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "SustainabilityPerformance"));
            sustainabilityPerformancePairs.Add(new PsetPropertyPair("PSET_SPECIFICATION", "SustainabilityPerformance"));
            sustainabilityPerformancePairs.Add(new PsetPropertyPair("OTHER", "SustainabilityPerformance"));
            typePropertyMaps.Add(SUSTAINABILITYPERFORMANCE, sustainabilityPerformancePairs);

            List<PsetPropertyPair> manufacturerPairs = new List<PsetPropertyPair>();
            manufacturerPairs.Add(new PsetPropertyPair("PSET_MANUFACTURERTYPEINFORMATION", "Manufacturer"));
            manufacturerPairs.Add(new PsetPropertyPair("OTHER", "Manufacturer"));
            typePropertyMaps.Add(MANUFACTURER, manufacturerPairs);

            List<PsetPropertyPair> codePerformancePairs = new List<PsetPropertyPair>();
            codePerformancePairs.Add(new PsetPropertyPair("COBIE_SPECIFICATION", "CodePerformance"));
            codePerformancePairs.Add(new PsetPropertyPair("OTHER", "CodePerformance"));
            typePropertyMaps.Add(CODEPERFORMANCE, codePerformancePairs);

            List<PsetPropertyPair> warrantyDurationLaborPairs = new List<PsetPropertyPair>();
            warrantyDurationLaborPairs.Add(new PsetPropertyPair("COBIE_WARRANTY", "WarrantyDurationLabor"));
            warrantyDurationLaborPairs.Add(new PsetPropertyPair("OTHER", "WarrantyDurationLabor"));
            typePropertyMaps.Add(WARRANTYDURATIONLABOR, warrantyDurationLaborPairs);

            List<PsetPropertyPair> warrantyDurationPartsPairs = new List<PsetPropertyPair>();
            warrantyDurationPartsPairs.Add(new PsetPropertyPair("COBIE_WARRANTY", "WarrantyDurationParts"));
            warrantyDurationPartsPairs.Add(new PsetPropertyPair("OTHER", "WarrantyDurationParts"));
            typePropertyMaps.Add(WARRANTYDURATIONPARTS, warrantyDurationPartsPairs);

            List<PsetPropertyPair> warrantyDurationUnitPairs = new List<PsetPropertyPair>();
            warrantyDurationUnitPairs.Add(new PsetPropertyPair("COBIE_WARRANTY", "WarrantyDurationUnit"));
            warrantyDurationUnitPairs.Add(new PsetPropertyPair("OTHER", "WarrantyDurationUnit"));
            typePropertyMaps.Add(WARRANTYDURATIONUNIT, warrantyDurationUnitPairs);

            List<PsetPropertyPair> warrantyGuarantorLaborPairs = new List<PsetPropertyPair>();
            warrantyGuarantorLaborPairs.Add(new PsetPropertyPair("COBIE_WARRANTY", "WarrantyDurationUnit"));
            warrantyGuarantorLaborPairs.Add(new PsetPropertyPair("OTHER", "WarrantyDurationUnit"));
            warrantyGuarantorLaborPairs.Add(new PsetPropertyPair("COBIE_WARRANTY", "WarrantyGuarantorParts"));
            warrantyGuarantorLaborPairs.Add(new PsetPropertyPair("OTHER", "WarrantyGuarantorParts"));
            typePropertyMaps.Add(WARRANTYGUARANTORLABOR, warrantyGuarantorLaborPairs);

            List<PsetPropertyPair> warrantyGuarantorPartsPairs = new List<PsetPropertyPair>();
            warrantyGuarantorPartsPairs.Add(new PsetPropertyPair("COBIE_WARRANTY", "WarrantyGuarantorParts"));
            warrantyGuarantorPartsPairs.Add(new PsetPropertyPair("OTHER", "WarrantyGuarantorParts"));
            warrantyGuarantorPartsPairs.Add(new PsetPropertyPair("COBIE_WARRANTY", "WarrantyDurationUnit"));
            warrantyGuarantorPartsPairs.Add(new PsetPropertyPair("OTHER", "WarrantyDurationUnit"));
            typePropertyMaps.Add(WARRANTYGUARANTORPARTS, warrantyGuarantorPartsPairs);

            List<PsetPropertyPair> warrantyDescriptionPairs = new List<PsetPropertyPair>();
            warrantyDescriptionPairs.Add(new PsetPropertyPair("COBIE_WARRANTY", "WarrantyDescription"));
            warrantyDescriptionPairs.Add(new PsetPropertyPair("OTHER", "WarrantyDescription"));
            typePropertyMaps.Add(WARRANTYDESCRIPTION, warrantyDescriptionPairs);

            return typePropertyMaps;
        }
        private static Dictionary<string, List<PsetPropertyPair>> createSparePropertyMap()
        {
            Dictionary<string, List<PsetPropertyPair>> sparePropertyMaps = new Dictionary<string, List<PsetPropertyPair>>();

            List<PsetPropertyPair> suppliersPairs = new List<PsetPropertyPair>();
            suppliersPairs.Add(new PsetPropertyPair("COBie_Spare", "Suppliers"));
            sparePropertyMaps.Add(SUPPLIERS, suppliersPairs);

            List<PsetPropertyPair> setNumberPairs = new List<PsetPropertyPair>();
            setNumberPairs.Add(new PsetPropertyPair("COBie_Spare", "SetNumber"));
            sparePropertyMaps.Add(SETNUMBER, setNumberPairs);

            List<PsetPropertyPair> partNumberPairs = new List<PsetPropertyPair>();
            partNumberPairs.Add(new PsetPropertyPair("COBie_Spare", "PartNumber"));
            sparePropertyMaps.Add(PARTNUMBER, partNumberPairs);

            return sparePropertyMaps;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xbim.CobieLiteUk;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using XbimExchanger.IfcHelpers;
using XbimExchanger.IfcToCOBieLiteUK.EqCompare;

namespace libal.Services
{
    public class CobieLiteUkAsyncConverterService : ICobieLiteUkAsyncConverterService
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
                    return await ConvertIfcToCobieLiteUkAsync(ifcStore, destination);
                }
            }
        }

        private Task<String> ConvertIfcToCobieLiteUkAsync(IfcStore ifcStore, string fileName)
        {
            return Task.Run<String>(() =>
            {

                var defaultExtSystem = "LIBAL";
                var siteName = "";
                var phase = "";

                var psetPropertyToUnitDictionary = new Dictionary<string, string>();
                bool isIfc2x3 = ifcStore.SchemaVersion == Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3;
                if (isIfc2x3)
                {
                    Ifc2x3IfcEntityLabelGenerator.enrichModelWithIfcEntityLabel(ifcStore);
                    defaultExtSystem = Ifc2x3IfcEntityLabelGenerator.getExtSystem(ifcStore);
                    siteName = Ifc2x3IfcEntityLabelGenerator.getSiteName(ifcStore);
                    phase = Ifc2x3IfcEntityLabelGenerator.getPhase(ifcStore);


                    Ifc2x3QuantitySetGenerator.ResolveQuantitySet(ifcStore);
                    Ifc2x3UnitResolver.ResolveUnits(ifcStore, psetPropertyToUnitDictionary);
                }
                else
                {
                    Ifc4x1IfcEntityLabelGenerator.enrichModelWithIfcEntityLabel(ifcStore);
                    defaultExtSystem = Ifc4x1IfcEntityLabelGenerator.getExtSystem(ifcStore);
                    siteName = Ifc4x1IfcEntityLabelGenerator.getSiteName(ifcStore);
                    phase = Ifc4x1IfcEntityLabelGenerator.getPhase(ifcStore);

                    Ifc4x1QuantitySetGenerator.ResolveQuantitySet(ifcStore);
                    Ifc4x1UnitResolver.ResolveUnits(ifcStore, psetPropertyToUnitDictionary);
                }

                var memStream = new MemoryStream();
                var facilities = new List<Facility>();
                var ifcToCoBieLiteUkExchanger = new LibalIfcToCOBieLiteUkExchanger(ifcStore, facilities, null, null, null, EntityIdentifierMode.GloballyUniqueIds, SystemExtractionMode.System);

                facilities = ifcToCoBieLiteUkExchanger.Convert();
                var facility = facilities.ToArray()[0];

                if (facility.Site != null) {
                    facility.Site.Name = siteName;
                }
                facility.Phase = phase;

                AddUnitsToAttributes(facility, psetPropertyToUnitDictionary);
                CobieLiteUkAttributeResolver.resolveCobieAttributes(facility);
                ResolveContacts(facility, defaultExtSystem);
                MapSystems(ifcStore, ifcToCoBieLiteUkExchanger, facility);

                if (ifcStore.SchemaVersion == Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3)
                {
                    Ifc2x3IfcEntityLabelGenerator.resolveExtObjectWithPredefinedType(ifcStore, facility);
                }
                else {
                    Ifc4x1IfcEntityLabelGenerator.resolveExtObjectWithPredefinedType(ifcStore, facility);
                }

                facility.ValidateUK2012(Console.Out, true);

                SetCorrectQSetName(facility);
                SetCorrectPsetName(facility);

                CleanupSystems(facility);
                CleanupZones(facility);

                // set ifc version in facility metadata
                facility.Metadata.Status = ifcStore.SchemaVersion.ToString();

                facility.WriteXml(memStream);

                _s3.Create(fileName, new MemoryStream(memStream.ToArray()));

                return "processed";
            });
        }

        private void SetCorrectPsetName(Facility facility)
        {
            var attributesWithEndingPsetName = facility.Get<Xbim.CobieLiteUk.Attribute>(a => a.PropertySetName != null && a.Name != null && (a.Name.EndsWith("_" + a.PropertySetName)));
            foreach (var attribute in attributesWithEndingPsetName)
            {
                if (attribute.Name != null && attribute.Name.EndsWith("_" + attribute.PropertySetName))
                {
                    attribute.Name = attribute.Name.Replace("_" + attribute.PropertySetName.ToString(), "");
                }
            }
        }

        private void CleanupZones(Facility facility)
        {
            List<Zone> zones = System.Linq.Enumerable.ToList(facility.Get<Zone>());
            Zone defaultZone = zones.Find(zone => zone.Name.Equals("Default Zone"));
            if (defaultZone != null)
            {
                zones.Remove(defaultZone);
                facility.Zones = zones;
            }
        }

        private void CleanupSystems(Facility facility)
        {
            List<Xbim.CobieLiteUk.System> systems = System.Linq.Enumerable.ToList(facility.Get<Xbim.CobieLiteUk.System>());
            Xbim.CobieLiteUk.System defaultSystem = systems.Find(system => system.Name.Equals("Default system"));
            if (defaultSystem != null)
            {
                systems.Remove(defaultSystem);
                facility.Systems = systems;
            }
        }

        private void AddUnitsToAttributes(Facility facility, Dictionary<string, string> dictionary)
        {
            var attributesWithDots = facility.Get<Xbim.CobieLiteUk.Attribute>();
            foreach (var attribute in attributesWithDots)
            {
                var attributeName = attribute.Name;
                if (attributeName.Contains("."))
                {
                    attribute.Name = attribute.Name.Replace('.', '_');
                }

                string key = attribute.ExternalEntity + "_" + attributeName;

                string altKey = attributeName.Contains("." + attribute.ExternalEntity)
                    ? attribute.ExternalEntity + "_" + attributeName.Replace("." + attribute.ExternalEntity, "")
                    : "n/a";

                if (dictionary != null && dictionary.ContainsKey(key) && attribute.Value != null)
                {
                    attribute.Value.Unit = dictionary[key];
                } else if(dictionary != null && altKey != "n/a" && dictionary.ContainsKey(altKey) && attribute.Value != null)
                {
                    attribute.Value.Unit = dictionary[altKey];
                }

            }
        }

        private void SetCorrectQSetName(Facility facility)
        {
            var attributesWithQsets = facility.Get<Xbim.CobieLiteUk.Attribute>(a => a.PropertySetName != null && (a.PropertySetName.StartsWith("LIBAL_Qto_") || a.PropertySetName.StartsWith("Qto_")));
            foreach (var attribute in attributesWithQsets)
            {
                if (attribute.Name != null && attribute.Name.EndsWith("_" + attribute.PropertySetName))
                {
                    attribute.Name = attribute.Name.Replace("_" + attribute.PropertySetName.ToString(), "");
                } 
                else if (attribute.Name != null && attribute.Name.EndsWith(attribute.PropertySetName))
                {
                    attribute.Name = attribute.Name.Replace(attribute.PropertySetName.ToString(), "");
                }

                attribute.PropertySetName = attribute.PropertySetName.Replace("LIBAL_Qto_", "Qto_");
            }
        }

        private void MapSystems(IfcStore ifcStore, LibalIfcToCOBieLiteUkExchanger ifcToCoBieLiteUkExchanger, Facility facility)
        {
            var systemDictionary = Enumerable.ToList(ifcStore.Instances.OfType<IIfcRelAssignsToGroup>())
               .Where(r => (r.RelatingGroup is IIfcSystem) && !(r.RelatingGroup is IIfcZone))
               .ToDictionary(k => (IIfcSystem)k.RelatingGroup, v => v.RelatedObjects);

            facility.Systems = new List<Xbim.CobieLiteUk.System>();
            foreach (var keyValue in systemDictionary)
            {
                Xbim.CobieLiteUk.System mappedSystem = ifcToCoBieLiteUkExchanger.mapSystem(keyValue.Key, new Xbim.CobieLiteUk.System());
                facility.Systems.Add(mappedSystem);
            }

            var SystemAssignmentSet = ifcStore.Instances.OfType<IIfcRelAssignsToGroup>().Where(r => r.RelatingGroup is IIfcSystem)
                   .Distinct(new IfcRelAssignsToGroupRelatedGroupObjCompare());

            var ret = SystemAssignmentSet.ToDictionary(k => (IIfcSystem)k.RelatingGroup, v => v.RelatedObjects);

            foreach (var systemAssignment in ret)
            {
                var name = systemAssignment.Key.Name;
                Xbim.CobieLiteUk.System foundSystem = facility.Systems.Find(system => system.Name.Equals(name));
                if (foundSystem != null)
                {
                    IItemSet<IIfcObjectDefinition> components = systemAssignment.Value;

                    foreach (var component in components)
                    {
                        if (!(component is IIfcSystem))
                        {
                            var assetKey = new AssetKey();
                            assetKey.Name = component.Name;
                            List<AssetKey> systemComponents = foundSystem.Components != null
                                ? foundSystem.Components
                                : new List<AssetKey>();
                            systemComponents.Add(assetKey);
                            foundSystem.Components = systemComponents;
                        }
                    }
                }
            }
        }

        private void ResolveContacts(Facility facility, string defaultSystem)
        {
            Dictionary<string, string> resolvedEmails = new Dictionary<string, string>();

            var contacts = facility.Get<Contact>();
            foreach (var o in contacts)
            {
                var originEmail = o.Email;
                var resolvedEmail = EmailFallbackGenerator.email(o.Email, o.Company, o.Name);
                resolvedEmails.Add(originEmail, resolvedEmail);

                o.Email = resolvedEmail;
            }

            var types = facility.Get<AssetType>();
            foreach (var type in types)
            {
                if (type.Manufacturer != null && type.Manufacturer.Email != null)
                {
                    string resolvedEmail;
                    resolvedEmails.TryGetValue(type.Manufacturer.Email, out resolvedEmail);
                    type.Manufacturer.Email = resolvedEmail;
                }

                // WARRANTY LABOR

                if (type.Warranty != null && type.Warranty.GuarantorLabor != null && type.Warranty.GuarantorLabor.Email != null)
                {
                    string resolvedEmail;
                    resolvedEmails.TryGetValue(type.Warranty.GuarantorLabor.Email, out resolvedEmail);
                    type.Warranty.GuarantorLabor.Email = resolvedEmail;
                }


                // WARRANTY PARTS

                if (type.Warranty != null && type.Warranty.GuarantorParts != null && type.Warranty.GuarantorParts.Email != null)
                {
                    string resolvedEmail;
                    resolvedEmails.TryGetValue(type.Warranty.GuarantorParts.Email, out resolvedEmail);
                    type.Warranty.GuarantorParts.Email = resolvedEmail;
                }
            }


            var issues = facility.Get<Issue>();
            foreach (var issue in issues)
            {
                if (issue.Owner != null && issue.Owner.Email != null)
                {
                    string resolvedEmail;
                    resolvedEmails.TryGetValue(issue.Owner.Email, out resolvedEmail);
                    issue.Owner.Email = resolvedEmail;
                }
            }

            var spares = facility.Get<Spare>();
            foreach (var spare in spares)
            {
                if (spare.Suppliers != null)
                {
                    foreach (var supplier in spare.Suppliers)
                    {
                        if (supplier != null && supplier.Email != null)
                        {
                            string resolvedEmail;
                            resolvedEmails.TryGetValue(supplier.Email, out resolvedEmail);
                            supplier.Email = resolvedEmail;
                        }
                    }
                }
            }

            var all = facility.Get<CobieObject>();
            foreach (var o in all)
            {
                // set external system from IfcApplication
                o.ExternalSystem = defaultSystem;

                if (o.CreatedBy != null && o.CreatedBy.Email != null)
                {
                    string resolvedEmail;
                    resolvedEmails.TryGetValue(o.CreatedBy.Email, out resolvedEmail);
                    o.CreatedBy.Email = resolvedEmail;
                }
            }

        }

    }
}

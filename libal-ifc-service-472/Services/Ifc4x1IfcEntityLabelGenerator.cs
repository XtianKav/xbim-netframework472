using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.CobieLiteUk;
using Xbim.Ifc;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.UtilityResource;

namespace libal.Services
{
    public class Ifc4x1IfcEntityLabelGenerator
    {
        public static void enrichModelWithIfcEntityLabel(IfcStore ifcStore)
        {
            var allObjects = ifcStore.Instances.OfType<IfcObject>();
            using (var txn = ifcStore.BeginTransaction("IfcObject Modification Ifc2x3"))
            {
                var i = 0;
                foreach (var ifcObject in allObjects)
                {
                    if (string.IsNullOrEmpty(ifcObject.Name))
                        ifcObject.Name = "Unknown" + (++i);

                    var entityLabel = ifcObject.EntityLabel;
                    var pSetRel = ifcStore.Instances.New<IfcRelDefinesByProperties>(r =>
                    {
                        r.RelatingPropertyDefinition = ifcStore.Instances.New<IfcPropertySet>(pSet =>
                        {
                            pSet.Name = "Pset_LibalCommon";
                            pSet.HasProperties.Add(ifcStore.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = "IfcEntityLabel";
                                p.NominalValue = new IfcText("" + entityLabel);
                            }));
                        });
                    });

                    pSetRel.RelatedObjects.Add(ifcObject);
                }
                txn.Commit();
            }
        }

        public static string getExtSystem(IfcStore ifcStore)
        {
            var ifcApplications = ifcStore.Instances.OfType<IfcApplication>();
            var apps = System.Linq.Enumerable.ToArray(ifcApplications);
            return apps != null && apps.Length > 0 ? apps[0].ApplicationFullName : null;
        }

        public static string getPhase(IfcStore ifcStore)
        {
            var ifcProjects = ifcStore.Instances.OfType<IfcProject>();
            var projects = System.Linq.Enumerable.ToArray(ifcProjects);
            return projects != null && projects.Length > 0 ? projects[0].Phase : null;
        }

        internal static string getSiteName(IfcStore ifcStore)
        {
            var ifcSites = ifcStore.Instances.OfType<IfcSite>();
            var sites = System.Linq.Enumerable.ToArray(ifcSites);
            var site = sites != null && sites.Length > 0 ? sites[0] : null;

            return (site != null && site.Name != null)
                ? site.Name
                : (site != null && site.LongName != null)
                ? site.LongName
                : null;
        }

        public static void resolveExtObjectWithPredefinedType(IfcStore ifcStore, Facility facility)
        {
            var globalIdPredefinedType = new Dictionary<string, string>();

            var allObjects = ifcStore.Instances.OfType<IfcObject>();
            foreach (var ifcObject in allObjects)
            {
                var predefinedType = ifcObject.GetType() != null && ifcObject.GetType().GetProperty("PredefinedType") != null ?
                    ifcObject.GetType().GetProperty("PredefinedType").GetValue(ifcObject)
                    : null;

                if (predefinedType != null) {
                    globalIdPredefinedType.Add(ifcObject.GlobalId, predefinedType.ToString());
                }
            }

            var cobieObjects = facility.Get<CobieObject>();
            cobieObjects.ToList().ForEach(cobieObject =>
            {
                if (cobieObject.ExternalId != null && globalIdPredefinedType.ContainsKey(cobieObject.ExternalId)) {
                    cobieObject.ExternalEntity = cobieObject.ExternalEntity + "/" + globalIdPredefinedType[cobieObject.ExternalId];
                }
            });
        }

    }
    
}

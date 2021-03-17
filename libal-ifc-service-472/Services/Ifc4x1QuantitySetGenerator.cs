using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.QuantityResource;

namespace libal.Services
{
    class Ifc4x1QuantitySetGenerator
{
        public static void ResolveQuantitySet(IfcStore ifcStore)
        {
            List<IIfcRelDefinesByProperties> relProps = ifcStore.Instances.OfType<IIfcRelDefinesByProperties>().ToList();

            Dictionary<IfcObject, List<IfcPropertySet>> qSetsByIfcObject = new Dictionary<IfcObject, List<IfcPropertySet>>();

            using (var txn = ifcStore.BeginTransaction("QuantitySet Creation Ifc4x1"))
            { 
                 foreach (IIfcRelDefinesByProperties relProp in relProps)
                 {
                    IEnumerable<IIfcPropertySetDefinition> relatingPropertyDefinitions = relProp.RelatingPropertyDefinition.PropertySetDefinitions
                        .Where(def => def.Name.ToString().Contains("BaseQuantities"));

                    foreach (IIfcPropertySetDefinition relatingPropertyDefinition in relatingPropertyDefinitions)
                    {
                        IItemSet<IIfcObjectDefinition> relatedObjects = relProp.RelatedObjects;
                        IEnumerable<IIfcPropertySetDefinition> definitions = relatingPropertyDefinition.PropertySetDefinitions;

                        foreach (var notAssignedSet in definitions)
                        {
                            if(notAssignedSet is IIfcElementQuantity)
                            {
                                IIfcElementQuantity qset = (IIfcElementQuantity)notAssignedSet;
                                IfcPropertySet pset = ifcStore.Instances.New<IfcPropertySet>(pSet =>
                                {
                                    pSet.Name = qset.Name.ToString().StartsWith("Qto_") 
                                    ? "LIBAL_" + qset.Name.ToString()
                                    : "LIBAL_Qto_" + qset.Name.ToString();

                                    IItemSet<IIfcPhysicalQuantity> quantities = qset.Quantities;
                                    foreach (IIfcPhysicalQuantity quan in quantities)
                                    {
                                        var value = resolveValue(quan);
                                        var unit = resolveUnit(quan);
                                        var name = quan.Name.ToString();

                                        if(value != null)
                                        {
                                            pSet.HasProperties.Add(ifcStore.Instances.New<IfcPropertySingleValue>(p =>
                                            {
                                                p.Name = name;
                                                p.NominalValue = value;
                                                p.Unit = unit;
                                            }));
                                        }
                                    }
                                });

                                foreach (IfcObject ifcObject in relatedObjects)
                                {
                                    if (!qSetsByIfcObject.ContainsKey(ifcObject))
                                    {
                                        qSetsByIfcObject.Add(ifcObject, new List<IfcPropertySet>());
                                    }

                                    List<IfcPropertySet> psets = qSetsByIfcObject[ifcObject];
                                    psets.Add(pset);
                            }
                            }
                        }
                    }
                 }
                txn.Commit();

            }

            using (var txn = ifcStore.BeginTransaction("QuantitySet Reference Creation Ifc4x1"))
            {
                foreach (IfcObject key in qSetsByIfcObject.Keys)
                {
                    List<IfcPropertySet> psets = qSetsByIfcObject[key];

                    foreach (IfcPropertySet pset in psets)
                    {
                        var pSetRel = ifcStore.Instances.New<IfcRelDefinesByProperties>(r =>
                        {
                            r.RelatingPropertyDefinition = pset;
                        });
                        pSetRel.RelatedObjects.Add(key);
                    }

                }

                txn.Commit();

            }
        }

        private static IfcNamedUnit resolveUnit(IIfcPhysicalQuantity quan)
        {
            IEntityCollection ifcModel = quan.Model.Instances;

            if (quan is IfcQuantityLength)
            {
                IfcQuantityLength ifcValue = (IfcQuantityLength)quan;
                return ifcValue.Unit != null
                    ? ifcValue.Unit 
                    : ifcModel.FirstOrDefault<IfcNamedUnit>(ifcNamedUnit => ifcNamedUnit.UnitType.Equals(IfcUnitEnum.LENGTHUNIT));
            }
            else if (quan is IfcQuantityArea)
            {
                IfcQuantityArea ifcValue = (IfcQuantityArea)quan;
                return ifcValue.Unit != null 
                    ? ifcValue.Unit 
                    : ifcModel.FirstOrDefault<IfcNamedUnit>(ifcNamedUnit => ifcNamedUnit.UnitType.Equals(IfcUnitEnum.AREAUNIT));
            }
            else if (quan is IfcQuantityVolume)
            {
                IfcQuantityVolume ifcValue = (IfcQuantityVolume)quan;
                return ifcValue.Unit != null 
                    ? ifcValue.Unit
                    : ifcModel.FirstOrDefault<IfcNamedUnit>(ifcNamedUnit => ifcNamedUnit.UnitType.Equals(IfcUnitEnum.VOLUMEUNIT));
            }
            else if (quan is IfcQuantityTime)
            {
                IfcQuantityTime ifcValue = (IfcQuantityTime)quan;
                return ifcValue.Unit != null 
                    ? ifcValue.Unit
                    : ifcModel.FirstOrDefault<IfcNamedUnit>(ifcNamedUnit => ifcNamedUnit.UnitType.Equals(IfcUnitEnum.TIMEUNIT));
            }
            else if (quan is IfcQuantityWeight)
            {
                IfcQuantityWeight ifcValue = (IfcQuantityWeight)quan;
                return ifcValue.Unit != null 
                    ? ifcValue.Unit
                    : ifcModel.FirstOrDefault<IfcNamedUnit>(ifcNamedUnit => ifcNamedUnit.UnitType.Equals(IfcUnitEnum.MASSUNIT));
            }
            else if (quan is IfcQuantityCount)
            {
                IfcQuantityCount ifcValue = (IfcQuantityCount)quan;
                return ifcValue.Unit;
            }
            else
            {
                return null;
            }
        }

        private static IfcValue resolveValue(IIfcPhysicalQuantity quan)
        {
            if (quan is IfcQuantityLength)
            {
                IfcQuantityLength ifcValue = (IfcQuantityLength)quan;
                return new IfcLengthMeasure(ifcValue.LengthValue.ToString());
            }
            else if (quan is IfcQuantityArea)
            {
                IfcQuantityArea ifcValue = (IfcQuantityArea)quan;
                return new IfcAreaMeasure(ifcValue.AreaValue.ToString());
            }
            else if (quan is IfcQuantityVolume)
            {
                IfcQuantityVolume ifcValue = (IfcQuantityVolume)quan;
                return new IfcVolumeMeasure(ifcValue.VolumeValue.ToString());
            }
            else if (quan is IfcQuantityTime)
            {
                IfcQuantityTime ifcValue = (IfcQuantityTime)quan;
                return new IfcTimeMeasure(ifcValue.TimeValue.ToString());
            }
            else if (quan is IfcQuantityWeight)
            {
                IfcQuantityWeight ifcValue = (IfcQuantityWeight)quan;
                return new IfcMassMeasure(ifcValue.WeightValue);
            }
            else if (quan is IfcQuantityCount)
            {
                IfcQuantityCount ifcValue = (IfcQuantityCount)quan;
                return new IfcCountMeasure(ifcValue.CountValue.ToString());
            } else
            {
                return null;
            }
        }
    }
}

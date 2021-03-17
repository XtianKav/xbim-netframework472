using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;

namespace libal.Services
{
    internal class Ifc4x1UnitResolver
    {
        internal static void ResolveUnits(IfcStore ifcStore, Dictionary<string, string> propertySetNamePropertyNameToUnit)
        {
            IEnumerable<IfcPropertySet> psets = ifcStore.Instances.OfType<IfcPropertySet>();
            foreach (IfcPropertySet pset in psets)
            {
                if(pset != null && pset.Name != null)
                {
                    IItemSet<IfcProperty> properties = pset.HasProperties;
                    foreach (IfcProperty property in properties)
                    {
                        if (property is IIfcPropertySingleValue && property != null && property.Name != null)
                        {
                            IIfcPropertySingleValue propertySingleValue = property as IIfcPropertySingleValue;

                            IfcUnitEnum ifcUnitEnum = resolveUnit(propertySingleValue);
                            if(!ifcUnitEnum.Equals(IfcUnitEnum.USERDEFINED))
                            {
                                var key = pset.Name + "_" + property.Name;
                                if (!propertySetNamePropertyNameToUnit.ContainsKey(key)) {
                                    IfcSIUnit ifcSIUnit = ifcStore.FederatedInstances
                                        .Where<IfcSIUnit>(a => a.UnitType.ToString().Equals(ifcUnitEnum.ToString()))
                                        .FirstOrDefault<IfcSIUnit>();

                                    if (ifcSIUnit != null && ifcSIUnit.Symbol != null)
                                    {
                                        var value = ifcSIUnit.UnitType.ToString() + "|" + ifcSIUnit.Symbol.ToString();
                                        propertySetNamePropertyNameToUnit.Add(key, value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static IfcUnitEnum resolveUnit(IIfcPropertySingleValue property)
        {
            var expressValueType = (IExpressValueType)property.NominalValue;

            IfcUnitEnum ifcUnitEnum = IfcUnitEnum.USERDEFINED;
            if (expressValueType is IfcAmountOfSubstanceMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.AMOUNTOFSUBSTANCEUNIT;
            } else if(expressValueType is IfcAreaMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.AREAUNIT;
            }
            else if (expressValueType is IfcElectricCurrentMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.ELECTRICCURRENTUNIT;
            }
            else if (expressValueType is IfcLengthMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.LENGTHUNIT;
            }
            else if (expressValueType is IfcLuminousIntensityMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.LUMINOUSINTENSITYUNIT;
            }
            else if (expressValueType is IfcMassMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.MASSUNIT;
            }
            else if (expressValueType is IfcPlaneAngleMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.PLANEANGLEUNIT;
            }
            else if (expressValueType is IfcPositiveLengthMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.LENGTHUNIT;
            }
            else if (expressValueType is IfcPositivePlaneAngleMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.PLANEANGLEUNIT;
            }
            else if (expressValueType is IfcSolidAngleMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.SOLIDANGLEUNIT;
            }
            else if (expressValueType is IfcThermodynamicTemperatureMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT;
            }
            else if (expressValueType is IfcTimeMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.TIMEUNIT;
            }
            else if (expressValueType is IfcVolumeMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.VOLUMEUNIT;
            }
            else if (expressValueType is IfcAbsorbedDoseMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.ABSORBEDDOSEUNIT;
            }
            else if (expressValueType is IfcDoseEquivalentMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.DOSEEQUIVALENTUNIT;
            }
            else if (expressValueType is IfcElectricCapacitanceMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.ELECTRICCAPACITANCEUNIT;
            }
            else if (expressValueType is IfcElectricChargeMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.ELECTRICCHARGEUNIT;
            }
            else if (expressValueType is IfcElectricConductanceMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.ELECTRICCONDUCTANCEUNIT;
            }
            else if (expressValueType is IfcElectricResistanceMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.ELECTRICRESISTANCEUNIT;
            }
            else if (expressValueType is IfcElectricVoltageMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.ELECTRICVOLTAGEUNIT;
            }
            else if (expressValueType is IfcForceMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.FORCEUNIT;
            } else if (expressValueType is IfcFrequencyMeasure) {
                ifcUnitEnum = IfcUnitEnum.FREQUENCYUNIT;        
            }
             else if (expressValueType is IfcIlluminanceMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.ILLUMINANCEUNIT;
            }
            else if (expressValueType is IfcInductanceMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.INDUCTANCEUNIT;
            }
            else if (expressValueType is IfcLuminousFluxMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.LUMINOUSFLUXUNIT;
            }
            else if (expressValueType is IfcMagneticFluxDensityMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.MAGNETICFLUXDENSITYUNIT;
            }
            else if (expressValueType is IfcMagneticFluxMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.MAGNETICFLUXUNIT;
            }
            else if (expressValueType is IfcPowerMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.POWERUNIT;
            }
            else if (expressValueType is IfcPressureMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.PRESSUREUNIT;
            }
            else if (expressValueType is IfcRadioActivityMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.RADIOACTIVITYUNIT;
            }
            else if (expressValueType is IfcNonNegativeLengthMeasure)
            {
                ifcUnitEnum = IfcUnitEnum.LENGTHUNIT;
            }

            return ifcUnitEnum;

        }
    }
}
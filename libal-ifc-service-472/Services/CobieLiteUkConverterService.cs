using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xbim.CobieLiteUk;
using Xbim.Ifc;
using XbimExchanger.IfcHelpers;
using XbimExchanger.IfcToCOBieLiteUK;

namespace libal.Services
{
    public class CobieLiteUkConverterService
    {
        public static async Task<Stream> ConvertAsync(Stream ifcStream)
        {
            var ifcStoreGenerator = new IfcStoreGenerator(ifcStream);
            using (var ifcStore = await ifcStoreGenerator.GetIfcStoreAsync())
            {
                return await ConvertIfcToCobieLiteUkAsync(ifcStore);
            }
        }

        private static Task<Stream> ConvertIfcToCobieLiteUkAsync(IfcStore ifcStore)
        {
            return Task.Run<Stream>(() =>
            {

                if (ifcStore.SchemaVersion == Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3)
                {
                    Ifc2x3IfcEntityLabelGenerator.enrichModelWithIfcEntityLabel(ifcStore);
                } 
                else {
                    Ifc4x1IfcEntityLabelGenerator.enrichModelWithIfcEntityLabel(ifcStore);
                }

                var memStream = new MemoryStream();

                var facilities = new List<Facility>();
                var ifcToCoBieLiteUkExchanger = new IfcToCOBieLiteUkExchanger(ifcStore, facilities, null, null, null, EntityIdentifierMode.GloballyUniqueIds, SystemExtractionMode.System);
                facilities = ifcToCoBieLiteUkExchanger.Convert();

                var facility = facilities.ToArray()[0];

                facility.WriteXml(memStream);

                return new MemoryStream(memStream.ToArray());
            });
        }

    }
}

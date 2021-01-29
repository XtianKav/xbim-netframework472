using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Http;
using Xbim.CobieLiteUk;
using Xbim.Ifc;
using XbimExchanger.IfcHelpers;
using XbimExchanger.IfcToCOBieLiteUK;

namespace libal_ifc_service_472
{
    public class IfcController : ApiController
    {
        // GET api/ifc
        public IEnumerable<string> Get()
        {

            var file = @"C:\Users\shirs\OneDrive\Dokumente\libal\cobies\ALC_ARC 210127.ifc";
            using (var stepModel = IfcStore.Open(file))
            {
                var facilities = new List<Facility>();
                var ifcToCoBieLiteUkExchanger = new IfcToCOBieLiteUkExchanger(stepModel, facilities, null, null, null, EntityIdentifierMode.GloballyUniqueIds);
                facilities = ifcToCoBieLiteUkExchanger.Convert();

                var facility = facilities.ToArray()[0];

                facility.WriteXml(@"C:\Users\shirs\OneDrive\Dokumente\libal\cobies\ALC_ARC 210127.xml");

                var wexBimFilename = Path.ChangeExtension(file, "wexBIM");
                using (var wexBiMfile = File.Create(wexBimFilename))
                {
                    using (var wexBimBinaryWriter = new BinaryWriter(wexBiMfile))
                    {
                        stepModel.SaveAsWexBim(wexBimBinaryWriter);
                        wexBimBinaryWriter.Close();
                    }
                    wexBiMfile.Close();
                }
            }

            return new string[] { "value1", "value2" };
        }

    }
}
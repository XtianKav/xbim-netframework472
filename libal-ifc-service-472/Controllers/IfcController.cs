using System;
using System.Collections.Generic;
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

            var file = "SampleHouse.ifc";
            using (var stepModel = IfcStore.Open(file))
            {
                var facilities = new List<Facility>();
                var ifcToCoBieLiteUkExchanger = new IfcToCOBieLiteUkExchanger(stepModel, facilities, null, null, null, EntityIdentifierMode.GloballyUniqueIds);
                facilities = ifcToCoBieLiteUkExchanger.Convert();

                var facility = facilities.ToArray()[0];

                facility.WriteXml("SampleHouse.xml");
            }

            return new string[] { "value1", "value2" };
        }

    }
}
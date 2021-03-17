using libal.Domain;
using libal.Services;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace libal_ifc_service_472
{
    public class IfcController : ApiController
    {

        ICobieLiteUkAsyncConverterService _cobieLiteUkAsyncConverterService = new CobieLiteUkAsyncConverterService();

        WexbimConverterService _wexbimConverterService = new WexbimConverterService();

        CobieConverterService _cobieConverterService = new CobieConverterService();

        [ActionName("isAlive")]
        [HttpGet]
        public string IsAlive()
        {
            return "© LIBAL Deutschland GmbH - LIBAL IFC SERVICE";
        }

        [ActionName("IfcToCobie")]
        [HttpPost]
        public async Task<OperationResult> ConvertFromIfcCobie(string uuid)
        {
            var source = uuid + ".ifc";
            var destination = Guid.NewGuid() + ".xlsx";
            var stream = _cobieConverterService.ConvertAsync(source, destination);

            var operationResult = new OperationResult();
            operationResult.fileName = destination;
            return operationResult;
        }

        [ActionName("IfcToWexbim")]
        [HttpPost]
        public async Task<OperationResult> IfcToWexbim(string uuid)
        {
            var source = uuid + ".ifc";
            var destination = Guid.NewGuid() + ".wexbim";
            var stream = _wexbimConverterService.ConvertAsync(source, destination);

            var operationResult = new OperationResult();
            operationResult.fileName = destination;

            return operationResult;
        }

        [ActionName("IfcToCobieLiteUk")]
        [HttpPost]
        public async Task<OperationResult> ConvertFromIfcToCobieLiteUk(string uuid)
        {
            var source = uuid + ".ifc";
            var destination = Guid.NewGuid() + ".xml";
            var stream = _cobieLiteUkAsyncConverterService.ConvertAsync(source, destination);

            var operationResult = new OperationResult();
            operationResult.fileName = destination;

            return operationResult;
        }

    }

}
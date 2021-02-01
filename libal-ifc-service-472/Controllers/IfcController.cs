using libal.Services;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;

namespace libal_ifc_service_472
{
    public class IfcController : ApiController
    {
        // POST api/ifc
        public async Task<Stream> PostAsync()
        {
            var ifcStream = await Request.Content.ReadAsStreamAsync();
            var wexbimStream = await WexbimConverterService.ConvertAsync(ifcStream);

            return wexbimStream;
        }
    }

}
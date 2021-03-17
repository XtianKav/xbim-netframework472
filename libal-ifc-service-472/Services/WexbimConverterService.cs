using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc;
using Xbim.ModelGeometry.Scene;

namespace libal.Services
{
    public class WexbimConverterService
    {

        IS3Service _s3 = new S3Service();

        public async Task<String> ConvertAsync(String source, string destination)
        {
            var content = await _s3.Get(source);
            using (var ifcStream = new MemoryStream(content))
            {
                var ifcStoreGenerator = new IfcStoreGenerator(ifcStream);
                using (var ifcStore = await ifcStoreGenerator.GetIfcStoreAsync())
                {
                    var wexBimStream = await ConvertIfcToWexBimAsync(ifcStore, destination);
                    return wexBimStream;
                }
            }
        }

        private Task<String> ConvertIfcToWexBimAsync(IfcStore ifcStore, string fileName)
        {
            return Task.Run<String>(() =>
            {
                var context = new Xbim3DModelContext(ifcStore);
                context.CreateContext();
                var memStream = new MemoryStream();
                using (var wexBimBinaryWriter = new BinaryWriter(memStream, Encoding.Default, true))
                {
                    ifcStore.SaveAsWexBim(wexBimBinaryWriter);
                }
                _s3.Create(fileName, new MemoryStream(memStream.ToArray()));

                return "processed";
            });
        }
    }
}

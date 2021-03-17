using libal.Domain;
using System.IO;
using System.Threading.Tasks;

namespace libal.Services
{
    public interface IS3Service
    {
        Task<byte[]> Get(string fileName);

        Task<OperationResult> Create(string fileName, Stream stream);

    }
}
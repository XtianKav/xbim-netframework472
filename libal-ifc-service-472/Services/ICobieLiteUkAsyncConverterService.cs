
using System;
using System.IO;
using System.Threading.Tasks;

namespace libal.Services
{
    public interface ICobieLiteUkAsyncConverterService
    {
        Task<String> ConvertAsync(String source, string destination);
    }

}
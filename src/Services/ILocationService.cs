using System.Threading.Tasks;

namespace TemperatureHistogramChallenge.Services
{
    public interface ILocationService
    {
        Task<string> Run(string ipAddress);
    }
}
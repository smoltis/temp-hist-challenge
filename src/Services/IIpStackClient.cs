using System;
using System.Runtime.InteropServices;
using IpStack.Models;

namespace TemperatureHistogramChallenge.Services
{
    public interface IIpStackClient
    {
        IpAddressDetails GetIpAddressDetails(string ipAddress, [Optional] string fields, [Optional] bool? hostname, [Optional] bool? security, [Optional] string language, [Optional] string callback);
    }
}

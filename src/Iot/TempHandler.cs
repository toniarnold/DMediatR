using Iot.Device.CpuTemperature;

namespace Iot
{
    [Local("TempHandler")]
    public class TempHandler : IRequestHandler<TempRequest, double>
    {
        public virtual async Task<double> Handle(TempRequest request, CancellationToken cancellationToken)
        {
            using var cpuTemperature = new CpuTemperature();
            if (!cpuTemperature.IsAvailable)
            {
                throw new Exception("Iot.Device.CpuTemperature is not available on this device");
            }
            var temperature = cpuTemperature.ReadTemperatures().FirstOrDefault();
            if (double.IsNaN(temperature.Temperature.DegreesCelsius))
            {
                throw new Exception("Temperature read is not numeric");
            }
            return await Task.FromResult(temperature.Temperature.DegreesCelsius);
        }
    }
}
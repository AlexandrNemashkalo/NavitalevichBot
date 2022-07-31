using System.Net.NetworkInformation;

namespace NavitalevichBot.Helpers;

public static class EnvironmentHelper
{

    public static bool VPNIsOn()
    {
        return NetworkInterface.GetIsNetworkAvailable()
                && NetworkInterface.GetAllNetworkInterfaces()
                                   .FirstOrDefault(ni => ni.Description.Contains("Windscribe"))
                                   ?.OperationalStatus == OperationalStatus.Up;
    }

    public static bool IsLocal()
    {
        var path = Directory.GetCurrentDirectory();
        return path == "C:\\Git\\NavitalevichBot\\NavitalevichBot\\bin\\Debug\\net6.0";
    }
}

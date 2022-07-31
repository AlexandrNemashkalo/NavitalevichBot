using NavitalevichBot.Helpers;
using NavitalevichBot.Models;
using System.Net;

namespace NavitalevichBot.Services;

public class ProxyManager
{
    private static string GermanProxy = "http://167.86.80.102:3128";
    public WebProxy CurrentProxy { get; private set; }

    public ProxyManager()
    {
        SetProxy(GermanProxy);
    }

    public WebProxy GetProxy()
    {
        if (EnvironmentHelper.VPNIsOn())
        {
            return null;
        };

        if (CurrentProxy == null)
        {
            return null;
        }

        return CurrentProxy;
    }


    public void SetProxy(string address)
    {
        if(address == null)
        {
            CurrentProxy = null;
        }
        else if (CurrentProxy == null)
        {
            CurrentProxy = new WebProxy(address);
            CurrentProxy.BypassProxyOnLocal = false;
        }
        else
        {
            CurrentProxy.Address = new Uri(address);
        }
    }
}

using System;
using Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using GHI.Networking;
using Microsoft.SPOT;
using Gadgeteer.Networking;

namespace JenkinsGadget
{
    public class WiFiHelper
    {
        private WiFiRS21 wifiModule;

        public WiFiHelper(WiFiRS21 wifiModule)
        {
            this.wifiModule = wifiModule;
        }

        public bool Start(string ssid, string networkKey)
        {
            if (wifiModule.NetworkInterface.Opened)
            {
                wifiModule.NetworkInterface.Close();
            }

            if (!wifiModule.NetworkInterface.Opened)
            {
                wifiModule.NetworkInterface.Open();
            }

            if (!wifiModule.NetworkInterface.IsDhcpEnabled)
            {
               wifiModule.NetworkInterface.EnableDhcp();
            }

            if (!wifiModule.NetworkInterface.LinkConnected)
            {
                WiFiRS9110.NetworkParameters[] response = wifiModule.NetworkInterface.Scan(ssid);
                wifiModule.NetworkInterface.Join(response[0].Ssid, networkKey);
            }
            return wifiModule.IsNetworkConnected;
        }

        public void Stop()
        {
            if (wifiModule.IsNetworkConnected)
            {
                wifiModule.NetworkInterface.Close();
            }
        }
    }
}

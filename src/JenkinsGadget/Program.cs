using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace JenkinsGadget
{
    public enum JenkinsState
    {
        Unknown,
        Blue,
        Red,
        Yellow,
        Blue_Building,
        Red_Building,
        Yellow_Building,
    }

    public partial class Program
    {
        private JenkinsState currentState = JenkinsState.Red;
        private WiFiHelper helper;
        private bool networkingOk = false;
        GT.Timer timer = new GT.Timer(15000);
        string jenkinsUrl = "";
        private string ssid = "";
        private string networkKey = "";
        private string jenkinsUserName = "";
        private string jenkinsPassword = "";

   
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            /*******************************************************************************************
            Modules added in the Program.gadgeteer designer view are used by typing 
            their name followed by a period, e.g.  button.  or  camera.
            
            Many modules generate useful events. Type +=<tab><tab> to add a handler to an event, e.g.:
                button.ButtonPressed +=<tab><tab>
            
            If you want to do something periodically, use a GT.Timer and handle its Tick event, e.g.:
                GT.Timer timer = new GT.Timer(1000); // every second (1000ms)
                timer.Tick +=<tab><tab>
                timer.Start();
            *******************************************************************************************/


            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");
            helper = new WiFiHelper(this.wifiRS21);
            this.button.ButtonPressed += button_ButtonPressed;
            wifiRS21.NetworkUp += wifiModule_NetworkUp;
            timer.Tick += timer_Tick;

        }

        void timer_Tick(GT.Timer timer)
        {
            if (networkingOk)
                WatchJenkins();
        }

        private void wifiModule_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            if (state == GTM.Module.NetworkModule.NetworkState.Up)
            {
                string address = wifiRS21.NetworkInterface.IPAddress;
                Debug.Print("IP Address :" + address);
                this.multicolorLED.BlinkOnce(GT.Color.White);
                networkingOk = true;
                timer.Start();
            }
        }

        private void button_ButtonPressed(Button sender, Button.ButtonState state)
        {
            var started = helper.Start(ssid, networkKey);
            Debug.Print("Wifi Started : " + started);
        }

        private void WatchJenkins()
        {
            string content;
            try
            {
                using (HttpWebRequest request = (HttpWebRequest)WebRequest.Create(jenkinsUrl))
                {
                    request.Credentials = new NetworkCredential(jenkinsUserName, jenkinsPassword);
                    using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            content = reader.ReadToEnd();
                            reader.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
                return;
            }
            if (content == null || content == "") return;

            JenkinsState state = GetCurrentState(content);

            if (currentState == state)
                return;

            if (state == JenkinsState.Blue)
            {
                this.multicolorLED.TurnBlue();
            }
            if (state == JenkinsState.Blue_Building)
            {
                this.multicolorLED.BlinkRepeatedly(GT.Color.Blue);
            }
            if (state == JenkinsState.Red)
            {
                this.multicolorLED.TurnRed();
            }
            if (state == JenkinsState.Red_Building)
            {
                this.multicolorLED.BlinkRepeatedly(GT.Color.Red);
            }
            if (state == JenkinsState.Yellow)
            {
                this.multicolorLED.TurnColor(GT.Color.Yellow);
            }
            if (state == JenkinsState.Yellow_Building)
            {
                this.multicolorLED.BlinkRepeatedly(GT.Color.Yellow);
            }
            currentState = state;
        }

        private JenkinsState GetCurrentState(string content)
        {
            if (content.IndexOf(">blue<", 1) != -1)
            {
                return JenkinsState.Blue;
            }
            if (content.IndexOf(">blue_anime<", 1) != -1)
            {
                return JenkinsState.Blue_Building;
            }
            if (content.IndexOf(">red<", 1) != -1)
            {
                return JenkinsState.Red;
            }
            if (content.IndexOf(">red_anime<", 1) != -1)
            {
                return JenkinsState.Red_Building;
            }
            if (content.IndexOf(">yellow<", 1) != -1)
            {
                return JenkinsState.Yellow;
            }
            if (content.IndexOf(">yellow_anime<", 1) != -1)
            {
                return JenkinsState.Yellow_Building;
            }
            return JenkinsState.Red;
        }
    }
}

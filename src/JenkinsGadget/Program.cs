using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Threading;
using Gadgeteer.SocketInterfaces;
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
        string jenkinsUrl = "myjenkinsurl";
        private string ssid = "mynetworkssid";
        private string networkKey = "myhousekey";
        private string jenkinsUserName = "myusername";
        private string jenkinsPassword = "mypassword";

        private JenkinsState currentState = JenkinsState.Red;
        private WiFiHelper helper;
        private bool networkingOk = false;
        GT.Timer timer = new GT.Timer(10000);
        private DigitalOutput green;
        private DigitalOutput red;
        private DigitalOutput yellow;
        bool blink = false;
        DigitalOutput currentColor;
        GT.Timer lightControl = new GT.Timer(2000);
   
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            Debug.Print("Program Started");
            green = extender.CreateDigitalOutput(GT.Socket.Pin.Nine, false);
            red = extender.CreateDigitalOutput(GT.Socket.Pin.Eight, false);
            yellow = extender.CreateDigitalOutput(GT.Socket.Pin.Seven, false);
            currentColor = green;
            helper = new WiFiHelper(this.wifiRS21);
            this.button.ButtonPressed += button_ButtonPressed;
            wifiRS21.NetworkUp += wifiModule_NetworkUp;
            timer.Tick += timer_Tick;
            lightControl.Tick += lightControl_Tick;
        }

        void lightControl_Tick(GT.Timer timer)
        {
            green.Write(false);
            red.Write(false);
            yellow.Write(false);

            if (blink)
            {
                currentColor.Write(true);
                Thread.Sleep(1000);
                currentColor.Write(false);
            }
            else
            {
                currentColor.Write(true);
            }
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
                lightControl.Start();
            }
        }

        private void button_ButtonPressed(Button sender, Button.ButtonState state)
        {
            GT.Timer scanning = new GT.Timer(1000);
            scanning.Tick += scanning_Tick;
            scanning.Start();

            var started = helper.Start(ssid, networkKey);
            Debug.Print("Wifi Started : " + started);
            scanning.Stop();
            green.Write(false);
            red.Write(false);
            yellow.Write(false);
        }

        void scanning_Tick(GT.Timer timer)
        {
            green.Write(true);
            Thread.Sleep(100);
            red.Write(true);
            Thread.Sleep(100);
            yellow.Write(true);
            Thread.Sleep(100);
            green.Write(false);
            Thread.Sleep(100);
            red.Write(false);
            Thread.Sleep(100);
            yellow.Write(false);
            Thread.Sleep(100);
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

            if (state == JenkinsState.Blue)
            {
                currentColor = green;
                blink = false;
                this.multicolorLED.TurnBlue();
            }
            if (state == JenkinsState.Blue_Building)
            {
                currentColor = green;
                blink = true;
                this.multicolorLED.BlinkRepeatedly(GT.Color.Blue);
            }
            if (state == JenkinsState.Red)
            {
                currentColor = red;
                blink = false;
                this.multicolorLED.TurnRed();
            }
            if (state == JenkinsState.Red_Building)
            {
                currentColor = red;
                blink = true;
                this.multicolorLED.BlinkRepeatedly(GT.Color.Red);
            }
            if (state == JenkinsState.Yellow)
            {
                currentColor = yellow;
                blink = false;
                this.multicolorLED.TurnColor(GT.Color.Yellow);
            }
            if (state == JenkinsState.Yellow_Building)
            {
                currentColor = yellow;
                blink = true;
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

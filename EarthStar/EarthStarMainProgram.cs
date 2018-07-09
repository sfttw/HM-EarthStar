using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EarthStar
{
    static class EarthStarMainProgram
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new EarthStarForm());
        }

        public static string GetHostName(string ipAddress)
        {
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(ipAddress);
                if (entry != null)
                {
                    string hostName = entry.HostName;
                    string[] split = hostName.Split('.');

                    return split[0];
                }
            }
            catch (SocketException ex)
            {
                //unknown host or
                //not every IP has a name
                //log exception (manage it)
            }

            return null;
        }

        public static IPAddress GetBroadcastAddress(this IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }

        public static class IPMacMapper
        {
            private static List<IPAndMac> list;

            private static StreamReader ExecuteCommandLine(String file, String arguments = "")
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.FileName = file;
                startInfo.Arguments = arguments;

                Process process = Process.Start(startInfo);

                return process.StandardOutput;
            }

            private static void InitializeGetIPsAndMac()
            {
                if (list != null)
                    return;

                var arpStream = ExecuteCommandLine("arp", "-a");
                List<string> result = new List<string>();
                while (!arpStream.EndOfStream)
                {
                    var line = arpStream.ReadLine().Trim();
                    result.Add(line);
                }

                list = result.Where(x => !string.IsNullOrEmpty(x) && (x.Contains("dynamic") || x.Contains("static")))
                    .Select(x =>
                    {
                        string[] parts = x.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        return new IPAndMac { IP = parts[0].Trim(), MAC = parts[1].Trim() };
                    }).ToList();
            }

            public static string FindIPFromMacAddress(string macAddress)
            {
                InitializeGetIPsAndMac();
                return list.SingleOrDefault(x => x.MAC == macAddress).IP;
            }

            public static string FindMacFromIPAddress(string ip)
            {
                InitializeGetIPsAndMac();
                return list.SingleOrDefault(x => x.IP == ip).MAC;
            }

            private class IPAndMac
            {
                public string IP { get; set; }
                public string MAC { get; set; }
            }
        }

        public static class WakeOnLan
        {
            public static void WakeUp(string macAddress, string ipAddress, string subnetMask)
            {
                UdpClient client = new UdpClient();

                Byte[] datagram = new byte[102];

                for (int i = 0; i <= 5; i++)
                {
                    datagram[i] = 0xff;
                }

                string[] macDigits = null;
                if (macAddress.Contains("-"))
                {
                    macDigits = macAddress.Split('-');
                }
                else
                {
                    macDigits = macAddress.Split(':');
                }

                if (macDigits.Length != 6)
                {
                    throw new ArgumentException("Incorrect MAC address supplied!");
                }

                int start = 6;
                for (int i = 0; i < 16; i++)
                {
                    for (int x = 0; x < 6; x++)
                    {
                        datagram[start + i * 6 + x] = (byte)Convert.ToInt32(macDigits[x], 16);
                    }
                }

                IPAddress address = IPAddress.Parse(ipAddress);
                IPAddress mask = IPAddress.Parse(subnetMask);
                IPAddress broadcastAddress = GetBroadcastAddress(address, mask);

                client.Send(datagram, datagram.Length, broadcastAddress.ToString(), 3);
            }
        }
    }
}

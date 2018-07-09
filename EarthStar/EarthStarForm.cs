using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static EarthStar.EarthStarMainProgram;

namespace EarthStar
{
    public partial class EarthStarForm : Form
    {
        private List<string> macAddress;
        private string macListLocation = "macList.txt";

        public EarthStarForm()
        {
            macAddress = new List<string>();
            readMacList();

            InitializeComponent();

            foreach (string currentMAC in macAddress)
            {
                // Get our IP
                try
                {
                    string currentIP = IPMacMapper.FindIPFromMacAddress(currentMAC);

                    if (currentIP.Length > 0)
                    {
                        // Get the hostName
                        string hostName = GetHostName(currentIP);

                        if (hostName != null)
                            lb_items.Items.Add(hostName + " (" + currentIP + ")");
                        else
                            lb_items.Items.Add(currentIP);
                    }
                        
                }
                catch
                {
                    // MAC is not on the network
                }
            }

            //WakeOnLan.WakeUp("30-9C-23-8A-23-C2", "192.168.0.13", "255.255.255.255");
            //Debug.Write(IPMacMapper.FindIPFromMacAddress("30-9C-23-8A-23-C2"));
        }

        private void readMacList()
        {
            string[] readLines = File.ReadAllLines(macListLocation);

            for (int i = 0; i < readLines.Length; i++)
            {
                macAddress.Add(readLines[i].ToLower());
            }
        }

        private void btn_wakeup_Click(object sender, EventArgs e)
        {
            foreach (var item in lb_items.SelectedItems)
            {
                try
                {
                    string[] split = item.ToString().Split(' ');

                    string ipAddress;

                    if (split.Length > 1)
                    {
                        ipAddress = split[1].Replace("(", "").Replace(")", "");
                    }
                    else
                    {
                        ipAddress = split[0];
                    }

                    string macAddress = IPMacMapper.FindMacFromIPAddress(ipAddress);

                    WakeOnLan.WakeUp(macAddress, ipAddress, "255.255.255.255");
                }
                catch
                {
                    // Failed to find IP
                }
            }
        }
    }
}

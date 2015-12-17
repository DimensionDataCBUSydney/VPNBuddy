﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace VPNBuddy
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                Write(string.Empty, true);
                var configDoc = XDocument.Load("config.xml");
                var doc = configDoc.Descendants("vpn");
                GetVpnDetails(doc);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        private void Write(string text, bool clear = false)
        {
            if (clear)
            {
                textBox1.Clear();
            }
            if (!string.IsNullOrEmpty(text))
            {
                textBox1.AppendText(string.Format("{0}{1}", text, Environment.NewLine));
            }
        }

        private void Write(Collection<PSObject> psOutput)
        {
            if (psOutput != null && psOutput.Count > 0)
            {
                foreach (var outputItem in psOutput)
                {
                    if (outputItem != null)
                    {
                        textBox1.AppendText(string.Format("[{0}] {1}{2}", outputItem.BaseObject.GetType().FullName,
                            outputItem.BaseObject, Environment.NewLine));
                    }
                }
            }
        }


        private void Write(Collection<ErrorRecord> errorOuput)
        {
            if (errorOuput != null && errorOuput.Count > 0)
            {
                foreach (var errorItem in errorOuput)
                {
                    if (errorItem != null)
                    {
                        textBox1.AppendText(string.Format("[ERROR] {0}{1}", errorItem, Environment.NewLine));
                    }
                }
            }
        }

        private void Write(Exception ex)
        {
            textBox1.Clear();
            textBox1.Text = ex.ToString();
        }

        private void GetVpnDetails(IEnumerable<XElement> vpnList)
        {
            foreach (var vpn in vpnList)
            {
                var data = new VpnData();
                data.Name = vpn.Attribute("name").Value;
                data.HostName = vpn.Attribute("vpnhost").Value;
                data.Username = vpn.Attribute("username").Value;
                data.Password = Base64Decode(vpn.Attribute("password").Value);

                listBox1.Items.Add(data);
            }
            listBox1.DisplayMember = "Display";

            Write(string.Format("{0} vpn details found.", listBox1.Items.Count));
        }

        private void ConnectVpn2(VpnData data)
        {
            try
            {
                using (var psInstance = PowerShell.Create())
                {
                    psInstance.AddCommand("Set-ExecutionPolicy")
                        .AddArgument("Unrestricted")
                        .AddParameter("Scope", "CurrentUser");
                    var psOutput = psInstance.Invoke();
                    var errorOuput = psInstance.Streams.Error.ReadAll();
                    Write(errorOuput);
                    Write(psOutput);

                    var script = string.Format("./vpnConnect.ps1 -VPNHost \"{0}\" -username \"{1}\" -password \"{2}\"",
                        data.HostName, data.Username, data.Password);
                    psInstance.AddScript(script);

                    psOutput = psInstance.Invoke();
                    errorOuput = psInstance.Streams.Error.ReadAll();
                    Write(errorOuput);
                    Write(psOutput);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var data = listBox1.SelectedItem as VpnData;
            ConnectVpn2(data);
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            button1_Click(sender, e);
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadConfig();
        }

        private void editConfigxmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("config.xml");
        }

        private void editConnectionButton_Click(object sender, EventArgs e)
        {
            // Show the edit form
            var data = listBox1.SelectedItem as VpnData;
            EditConnectionForm editForm = new EditConnectionForm();
            editForm.VpnData = data;
            editForm.ShowDialog();

            // Replace the item with the new connection
            int index = listBox1.SelectedIndex;
            listBox1.Items.RemoveAt(index);
            listBox1.Items.Insert(index, editForm.VpnData);
            Save();
        }

        private void removeConnectionButton_Click(object sender, EventArgs e)
        {
            listBox1.Items.Remove(listBox1.SelectedItem as VpnData);
            listBox1.Refresh();
            Save();
        }

        private void Save()
        {
            XDocument configDoc = new XDocument();
            var rootEl = new XElement("vpnlist");
            configDoc.Add(rootEl);
            List<VpnData> vpns = (from object item in listBox1.Items select item as VpnData).ToList();
            foreach (VpnData vpn in vpns)
            {
                rootEl.Add(new XElement("vpn",
                    new XAttribute("name", vpn.Name),
                    new XAttribute("vpnhost", vpn.HostName),
                    new XAttribute("username", vpn.Username),
                    new XAttribute("password", Base64Encode(vpn.Password))));
            }
            configDoc.Save("Config.xml", SaveOptions.None);
        }
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        private void addConnectionButton_Click(object sender, EventArgs e)
        {
            // Show the edit form
            AddConnectionForm addForm = new AddConnectionForm();
            addForm.ShowDialog();

            // Replace the item with the new connection
            listBox1.Items.Add(addForm.VpnData);
            Save();
        }
    }
}
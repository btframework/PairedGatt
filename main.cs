using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using wclCommon;
using wclCommunication;
using wclBluetooth;

namespace PairedGatt
{
    public partial class fmMain : Form
    {
        private wclBluetoothManager FManager;
        private wclGattClient FClient;

        public fmMain()
        {
            InitializeComponent();
        }

        private void btConnect_Click(object sender, EventArgs e)
        {
            lbLog.Items.Clear();

            Int32 Res = FManager.Open();
            if (Res != wclErrors.WCL_E_SUCCESS)
                lbLog.Items.Add("Bluetooth Manager Open Failed: 0x" + Res.ToString("X8"));
        }

        private void fmMain_Load(object sender, EventArgs e)
        {
            FManager = new wclBluetoothManager();
            FManager.AfterOpen += FManager_AfterOpen;
            FManager.BeforeClose += FManager_BeforeClose;

            FClient = new wclGattClient();
            FClient.ConnectOnRead = true;
            FClient.OnConnect += FClient_OnConnect;
            FClient.OnDisconnect += FClient_OnDisconnect;
        }

        private void FManager_BeforeClose(object sender, EventArgs e)
        {
            lbLog.Items.Add("Bluetooth manager has been closed.");
        }

        private void FClient_OnDisconnect(object Sender, int Reason)
        {
            lbLog.Items.Add("GATT client disconnected");
        }

        private void FClient_OnConnect(object Sender, int Error)
        {
            if (Error != wclErrors.WCL_E_SUCCESS)
            {
                lbLog.Items.Add("Connect failed: 0x" + Error.ToString("X8"));
                FManager.Close();
            }
            else
            {
                wclGattService[] Services;
                Int32 Res = FClient.ReadServices(wclGattOperationFlag.goReadFromCache, out Services);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    lbLog.Items.Add("Read services failed: 0x" + Res.ToString("X8"));
                else
                {
                    foreach (wclGattService s in Services)
                        lbLog.Items.Add(s.ToString());

                    FClient.Disconnect();
                }
                FManager.Close();
            }
        }

        private void FManager_AfterOpen(object sender, EventArgs e)
        {
            lbLog.Items.Add("Bluetooth Manager has been opened");

            if (FManager.Count == 0)
            {
                lbLog.Items.Add("No Bluetooth radio found");
                FManager.Close();
            }
            else
            {
                wclBluetoothRadio Radio = FManager[0];
                lbLog.Items.Add("Found " + Radio.ApiName + " Bluetooth radio");

                Int64[] Devices;
                Int32 Res = Radio.EnumPairedDevices(out Devices);
                if (Res != wclErrors.WCL_E_SUCCESS)
                    lbLog.Items.Add("Enumerate paired devices failed: 0x" + Res.ToString("X8"));
                else
                {
                    if (Devices == null || Devices.Length == 0)
                    {
                        lbLog.Items.Add("Not paired devices found");
                        FManager.Close();
                    }
                    else
                    {
                        Int64 Address = 0;
                        foreach (Int64 a in Devices)
                        {
                            wclBluetoothDeviceType Type;
                            Res = Radio.GetRemoteDeviceType(a, out Type);
                            if (Res == wclErrors.WCL_E_SUCCESS && Type == wclBluetoothDeviceType.dtBle)
                            {
                                Address = a;
                                break;
                            }
                        }

                        if (Address == 0)
                        {
                            lbLog.Items.Add("No BLE device found");
                            FManager.Close();
                        }
                        else
                        {
                            FClient.Address = Address;
                            Res = FClient.Connect(Radio);
                            if (Res != wclErrors.WCL_E_SUCCESS)
                                lbLog.Items.Add("Connect failed; 0x" + Res.ToString("X8"));
                        }
                    }
                }
                
                if (Res != wclErrors.WCL_E_SUCCESS)
                    FManager.Close();
            }
        }

        private void fmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            FClient.Disconnect();
            FManager.Close();
        }
    }
}

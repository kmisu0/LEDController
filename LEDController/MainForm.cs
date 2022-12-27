using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace LEDController
{
    public partial class MainForm : Form
    {
        private bool connected = false;
        static System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();
        private TrackBar[] tb_Led_Brightness;
        private TrackBar[] tb_FanSpeed_Setpoint;
        private Mitov.InstrumentLab.AngularGauge[] gau_Fan_ActualSpeed;
        private Mitov.InstrumentLab.Thermometer[] temp_Heatsink;

        private List<Parameters> currentControllerParameters = new List<Parameters>();

        public MainForm()
        {
            InitializeComponent();
            SetListViewColumnWidth();

            // Create arrays of controls
            tb_Led_Brightness = new TrackBar[] { tb_Led1_brightness, tb_Led2_brightness, tb_Led3_brightness, tb_Led4_brightness, tb_Led5_brightness };
            tb_FanSpeed_Setpoint = new TrackBar[] { tb_Fan1_SpeedSetpoint, tb_Fan2_SpeedSetpoint, tb_Fan3_SpeedSetpoint, tb_Fan4_SpeedSetpoint, tb_Fan5_SpeedSetpoint };
            gau_Fan_ActualSpeed = new Mitov.InstrumentLab.AngularGauge[] { gau_Fan1_ActualSpeed, gau_Fan2_ActualSpeed, gau_Fan3_ActualSpeed, gau_Fan4_ActualSpeed, gau_Fan5_ActualSpeed };
            temp_Heatsink = new Mitov.InstrumentLab.Thermometer[] { temp_Heatsink1, temp_Heatsink2, temp_Heatsink3, temp_Heatsink4, temp_Heatsink5 };

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Thread.CurrentThread.Name = "winFormThread";
            Thread thrCommunication = new Thread(commJob);
            thrCommunication.IsBackground = true;
            thrCommunication.Name = "commThread";
            thrCommunication.Start();

            myTimer.Tick += new EventHandler(TimerEventProcessor);
            myTimer.Interval = 100;
            myTimer.Start();            
        }

        private void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {
            myTimer.Stop();

            if (lb_IPValue.Text != "")
            {
                if(!connected)
                {
                    btn_disconnect.Enabled = false;
                    btn_connect.Enabled = true;
                }
                else
                {
                    btn_disconnect.Enabled = true;
                    btn_connect.Enabled = false;
                }
            }
            else
            {
                btn_disconnect.Enabled = false;
                btn_connect.Enabled = false;
            }

            if (connected)
            {
                if (lb_IPValue.Text != "")
                {
                    currentControllerParameters = tcpHandler.TcpClientHandler(lb_IPValue.Text, 9760, getParametersFromWinform());
                    if (currentControllerParameters == null)
                    {
                        //tcpHandler.closeTcpClient();
                        //connected = false;
                    }
                }

            }
            else
            {
                if(currentControllerParameters != null)
                    currentControllerParameters.Clear();
            }

            if (currentControllerParameters != null)
            {
                updateWinFormParameters();
            }
            myTimer.Start();
        }

        private void btn_connect_Cklick(object sender, EventArgs e)
        {
            connected = true;
        }

        private void btn_disconnect_Click(object sender, EventArgs e)
        {
            tcpHandler.closeTcpClient();
            lb_IPValue.Text = "";

            foreach (ListViewItem sample in lv_AvailableDevices.SelectedItems)
            {
                sample.Selected = false;
            }
            connected = false;
        }

        private void lv_AvailableDevices_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            e.Cancel = true;
            e.NewWidth = lv_AvailableDevices.Columns[e.ColumnIndex].Width;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            tcpHandler.closeTcpClient();
        }

        private void commJob()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(100);
                    updateListView(UdpBroadcast.BroadcastLEDController("LED controller?\0\n", "LED controler", 30303));
                }
            }
            catch (Exception er)
            {
                MessageBox.Show(er.ToString());
            }
        }


        private void updateListView(List<Device> lst_currentAvailableDevices)
        {
            if (lst_currentAvailableDevices.Count == lv_AvailableDevices.Items.Count)
            {
                return;
            }
            
            lv_AvailableDevices.Invoke(new Action(() => lv_AvailableDevices.Items.Clear()));

            foreach (Device sample in lst_currentAvailableDevices)
            {
                ListViewItem lvi_SelectedItem = new ListViewItem(sample.DeviceName);
                lvi_SelectedItem.SubItems.Add(string.Join(":", BitConverter.ToString(sample.MacAddress)));
                lvi_SelectedItem.SubItems.Add(sample.MacType);
                lvi_SelectedItem.SubItems.Add(string.Join(".", sample.IPAddress));
                lv_AvailableDevices.Invoke(new Action(() => lv_AvailableDevices.Items.Add(lvi_SelectedItem)));
            }
         }

        private List<Parameters> getParametersFromWinform()
        {
            List<Parameters> parameters = new List<Parameters>();
            for (int i = 0; i < tb_Led_Brightness.Length; i++)
            {
                parameters.Add(new Parameters
                {
                    brightness = tb_Led_Brightness[i].Value,
                    fanSpeedSetpoint = tb_FanSpeed_Setpoint[i].Value,
                    fanActualSpeed = Convert.ToInt32(gau_Fan_ActualSpeed[i].Value),
                    tempOfHeatsink = Convert.ToInt32(temp_Heatsink[i].Value)
                });
            }
            return parameters;
        }

        private void updateWinFormParameters()
        {
            if (currentControllerParameters.Count == 5)
            {
                for (int i = 0; i < currentControllerParameters.Count; i++)
                {
                    gau_Fan_ActualSpeed[i].Value = currentControllerParameters[i].fanActualSpeed;
                    temp_Heatsink[i].Value = currentControllerParameters[i].tempOfHeatsink;
                } 
            }
            else
            {
                for (int i = 0; i < gau_Fan_ActualSpeed.Length; i++)
                {
                    gau_Fan_ActualSpeed[i].Value = 0;
                    temp_Heatsink[i].Value = 0;
                }
            }
        }

        private void SetListViewColumnWidth()
        {
            int[] columnWidth = { 25, 25, 25, 25 };
            for (int i = 0; i < columnWidth.Length; i++)
            {
                lv_AvailableDevices.Columns[i].Width = columnWidth[i] * (lv_AvailableDevices.Size.Width - 1) / 100;
            }
        }

        private void lv_AvailableDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            lb_IPValue.Text = lv_AvailableDevices.SelectedItems[0].SubItems[3].Text;
        }

    }
}

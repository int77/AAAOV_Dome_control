using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ASCOM.Arduino
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {

        public SetupDialogForm()
        {
            this.Config = new Config();

            InitializeComponent();

            this.comboBoxComPort.Items.AddRange(new ASCOM.Utilities.Serial().AvailableCOMPorts);
            this.comboBoxComPort.SelectedItem = this.Config.ComPort;
            this.txtHomeAzimuth.Text = this.Config.HomeAzimuth.ToString();
            this.txtParkAzimuth.Text = this.Config.ParkAzimuth.ToString();
            this.ckbTraceEnable.Checked = this.Config.TraceEnabled;
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            if (this.comboBoxComPort.SelectedItem != null)
            {
                this.Config.ComPort = this.comboBoxComPort.SelectedItem.ToString();
                this.Config.HomeAzimuth = Convert.ToDouble(this.txtHomeAzimuth.Text);
                this.Config.ParkAzimuth = Convert.ToDouble(this.txtParkAzimuth.Text);
                this.Config.TraceEnabled = this.ckbTraceEnable.Checked;
            }
            Dispose();
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void BrowseToAscom(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://ascom-standards.org/");
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void SetupDialogForm_Load(object sender, EventArgs e)
        {

        }

    }
}
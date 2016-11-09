using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PuppetMaster;
using PuppetMaster.Exceptions;

namespace PuppetMasterGUI
{
    public partial class frmPuppetMaster : Form
    {
        private PuppetMasterControler controler;
        private string configFileName;

        public frmPuppetMaster()
        {
            InitializeComponent();
            this.controler = new PuppetMasterControler();
            this.configFileName = null;

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void txtConfigPath_TextChanged(object sender, EventArgs e)
        {
            //insert comand in command queue, 
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.InitialDirectory = "c:\\";
            fileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            fileDialog.FilterIndex = 2;
            fileDialog.RestoreDirectory = true;

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                this.configFileName = fileDialog.FileName;

            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.configFileName != null)
                    this.controler.ParseConfig(this.configFileName);
            }
            catch (Exception exp) when (exp is UnknownOperatorTypeException || exp is UnknownOperatorRoutingException)
            {
                //TODO:
            }
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            this.controler.RunAll();
        }

        private void btnStep_Click(object sender, EventArgs e)
        {
            Command cm = this.controler.Step();
            if (cm.Operator.Id == null)
                NextCommadTextBox.Text = cm.Type.ToString();
            else
            {
                if (cm.RepId == null)
                    NextCommadTextBox.Text = cm.Type.ToString() + " " + cm.Operator.Id;
                else
                    NextCommadTextBox.Text = cm.Type.ToString() + " " + cm.Operator.Id + " " + cm.RepId.ToString();
            }
        }
    }
}

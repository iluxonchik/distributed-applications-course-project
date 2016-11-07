using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMasterGUI
{
    public partial class frmPuppetMaster : Form
    {
        public frmPuppetMaster()
        {
            InitializeComponent();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {
            //chamar o parsing do ficheiro
            // guardar os operadores
            // chamar PCS para criar Operadores
        }

        private void txtConfigPath_TextChanged(object sender, EventArgs e)
        {
            //insert comand in command queue, 
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            // open file system browser
            //and save path from config file
            
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            //start parsing
            //save whatever needs to be saved
        }
    }
}

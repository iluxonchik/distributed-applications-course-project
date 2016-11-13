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
using ConfigTypes;
using ConfigTypes.Exceptions;
using System.Collections;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;

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
                this.txtConfigPath.Text = fileDialog.FileName;

            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            //try
            //{
            if (this.configFileName != null)
            {
                this.controler.ParseConfig(this.configFileName);

                Command cm = controler.getTopCommand();
                if (cm != null)
                {
                    try
                    {
                        try
                        {

                            NextCommadTextBox.Text = cm.Type.ToString() + " " + cm.Operator.Id + " " + cm.RepId.ToString();

                        }
                        catch (NullReferrencePropertyException)
                        {

                            NextCommadTextBox.Text = cm.Type.ToString() + " " + cm.Operator.Id;
                        }
                    }
                    catch (NullReferrencePropertyException)
                    {
                        NextCommadTextBox.Text = cm.Type.ToString();
                    }
                }
            }
            //}
            //catch (Exception exp) when (exp is UnknownOperatorTypeException || exp is UnknownOperatorRoutingException)
            //{
            //    //TODO: our exception
            //}
            //catch (Exception expAll)
            //{
            //    MessageBox.Show(expAll.StackTrace, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            try
            {
                this.controler.RunAll();
            }
            catch (Exception exp) when (exp is UnknownOperatorTypeException || exp is UnknownOperatorRoutingException)
            {
                //TODO: our exception
            }
            catch (Exception expAll)
            {
                MessageBox.Show(expAll.StackTrace, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnStep_Click(object sender, EventArgs e)
        {

            Command cm = null;
            try
            {
                cm = this.controler.Step();
            }
            catch (Exception exp1) when (exp1 is UnknownOperatorTypeException || exp1 is UnknownOperatorRoutingException)
            {
                //TODO: our exception
                MessageBox.Show("Invalid system Configuration", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (EndOfCommandsException)
            {
                MessageBox.Show("No more Commands to run", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception expAll)
            {
                MessageBox.Show(expAll.StackTrace, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


            /* get the next command before executing it and show */
            Command next = controler.getTopCommand();
            if (next != null)
            {

                try
                {
                    try
                    {

                        NextCommadTextBox.Text = next.Type.ToString() + " " + next.Operator.Id + " " + next.RepId.ToString();

                    }
                    catch (NullReferrencePropertyException)
                    {

                        NextCommadTextBox.Text = next.Type.ToString() + " " + next.Operator.Id;
                    }
                }
                catch (NullReferrencePropertyException)
                {
                    NextCommadTextBox.Text = next.Type.ToString();
                }

                //if (cm.Operator.Id == null)
                //    NextCommadTextBox.Text = cm.Type.ToString();
                //else
                //{
                //    try
                //    {

                //        NextCommadTextBox.Text = cm.Type.ToString() + " " + cm.Operator.Id + " " + cm.RepId.ToString();

                //    }
                //    catch (NullReferrencePropertyException)
                //    {

                //        NextCommadTextBox.Text = cm.Type.ToString() + " " + cm.Operator.Id;
                //    }
                //}
            }
        }


        private void Form1_Closing(object sender, FormClosingEventArgs e)
        {
            this.controler.CrashAll();

        }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;

namespace diagtool
{
    public partial class diagtool : Form
    {
        public diagtool()
        {
            InitializeComponent();
        }

        private void btnCollectInfo_Click(object sender, EventArgs e)
        {
            try
            {
                btnCollectInfo.Enabled = false;

                //if any information is checked.
                bool bEELogCheck = checkBoxEELog.Checked;
                bool bEECfgCheck = checkBoxEEConfig.Checked;
                bool bPCLogCheck = checkBoxPClog.Checked;
                if (!bEELogCheck && !bEECfgCheck && !bPCLogCheck)
                {
                    MessageBox.Show("None information selected.", "Error", MessageBoxButtons.OK);
                    return;
                }

                //check output path
                string strOutput = textBoxOutputDir.Text.Trim();
                if (string.IsNullOrEmpty(strOutput))
                {
                    MessageBox.Show("Please select the directory to output the information.", "Error", MessageBoxButtons.OK);
                    return;
                }

                if (!System.IO.Directory.Exists(strOutput))
                {
                    MessageBox.Show("The output directory doesn't exist.", "Error", MessageBoxButtons.OK);
                    return;
                }

                //collect info;
                if (bEELogCheck)
                {
                   CollectEELogInfo(strOutput);
                }

  
                if(bEECfgCheck)
                {
                    CollectEECfgInfo(strOutput);
                }

                if(bPCLogCheck)
                {
                    CollectPCLogInfo(strOutput);
                }

                MessageBox.Show("Finished.", "Finished", MessageBoxButtons.OK);

            }
            catch(Exception ex)
            {
                MessageBox.Show(string.Format("Exception happened:{0}", ex.Message), "Exception", MessageBoxButtons.OK);
            }
            finally
            {
                btnCollectInfo.Enabled = true;
            }
           
        }

        private string GetEEInstallPath()
        {
            RegistryKey eekey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\NextLabs\Compliant Enterprise\Exchange Enforcer");
    
            if(eekey!=null)
            {
               return  (eekey.GetValue("InstallDir") as string);
            }
            return "";
        }

        private string GetPCInstallPath()
        {
            RegistryKey pckey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\NextLabs\Compliant Enterprise\Policy Controller");

            if (pckey != null)
            {
                return (pckey.GetValue("PolicyControllerDir") as string);
            }
            return "";
        }

        private void CollectPCLogInfo(string strOutput)
        {
            string strPCInstallPath = GetPCInstallPath();

            if (!string.IsNullOrEmpty(strPCInstallPath))
            {
                string strPCLogDir = strPCInstallPath + "agentLog";

                string strPcCfgOutputDir = strOutput + "\\PCLog";
                System.IO.Directory.CreateDirectory(strPcCfgOutputDir);

                DirectoryCopy(strPCLogDir, strPcCfgOutputDir, true);
            }
        }

        private void CollectEECfgInfo(string strOutput)
        {
            string strEEInstallPath = GetEEInstallPath();

            if (!string.IsNullOrEmpty(strEEInstallPath))
            {
                string strEECfgDir = strEEInstallPath + "config";

                string strEECfgOutputDir = strOutput + "\\EEConfig";
                System.IO.Directory.CreateDirectory(strEECfgOutputDir);

                DirectoryCopy(strEECfgDir, strEECfgOutputDir, true);
            }
        }

        private void CollectEELogInfo(string strOutput)
        {
            string strEEInstallPath = GetEEInstallPath();

            if(!string.IsNullOrEmpty(strEEInstallPath))
            {
                string strEELogDir = strEEInstallPath + "logs";

                string strEELogOutputDir = strOutput + "\\EELog";
                System.IO.Directory.CreateDirectory(strEELogOutputDir);

                DirectoryCopy(strEELogDir, strEELogOutputDir, true);
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private void btnInfoSelectDir_Click(object sender, EventArgs e)
        {
            // Show the FolderBrowserDialog.
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                string folderName = folderBrowserDialog1.SelectedPath;
                textBoxOutputDir.Text = folderName;
            }
        }
    }
}

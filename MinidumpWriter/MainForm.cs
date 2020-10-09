using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using MinidumpWriter.Properties;
using System.IO;
using System.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace MinidumpWriter
{
    public partial class MainForm : Form
    {
        FileSystemWatcher fsw = new FileSystemWatcher();

        public MainForm()
        {
            InitializeComponent();

            if (Program.IsAdministrator() == true)
            {
                this.Text += "(Administrator)";
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Settings.Default.DumpFolder) == true)
            {
                Settings.Default.DumpFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                Settings.Default.Save();
            }

            this.lnkFolder.Text = Settings.Default.DumpFolder;
            fsw.IncludeSubdirectories = false;
            fsw.Path = this.lnkFolder.Text;
            fsw.Changed += new FileSystemEventHandler(fsw_Changed);
            fsw.Created += new FileSystemEventHandler(fsw_Changed);
            fsw.Deleted += new FileSystemEventHandler(fsw_Changed);
            fsw.EnableRaisingEvents = true;

            this.lstProcesses.HideSelection = false;
            this.btnDump.Enabled = false;
            this.btnDelete.Enabled = false;
            this.lstProcesses.ListViewItemSorter = new ListViewItemComparer(2);

            FillProcesses();
            FillDumpList();
        }

        void fsw_Changed(object sender, FileSystemEventArgs e)
        {
            this.BeginInvoke(
                new ThreadStart( 
                    () =>
                        {
                            FillDumpList();
                        }
            ));
        }

        private void FillProcesses()
        {
            this.lstProcesses.Items.Clear();

            foreach (Process process in Process.GetProcesses())
            {
                ListViewItem item = new ListViewItem(process.Id.ToString());

                ListViewItem.ListViewSubItem subItem1 = new ListViewItem.ListViewSubItem();
                subItem1.Text = Is64bitProcess(process.Id);
                item.SubItems.Add(subItem1);

                ListViewItem.ListViewSubItem subItem2 = new ListViewItem.ListViewSubItem();
                subItem2.Text = process.ProcessName;
                item.SubItems.Add(subItem2);

                this.lstProcesses.Items.Add(item);
            }
        }

        private string Is64bitProcess(int processId)
        {
            if (Environment.Is64BitOperatingSystem == true)
            {
                bool wow64Process = false;

                IntPtr pProcessHandle = Utility.OpenProcess(MinidumpWriter.Utility.ProcessAccessFlags.QueryInformation, false, processId);

                Utility.IsWow64Process(pProcessHandle, out wow64Process);

                Utility.CloseHandle(pProcessHandle);

                return (wow64Process == true) ? "x86" : "x64";
            }

            return "x86";
        }

        public class ListViewItemComparer : IComparer
        {
            private int col;

            public ListViewItemComparer()
            {
                col = 0;
            }

            public ListViewItemComparer(int column)
            {
                col = column;
            }

            public int Compare(object x, object y)
            {
                return String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
            }
        }

        private void lstProcesses_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            this.lstProcesses.ListViewItemSorter = new ListViewItemComparer(e.Column);
        }

        private void lstProcesses_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.btnDump.Enabled = this.lstProcesses.SelectedIndices.Count != 0;
        }

        private void btnDump_Click(object sender, EventArgs e)
        {
            if (this.lstProcesses.SelectedItems.Count == 0)
            {
                return;
            }

            string pid = this.lstProcesses.SelectedItems[0].Text;
            string processName = this.lstProcesses.SelectedItems[0].SubItems[2].Text;

            string filePath = System.IO.Path.Combine(this.lnkFolder.Text, string.Format("{0}_{1}_{2}.dmp", processName, pid,
                DateTime.Now.ToString("s").Replace(":", "-")));

            string dumpToolName;
            if (this.lstProcesses.SelectedItems[0].SubItems[1].Text == "x86")
            {
                dumpToolName = "x86DumpWriter";
            }
            else
            {
                dumpToolName = "x64DumpWriter";
            }

            string path = Process.GetCurrentProcess().MainModule.FileName;
            string directoryPath = System.IO.Path.GetDirectoryName(path);

            string toolPath = System.IO.Path.Combine(directoryPath, string.Format("{0}.exe", dumpToolName));

            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.FileName = toolPath;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = string.Format("\"{0}\" {1}", filePath, pid);

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            Process newProcess = System.Diagnostics.Process.Start(startInfo);
            newProcess.WaitForExit();
            this.Cursor = oldCursor;
        }

        private void lnkFolder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenVerb(lnkFolder.Text);
        }

        public static void OpenVerb(string path)
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(path);
            startInfo.UseShellExecute = true;
            System.Diagnostics.Process.Start(startInfo);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog fbd = new CommonOpenFileDialog();
            fbd.DefaultFileName = lnkFolder.Text;
            fbd.IsFolderPicker = true;

            if (fbd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                this.lnkFolder.Text = fbd.FileName;
                this.fsw.Path = this.lnkFolder.Text;
                Settings.Default.DumpFolder = fbd.FileName;
                Settings.Default.Save();
            }

            FillDumpList();
        }

        private void FillDumpList()
        {
            this.lstDumps.Items.Clear();

            foreach (string filePath in System.IO.Directory.EnumerateFiles(this.lnkFolder.Text, "*.dmp"))
            {
                ListViewItem item = new ListViewItem(filePath);

                ListViewItem.ListViewSubItem subItem = new ListViewItem.ListViewSubItem();

                FileInfo info = new FileInfo(filePath);
                subItem.Text = FileHelper.ToByteString(info.Length);

                item.SubItems.Add(subItem);

                this.lstDumps.Items.Add(item);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            FillProcesses();
            FillDumpList();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (this.lstDumps.SelectedIndices.Count == 0)
            {
                return;
            }

            string filePath = this.lstDumps.SelectedItems[0].Text;
            System.IO.File.Delete(filePath);
        }

        private void lstDumps_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.btnDelete.Enabled = this.lstDumps.SelectedIndices.Count != 0;
        }

        public class FileHelper
        {
            private static readonly long kilobyte = 1024;
            private static readonly long megabyte = 1024 * kilobyte;
            private static readonly long gigabyte = 1024 * megabyte;
            private static readonly long terabyte = 1024 * gigabyte;

            public static string ToByteString(long bytes)
            {
                if (bytes > terabyte) return ((double)bytes / terabyte).ToString("0.00 TB");
                else if (bytes > gigabyte) return ((double)bytes / gigabyte).ToString("0.00 GB");
                else if (bytes > megabyte) return ((double)bytes / megabyte).ToString("0.00 MB");
                else if (bytes > kilobyte) return ((double)bytes / kilobyte).ToString("0.00 KB");
                else return bytes + " Bytes";
            }
        } 

    }
}

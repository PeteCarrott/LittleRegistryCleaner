﻿/*
    Little Registry Cleaner
    Copyright (C) 2008 Nick H.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

namespace Little_Registry_Cleaner
{
    static class Program
    {
        /// <summary>
        /// <para>The main entry point for the application.</para>
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool bMutexCreated = false;
            Mutex mutexMain = new Mutex(true, "Little Registry Cleaner", out bMutexCreated);

            // If mutex isnt available, show message and exit...
            if (!bMutexCreated)
            {
                MessageBox.Show("Another program seems to be running already...", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            // Set the build time
            Properties.Settings.Default.strBuildTime = new DateTime(2000, 1, 1).AddDays(Assembly.GetExecutingAssembly().GetName().Version.Build).ToShortDateString();

            // Create event log source
            if (!EventLog.SourceExists(Application.ProductName))
                EventLog.CreateEventSource(Application.ProductName, "Application");

            // Check if backup settings and directory exists...
            if (string.IsNullOrEmpty(Properties.Settings.Default.strProgramSettingsDir))
            {
                Properties.Settings.Default.strProgramSettingsDir = string.Format("{0}\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), Application.ProductName);
                if (!Directory.Exists(Properties.Settings.Default.strProgramSettingsDir))
                    Directory.CreateDirectory(Properties.Settings.Default.strProgramSettingsDir);
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.strOptionsBackupDir))
            {
                Properties.Settings.Default.strOptionsBackupDir = string.Format("{0}\\Backups", Properties.Settings.Default.strProgramSettingsDir);
                if (!Directory.Exists(Properties.Settings.Default.strOptionsBackupDir))
                    Directory.CreateDirectory(Properties.Settings.Default.strOptionsBackupDir);
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.strOptionsLogDir))
            {
                Properties.Settings.Default.strOptionsLogDir = string.Format("{0}\\Logs", Properties.Settings.Default.strProgramSettingsDir);
                if (!Directory.Exists(Properties.Settings.Default.strOptionsLogDir))
                    Directory.CreateDirectory(Properties.Settings.Default.strOptionsLogDir);
            }

            if (string.IsNullOrEmpty(Properties.Settings.Default.strErrorDir))
            {
                Properties.Settings.Default.strErrorDir = string.Format("{0}\\Errors", Properties.Settings.Default.strProgramSettingsDir);
                if (!Directory.Exists(Properties.Settings.Default.strErrorDir))
                    Directory.CreateDirectory(Properties.Settings.Default.strErrorDir);
            }   

            // Add event handler for thread exceptions
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

            // Get SeBackupPrivilege and SeRestorePrivilege
            Permissions.SetPrivileges();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());

            mutexMain.ReleaseMutex();

            Properties.Settings.Default.Save();

            return;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ErrorDlg ErrorDlg = new ErrorDlg((Exception)e.ExceptionObject, e.IsTerminating);
            ErrorDlg.ShowDialog();
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ErrorDlg ErrorDlg = new ErrorDlg(e.Exception);
            ErrorDlg.ShowDialog();
        }
    }
}

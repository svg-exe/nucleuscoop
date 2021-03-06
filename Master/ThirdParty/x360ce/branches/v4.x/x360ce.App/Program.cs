﻿using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using x360ce.App.Properties;

namespace x360ce.App
{
	static class Program
	{

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			try
			{
				//var fi = new FileInfo(Application.ExecutablePath);
				//Directory.SetCurrentDirectory(fi.Directory.FullName);
				// IMPORTANT: Make sure this method don't have any static references to x360ce.Engine library or
				// program tries to load x360ce.Engine.dll before AssemblyResolve event is available and fails.
				AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
				if (!RuntimePolicyHelper.LegacyV2RuntimeEnabledSuccessfully)
				{
					// Failed to enable useLegacyV2RuntimeActivationPolicy at runtime.
				}
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				// Requires System.Configuration.Installl reference.
				var ic = new System.Configuration.Install.InstallContext(null, args);
				if (ic.Parameters.ContainsKey("Settings"))
				{
					OpenSettingsFolder(Application.UserAppDataPath);
					OpenSettingsFolder(Application.CommonAppDataPath);
					OpenSettingsFolder(Application.LocalUserAppDataPath);
					return;
				}
				if (!CheckSettings()) return;
				//Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
				MainForm.Current = new MainForm();
				if (ic.Parameters.ContainsKey("Exit"))
				{
					MainForm.Current.BroadcastMessage(MainForm.wParam_Close);
					return;
				}
				if (!IsOneCopyRunningAlready())
				{
					Application.Run(MainForm.Current);
				}
			}
			catch (Exception ex)
			{
				var message = "";
				AddExceptionMessage(ex, ref message);
				if (ex.InnerException != null) AddExceptionMessage(ex.InnerException, ref message);
				var box = new Controls.MessageBoxForm();
				if (message.Contains("Could not load file or assembly 'Microsoft.DirectX"))
				{
					message += "===============================================================\r\n";
					message += "You can click the link below to download Microsoft DirectX.";
					box.MainLinkLabel.Text = "http://www.microsoft.com/en-us/download/details.aspx?id=35";
					box.MainLinkLabel.Visible = true;
				}
				var result = box.ShowForm(message, "Exception!", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
				if (result == DialogResult.Cancel) Application.Exit();
			}
		}

		public static bool IsOneCopyRunningAlready()
		{
			var ini = new x360ce.Engine.Ini(SettingManager.IniFileName);
			var oneCopy = !ini.File.Exists || ini.GetValue("Options", SettingName.AllowOnlyOneCopy) == "1";
			return (oneCopy && MainForm.Current.BroadcastMessage(MainForm.wParam_Restore));
		}

		public static bool IsClosing;

		/// <summary>Add information about missing libraries and DLLs</summary>
		static void AddExceptionMessage(Exception ex, ref string message)
		{
			var ex1 = ex as ConfigurationErrorsException;
			var ex2 = ex as ReflectionTypeLoadException;
			var m = "";
			if (ex1 != null)
			{
				m += string.Format("Filename: {0}\r\n", ex1.Filename);
				m += string.Format("Line: {0}\r\n", ex1.Line);
			}
			else if (ex2 != null)
			{
				foreach (Exception x in ex2.LoaderExceptions) m += x.Message + "\r\n";
			}
			if (message.Length > 0)
			{
				message += "===============================================================\r\n";
			}
			message += ex.ToString() + "\r\n";
			foreach (var key in ex.Data.Keys)
			{
				m += string.Format("{0}: {1}\r\n", key, ex1.Data[key]);
			}
			if (m.Length > 0)
			{
				message += "===============================================================\r\n";
				message += m;
			}
		}

		public static object DeviceLock = new object();

		public static int TimerCount = 0;
		public static int ReloadCount = 0;
		public static int ErrorCount = 0;

		public static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
		{
			if (IsClosing) return;
			ErrorCount++;
			MainForm.Current.UpdateTimer.Stop();
			MainForm.Current.UpdateStatus("- " + e.Exception.Message);
			MainForm.Current.UpdateTimer.Start();
		}

		static void OpenSettingsFolder(string path)
		{
			var di = new DirectoryInfo(path);
			//if (!di.Exists) return;
			//if (di.GetFiles().Length == 0) return;
			var psi = new ProcessStartInfo(di.Parent.Parent.FullName);
			psi.UseShellExecute = true;
			psi.ErrorDialog = true;
			Process.Start(psi);
		}

		static bool CheckSettings()
		{
			try
			{
				Settings.Default.Reload();
			}
			catch (ConfigurationErrorsException ex)
			{
				// Requires System.Configuration
				string filename = ((ConfigurationErrorsException)ex.InnerException).Filename;
				var title = "Corrupt user settings of " + Application.ProductName;
				var text =
					"Program has detected that your user settings file has become corrupted. " +
					"This may be due to a crash or improper exiting of the program. " +
					"Program must reset your user settings in order to continue.\r\n" +
					"Click [Yes] to reset your user settings and continue.\r\n" +
					"Click [No] if you wish to exit and attempt manual repair.";
				var result = MessageBox.Show(text, title, MessageBoxButtons.YesNo, MessageBoxIcon.Error);
				if (result == DialogResult.Yes)
				{
					File.Delete(filename);
					Settings.Default.Reload();
				}
				else
				{
					OpenSettingsFolder(Application.UserAppDataPath);
					OpenSettingsFolder(Application.CommonAppDataPath);
					OpenSettingsFolder(Application.LocalUserAppDataPath);
					return false;
				}
			}
			return true;
		}


		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs e)
		{
			string dllName = e.Name.Contains(",") ? e.Name.Substring(0, e.Name.IndexOf(',')) : e.Name.Replace(".dll", "");
			string path = null;
			switch (dllName)
			{
				case "vJoyInterface":
				case "vJoyInterfaceWrap":
					path = GetResourceName("vJoy", dllName+".dll");
					break;
				case "x360ce.Engine":
					path = "Resources.x360ce.Engine.dll";
					break;
				case "x360ce.Engine.XmlSerializers":
					path = "Resources.x360ce.Engine.XmlSerializers.dll";
					break;
				case "SharpDX":
					path = "Resources.SharpDX.SharpDX.dll";
					break;
				case "SharpDX.DirectInput":
					path = "Resources.SharpDX.SharpDX.DirectInput.dll";
					break;
				default:
					break;
			}
			if (path == null) return null;
			var assembly = Assembly.GetExecutingAssembly();
			var sr = assembly.GetManifestResourceStream(typeof(MainForm).Namespace + "." + path);
			if (sr == null)
			{
				return null;
			}
			byte[] bytes = new byte[sr.Length];
			sr.Read(bytes, 0, bytes.Length);
			var asm = Assembly.Load(bytes);
			return asm;
		}

		public static string GetResourceName(string folder, string name)
		{
			var assembly = Assembly.GetEntryAssembly();
			var architecture = assembly.GetName().ProcessorArchitecture;
			// There must be an easier way to check embedded non managed DLL version.
			var paString = "";
			if (architecture == ProcessorArchitecture.Amd64) paString = "_x64";
			if (architecture == ProcessorArchitecture.X86) paString = "_x86";
			return string.Format("Resources.{0}{1}.{2}", folder, paString, name);
		}


	}
}

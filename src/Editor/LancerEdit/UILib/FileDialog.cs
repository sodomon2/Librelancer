﻿/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Diagnostics;
using System.Reflection;
using LibreLancer;
namespace LancerEdit
{
	static class FileDialog
	{
        static dynamic parentForm;
        static bool kdialog;
        static IntPtr parentWindow;
        public static void RegisterParent(Game game)
        {
			if (Platform.RunningOS == OS.Windows)
			{
				IntPtr ptr;
				if ((ptr = game.GetHwnd()) == IntPtr.Zero) return;
				LoadSwf();
				var t = winforms.GetType("System.Windows.Forms.Control");
				var method = t.GetMethod("FromHandle", BindingFlags.Public | BindingFlags.Static);
				parentForm = method.Invoke(null, new object[] { ptr });
			}
            else
            {
                kdialog = HasKDialog();
                game.GetX11Info(out IntPtr _, out parentWindow);
            }
        }

        public static string Open()
		{
			if (Platform.RunningOS == OS.Windows)
			{
				string result = null;
				using (var ofd = NewObj("System.Windows.Forms.OpenFileDialog"))
				{
                    if (parentForm != null) ofd.Parent = parentForm;
					if (ofd.ShowDialog() == SwfOk())
					{
						result = ofd.FileName;
					}
				}
				WinformsDoEvents();
				return result;
			}
			else if (Platform.RunningOS == OS.Linux)
			{
                if (kdialog)
                    return KDialogOpen();
                else
				    return GtkOpen();
			}
			else
			{
				//Mac
				throw new NotImplementedException();
			}
		}

		public static string Save()
		{
			if (Platform.RunningOS == OS.Windows)
			{
				string result = null;
				using (var sfd = NewObj("System.Windows.Forms.SaveFileDialog"))
				{
                    if (parentForm != null) sfd.Parent = parentForm;
                    if (sfd.ShowDialog() == SwfOk())
					{
						result = sfd.FileName;
					}
				}
				WinformsDoEvents();
				return result;
			}
			else if (Platform.RunningOS == OS.Linux)
			{
                if (kdialog)
                    return KDialogSave();
                else
				    return GtkSave();
			}
			else
			{
				//Mac
				throw new NotImplementedException();
			}
		}

        const string WINFORMS_NAME = "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        static Assembly winforms;
        static void LoadSwf()
        {
            if (winforms == null)
                winforms = Assembly.Load(WINFORMS_NAME);
        }
		static dynamic NewObj(string type)
		{
            LoadSwf();
			return Activator.CreateInstance(winforms.GetType(type));
		}

		static dynamic SwfOk()
		{
            LoadSwf();
			var type = winforms.GetType("System.Windows.Forms.DialogResult");
			return Enum.Parse(type, "OK");
		}
		static void WinformsDoEvents()
		{
            LoadSwf();
			var t = winforms.GetType("System.Windows.Forms.Application");
			var method = t.GetMethod("DoEvents", BindingFlags.Public | BindingFlags.Static);
			method.Invoke(null, null);
		}

		static string GtkOpen()
		{
			if (!Gtk.gtk_init_check(IntPtr.Zero, IntPtr.Zero))
			{
				throw new Exception();
			}
			var dlg = Gtk.gtk_file_chooser_dialog_new("Open File", IntPtr.Zero,
													  Gtk.GTK_FILE_CHOOSER_ACTION_OPEN,
													  IntPtr.Zero);
			Gtk.gtk_dialog_add_button(dlg, "_Cancel", Gtk.GTK_RESPONSE_CANCEL);
			Gtk.gtk_dialog_add_button(dlg, "_Accept", Gtk.GTK_RESPONSE_ACCEPT);
			Gtk.gtk_window_set_keep_above(dlg, true); //better than it disappearing
			string result = null;
			if (Gtk.gtk_dialog_run(dlg) == Gtk.GTK_RESPONSE_ACCEPT)
                result = UnsafeHelpers.PtrToStringUTF8(Gtk.gtk_file_chooser_get_filename(dlg));
			WaitCleanup();
			Gtk.gtk_widget_destroy(dlg);
			WaitCleanup();
			return result;
		}

		static string GtkSave()
		{
			if (!Gtk.gtk_init_check(IntPtr.Zero, IntPtr.Zero))
			{
				throw new Exception();
			}
			var dlg = Gtk.gtk_file_chooser_dialog_new("Save File", IntPtr.Zero,
													  Gtk.GTK_FILE_CHOOSER_ACTION_SAVE,
													  IntPtr.Zero);
			Gtk.gtk_dialog_add_button(dlg, "_Cancel", Gtk.GTK_RESPONSE_CANCEL);
			Gtk.gtk_dialog_add_button(dlg, "_Accept", Gtk.GTK_RESPONSE_ACCEPT);
			Gtk.gtk_window_set_keep_above(dlg, true); //better than it disappearing
			string result = null;
			if (Gtk.gtk_dialog_run(dlg) == Gtk.GTK_RESPONSE_ACCEPT)
                result = UnsafeHelpers.PtrToStringUTF8(Gtk.gtk_file_chooser_get_filename(dlg));
			WaitCleanup();
			Gtk.gtk_widget_destroy(dlg);
			WaitCleanup();
			return result;
		}

		static void WaitCleanup()
		{
			while (Gtk.gtk_events_pending())
				Gtk.gtk_main_iteration();
		}

        static bool HasKDialog()
        {
            var p = Process.Start("bash", "-c 'command -v kdialog'");
            p.WaitForExit();
            return p.ExitCode == 0;
        }

        static string KDialogProcess(string s)
        {
            if (parentWindow != IntPtr.Zero) 
                s = string.Format("--attach {0} {1}", parentWindow, s);
            var pinf = new ProcessStartInfo("kdialog", s);
            pinf.RedirectStandardOutput = true;
            pinf.UseShellExecute = false;
            var p = Process.Start(pinf);
            string output = "";
            p.OutputDataReceived += (sender, e) => {
                output += e.Data + "\n";
            };
            p.BeginOutputReadLine();
            p.WaitForExit();
            if (p.ExitCode == 0)
                return output.Trim();
            else
                return null;
        }

        static string lastSave = "";
        static string KDialogSave()
        {
            if (string.IsNullOrEmpty(lastSave))
                lastSave = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var ret = KDialogProcess(string.Format("--getsavefilename \"{0}\"", lastSave));
            lastSave = ret ?? lastSave;
            return ret;
        }
        static string lastOpen = "";
        static string KDialogOpen()
        {
            if (String.IsNullOrEmpty(lastOpen))
                lastOpen = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var ret = KDialogProcess(string.Format("--getopenfilename \"{0}\"", lastOpen));
            lastOpen = ret ?? lastOpen;
            return ret;
        }

	}
}

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;

namespace MyNppPlugin1
{
    class Main
    {
        #region " Fields "
        internal const string PluginName = "MyNppPlugin1";
        static string iniFilePath = null;
        static bool someSetting = false;
        static frmMyDlg frmMyDlg = null;
        static int idMyDlg = -1;
        static Bitmap tbBmp = Properties.Resources.star;
        static Bitmap tbBmp_tbTab = Properties.Resources.star_bmp;
        static Icon tbIcon = null;
        static Object dummyForWin32Call = new Object();

        //TODO avoid hardcode
        internal const string SCRIPT_ROOT_FOLDER = "d:\\scripts\\NppRunPerl";
        internal const string SCRIPT_FILE_NAME = "runperl_script.pl";
        #endregion

        #region " StartUp/CleanUp "
        internal static void CommandMenuInit()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(iniFilePath)) Directory.CreateDirectory(iniFilePath);
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");
            someSetting = (Win32.GetPrivateProfileInt("SomeSection", "SomeKey", 0, iniFilePath) != 0);

            PluginBase.SetCommand(0, "DEBUG", myMenuFunction, new ShortcutKey(false, false, false, Keys.None));
            PluginBase.SetCommand(1, "MyDockableDialog", myDockableDialog); idMyDlg = 1;
        }
        internal static void SetToolBarIcon()
        {
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[idMyDlg]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }
        internal static void PluginCleanUp()
        {
            Win32.WritePrivateProfileString("SomeSection", "SomeKey", someSetting ? "1" : "0", iniFilePath);
        }
        #endregion

        #region " Menu functions "
        internal static void myMenuFunction()
        {
            if (!checkIfScriptIsOpen()) {
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DOOPEN, 0, SCRIPT_ROOT_FOLDER + "\\" + SCRIPT_FILE_NAME);
                MessageBox.Show("DEBUG: File not open");
            } else {
                string currentFileName = getCurrentFileName();
                runInCommandLine("perl " + SCRIPT_ROOT_FOLDER + "\\runperl_runner.pl "
                    + SCRIPT_ROOT_FOLDER + "\\" + SCRIPT_FILE_NAME
                    + " \"" + currentFileName
                    + "\" > " + SCRIPT_ROOT_FOLDER + "\\command_line_output.txt");
                MessageBox.Show("DEBUG: script executed");
                //TODO copy result from temp file to current file(replace)
                try {
                    System.IO.StreamReader outputFile = new System.IO.StreamReader(SCRIPT_ROOT_FOLDER + "\\runperl_output.txt");
                    string output = outputFile.ReadToEnd();
                    outputFile.Close();
                    MessageBox.Show(output);
                    Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_SETTEXT, 0, output);
                } catch (Exception e) {
                    reportError(e.ToString(), e);
                }
            }
        }
        internal static void myDockableDialog()
        {
            if (frmMyDlg == null)
            {
                frmMyDlg = new frmMyDlg();

                using (Bitmap newBmp = new Bitmap(16, 16))
                {
                    Graphics g = Graphics.FromImage(newBmp);
                    ColorMap[] colorMap = new ColorMap[1];
                    colorMap[0] = new ColorMap();
                    colorMap[0].OldColor = Color.Fuchsia;
                    colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
                    ImageAttributes attr = new ImageAttributes();
                    attr.SetRemapTable(colorMap);
                    g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
                    tbIcon = Icon.FromHandle(newBmp.GetHicon());
                }

                NppTbData _nppTbData = new NppTbData();
                _nppTbData.hClient = frmMyDlg.Handle;
                _nppTbData.pszName = "My dockable dialog";
                _nppTbData.dlgID = idMyDlg;
                _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_RIGHT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
                _nppTbData.hIconTab = (uint)tbIcon.Handle;
                _nppTbData.pszModuleName = PluginName;
                IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
                Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
            }
            else
            {
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DMMSHOW, 0, frmMyDlg.Handle);
            }
        }
        #endregion

        #region " Internal functions "
        internal static bool checkIfScriptIsOpen()
        {
            bool result = false;
            foreach (string file in getOpenFiles()) {
                if (file.ToLower().Equals(SCRIPT_ROOT_FOLDER + "\\" + SCRIPT_FILE_NAME, StringComparison.OrdinalIgnoreCase)) {
                    result = true;
                }
            }
            return result;
        }
        internal static System.Collections.Generic.List<string> getOpenFiles() {
            int nbFile = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETNBOPENFILES, 0, 0);
            using (ClikeStringArray cStrArray = new ClikeStringArray(nbFile, Win32.MAX_PATH)) {
                if (Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETOPENFILENAMES, cStrArray.NativePointer, nbFile) != IntPtr.Zero)
                    return cStrArray.ManagedStringsUnicode;
                else 
                    return null;
            }
        }
        //This workaround was put here because NPPM_GETFULLCURRENTPATH doesn't seem to work correctly
        internal static string getCurrentFileName() {
            int capacity = Win32Ext.GetWindowTextLength(new HandleRef(dummyForWin32Call, PluginBase.nppData._nppHandle)) * 2;
            StringBuilder stringBuilder = new StringBuilder(capacity);
            Win32Ext.GetWindowText(new HandleRef(dummyForWin32Call, PluginBase.nppData._nppHandle), stringBuilder, stringBuilder.Capacity);
            string windowTitle = stringBuilder.ToString();
            return windowTitle.Substring(0, windowTitle.IndexOf(" - Notepad++"));

            /*IntPtr fileName = Marshal.AllocHGlobal(Win32.MAX_PATH);
            if (fileName.ToInt64() == 0) {
                reportError("Failed to allocate memory with AllocHGlobal");
            }
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETFULLCURRENTPATH, fileName, Win32.MAX_PATH);
            string result = Marshal.PtrToStringUni(fileName);
            Marshal.FreeHGlobal(fileName);
            return result;*/ 

            /*string mangledFileName = Marshal.PtrToStringAuto(fileName).Substring(1);
            foreach (string file in getOpenFiles()) {
                if (file.Length == mangledFileName.Length + 1 && file.EndsWith(mangledFileName)) {
                    return file;
                } else {
                    continue;
                }
            }
            reportError("Current open file cannot be found among all the open files");
            return null;*/
        }
        internal static void runInCommandLine(string command) {
            try
            {
                //TODO uncomment window hiding code

                // create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // Incidentally, /c tells cmd that we want it to execute the command that follows,
                // and then exit.
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);

                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
                //procStartInfo.RedirectStandardOutput = true;
                //procStartInfo.UseShellExecute = false;
                // Do not create the black window.
                //procStartInfo.CreateNoWindow = true;
                // Now we create a process, assign its ProcessStartInfo and start it
                System.Diagnostics.Process proc = System.Diagnostics.Process.Start(procStartInfo);
                // Get the output into a string
                //string result = proc.StandardOutput.ReadToEnd();
                // Display the command output.
                //Console.WriteLine(result);
            }
            catch (Exception objException)
            {
                MessageBox.Show("Exception while trying to run a command line command:\n'" + command
                    + "'\n" + objException.ToString());
            }
        }

        internal static void reportError(string message, Exception exception) {
            MessageBox.Show(message);
            if (exception != null) {
                throw exception;
            }
        }

        internal static void reportError(string message, Type exceptionType) {
            if (exceptionType != null) {
                Object exception = exceptionType.GetConstructor(new Type[] { message.GetType() }).Invoke(new Object[] { message });
                if (exception != null) {
                    reportError(message, (Exception)exception);
                } else {
                    reportError("Could not create an exception of type " + exceptionType.FullName);
                }
            }
        }

        internal static void reportError(string message) {
            reportError(message, (Exception)null);
        }

        #endregion

        #region " Platform "
        class Win32Ext {
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern int GetWindowTextLength(HandleRef hWnd);
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern int GetWindowText(HandleRef hWnd, StringBuilder lpString, int nMaxCount);
        }
        #endregion
    }
}
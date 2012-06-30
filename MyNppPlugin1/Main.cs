using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;

//TODO No error when script doesn't compile
//TODO Bring scripts to the plugin folder
//TODO Make plugin configurable via gui
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
        internal const string SCRIPT_OUTPUT_FILE_NAME = "runperl_output.txt";
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
            PluginBase.SetCommand(1, "Show Dockable Dialog", myDockableDialog); idMyDlg = 1;
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

        //TODO refactor : extract methods
        internal static void myMenuFunction()
        {
            //MessageBox.Show(getCurrentFileName());

            

            if (!checkIfScriptIsOpen()) {
                //Open script file
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DOOPEN, 0, SCRIPT_ROOT_FOLDER + "\\" + SCRIPT_FILE_NAME);
                //Move it to other view
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_VIEW_GOTO_ANOTHER_VIEW);
            } else {
                //string inputFileName = getCurrentFileName();
                string inputFileName = SCRIPT_ROOT_FOLDER + "\\" + "runperl_input.txt";
                writeInputToFile(inputFileName);
                string command = "perl " + SCRIPT_ROOT_FOLDER + "\\runperl_runner.pl "
                    + SCRIPT_ROOT_FOLDER + "\\" + SCRIPT_FILE_NAME
                    + " \"" + inputFileName
                    + "\"";// +" > " + SCRIPT_ROOT_FOLDER + "\\command_line_output.txt";
                //DEBUG
                //Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_SETTEXT, 0, command);
                runInCommandLine(command);
                try {
                    string outputFilePath = SCRIPT_ROOT_FOLDER + "\\" + SCRIPT_OUTPUT_FILE_NAME;
                    if (File.Exists(outputFilePath)) {
                        System.IO.StreamReader outputFile = new System.IO.StreamReader(outputFilePath);
                        string output = outputFile.ReadToEnd();
                        outputFile.Close();
                        File.Delete(outputFilePath);
                        placeOutput(output);
                    } else {
                        //TODO output more info about compilation error
                        reportError("No output file found.\nThis is likely due to a compilation error.");
                    }
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
        //TODO remove
        internal static string getCurrentFileName() {
            int capacity = Win32Ext.GetWindowTextLength(new HandleRef(dummyForWin32Call, PluginBase.nppData._nppHandle)) * 2;
            StringBuilder stringBuilder = new StringBuilder(capacity);
            Win32Ext.GetWindowText(new HandleRef(dummyForWin32Call, PluginBase.nppData._nppHandle), stringBuilder, stringBuilder.Capacity);
            string windowTitle = stringBuilder.ToString();
            return windowTitle.Substring(0, windowTitle.IndexOf(" - Notepad++"));
        }
        internal static string runInCommandLine(string command) {
            try {
                //MessageBox.Show(command);
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = System.Diagnostics.Process.Start(procStartInfo);
                proc.WaitForExit();
                string strOutput = proc.StandardOutput.ReadToEnd();
                return strOutput;
            } catch (Exception e) {
                reportError("Exception while trying to run a command line command:\n'" + command
                    + "'\n" + e.ToString(), e);
                return null;
            }
        }

        internal static void placeOutput(string output) {
            SciMsg command;
            if (isAnythingSelected()) {
                //Replace selection with output
                command = SciMsg.SCI_REPLACESEL;
            } else {
                //Replace text in currently active text area with output
                command = SciMsg.SCI_SETTEXT;
            }
            Win32.SendMessage(PluginBase.GetCurrentScintilla(), command, 0, output);
        }

        internal static void writeInputToFile(string fileName) {
            string input = getInput();
            System.IO.StreamWriter writer = new System.IO.StreamWriter(fileName);
            writer.Write(input);
            writer.Close();
        }

        internal static string getInput() {
            string selection = getSelection();
            if (!isAnythingSelected(selection)) {
                //Use whole file
                int textLength = (int)Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GETLENGTH, 0, 0);
                StringBuilder sb = new StringBuilder(textLength);
                Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GETTEXT, textLength, sb);
                return sb.ToString();
            } else {
                return selection;
            }
        }

        internal static string getSelection() {
            StringBuilder sb = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GETSELTEXT, 0, sb);
            return sb.ToString();
        }

        internal static bool isAnythingSelected() {
            return isAnythingSelected(getSelection());
        }

        internal static bool isAnythingSelected(string selection) {
            return selection.Length > 0;
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
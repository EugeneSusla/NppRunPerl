using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;

//TODO Format menu prettier :)
//TODO Clean up c# code convention violations if any
//TODO Clean up template-generated code
namespace NppRunPerl
{
    class Main
    {
        #region " Fields "
        internal const string PluginName = "NppRunPerl";
        static string iniFilePath = null;
        static bool someSetting = false;
        static frmMyDlg frmMyDlg = null;
        static int idMyDlg = -1;
        static Bitmap tbBmp = Properties.Resources.star;
        static Bitmap tbBmp_tbTab = Properties.Resources.star_bmp;
        static Icon tbIcon = null;
        static Object dummyForWin32Call = new Object();

        internal const string SCRIPT_ROOT_FOLDER = "plugins\\" + PluginName + "\\";
        internal const string SCRIPT_FILE_NAME = "runperl_script.pl";
        internal const string SCRIPT_OUTPUT_FILE_NAME = "runperl_output.txt";
        internal const string SCRIPT_INPUT_FILE_NAME = "runperl_input.txt";
        internal const string SCRIPT_LAUNCHER_FILE_NAME = "runperl_launcher.bat";
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

            int index = 0;
            PluginBase.SetCommand(index++, "DEBUG", myMenuFunction, new ShortcutKey(false, false, false, Keys.None));
            PluginBase.SetCommand(index++, "Open script", openScript, new ShortcutKey(false, false, false, Keys.None));
            PluginBase.SetCommand(index++, "Run script on selection/file", runScript, new ShortcutKey(false, false, false, Keys.None));
            PluginBase.SetCommand(index++, "Open/Run script", openOrRunScript, new ShortcutKey(false, true, true, Keys.P));
            PluginBase.SetCommand(index++, "Run selection in cmd", runInCommandLine, new ShortcutKey(false, false, false, Keys.None));
            PluginBase.SetCommand(index++, "Run selection in cmd (echo off)", runInCommandLineEchoOff, new ShortcutKey(false, true, true, Keys.C));
            PluginBase.SetCommand(index++, "Configure launcher", openLauncher, new ShortcutKey(false, false, false, Keys.None));
            //PluginBase.SetCommand(index++, "Show Dockable Dialog", myDockableDialog); 
            idMyDlg = 1;
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

        internal static void myMenuFunction() {
            MessageBox.Show("Hello world");
            //openOrRunScript();
        }
        internal static void openScript() {
            openScriptIfNotYetOpen();
        }
        //TODO refactor : extract methods
        internal static void runScript() {
            try {
                //Get input
                string inputFileName = SCRIPT_ROOT_FOLDER + SCRIPT_INPUT_FILE_NAME;
                writeInputToFile(inputFileName);

                //Run perl script on input
                string command = SCRIPT_ROOT_FOLDER + SCRIPT_LAUNCHER_FILE_NAME + " "
                    + SCRIPT_INPUT_FILE_NAME + " "
                    + SCRIPT_FILE_NAME + " "
                    + SCRIPT_OUTPUT_FILE_NAME;
                CommandLineOutput commandLineOutput = runInCommandLine(command);

                //Output results
                if (!commandLineOutput.ErrorsPresent()) {
                    try {
                        string outputFilePath = SCRIPT_ROOT_FOLDER + SCRIPT_OUTPUT_FILE_NAME;
                        if (File.Exists(outputFilePath)) {
                            System.IO.StreamReader outputFile = new System.IO.StreamReader(outputFilePath);
                            string output = outputFile.ReadToEnd();
                            outputFile.Close();
                            //Comment these lines for additional debug info
                            File.Delete(outputFilePath);
                            File.Delete(inputFileName);
                            placeOutput(output);
                        }
                        else {
                            reportError("No output file found");
                        }
                    }
                    catch (Exception e) {
                        reportError(e.ToString(), e);
                    }
                }
            } catch (Exception e) {
                reportError(e.ToString(), e);
            }
        }
        internal static void openOrRunScript() {
                if (!openScriptIfNotYetOpen()) {
                    runScript();
                }
        }
        internal static void runInCommandLine() {
            runCommandLineScript(false);
        }
        internal static void runInCommandLineEchoOff() {
            runCommandLineScript(true);
        }
        internal static void openLauncher() {
            openFile(SCRIPT_ROOT_FOLDER + SCRIPT_LAUNCHER_FILE_NAME);
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

        internal static bool checkIfScriptIsOpen(string fileName) {
            bool result = false;
            foreach (string file in getOpenFiles()) {
                if (file.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)) {
                    result = true;
                }
            }
            return result;
        }

        internal static bool openScriptIfNotYetOpen() {
            string scriptFileName = SCRIPT_ROOT_FOLDER + SCRIPT_FILE_NAME;
            if (!checkIfScriptIsOpen(scriptFileName)) {
                //Open script file
                openFile(scriptFileName);
                //Move it to other view
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_VIEW_GOTO_ANOTHER_VIEW);
                return true;
            }
            return false;
        }

        internal static void openFile(string fileName) {
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DOOPEN, 0, fileName);
        }

        internal static string getCurrentFileDirectoryName() {
            StringBuilder fileName = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTDIRECTORY, Win32.MAX_PATH, fileName);
            return fileName.ToString();
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

        internal static void runCommandLineScript(bool echoOff) {
            //string command = getInput();
            string commandScriptFile = SCRIPT_ROOT_FOLDER
                + SCRIPT_INPUT_FILE_NAME.Substring(0, SCRIPT_INPUT_FILE_NAME.LastIndexOf('.')) + ".bat";
            StringBuilder prefix = new StringBuilder();
            if (echoOff) {
                prefix.Append("@echo off\n");
            }
            prefix.Append("cd /d " + getCurrentFileDirectoryName() + "\n");
            writeInputToFile(commandScriptFile, prefix.ToString(), String.Empty);
            CommandLineOutput output = runInCommandLine(commandScriptFile, false);
            MessageBox.Show(output.ToString());
        }

        internal static CommandLineOutput runInCommandLine(string command) {
            return runInCommandLine(command, true);
        }

        internal static CommandLineOutput runInCommandLine(string command, bool doReportError) {
            try {
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.RedirectStandardError = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = System.Diagnostics.Process.Start(procStartInfo);
                string strOutput = proc.StandardOutput.ReadToEnd();
                string strError = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                CommandLineOutput result = new CommandLineOutput(strOutput, strError);
                if (doReportError && result.ErrorsPresent()) {
                    reportError(strError);
                }
                return result;
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
            writeInputToFile(fileName, String.Empty, String.Empty);
        }

        internal static void writeInputToFile(string fileName, string prefix, string suffix) {
            string input = getInput();
            System.IO.StreamWriter writer = new System.IO.StreamWriter(fileName);
            writer.Write(prefix);
            writer.Write(input);
            writer.Write(suffix);
            writer.Close();
        }

        internal static string getInput() {
            string selection = getSelection();
            if (!isAnythingSelected(selection)) {
                //Use whole file
                int textLength = (int)Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GETLENGTH, 0, 0) + 1;
                StringBuilder sb = new StringBuilder(textLength);
                Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GETTEXT, textLength, sb);
                return sb.ToString();
            } else {
                //Use selected fragment
                return selection;
            }
        }

        internal static string getSelection() {
            int selectionSize = (int)Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GETSELTEXT, 0, 0); ;
            StringBuilder sb = new StringBuilder(selectionSize);
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
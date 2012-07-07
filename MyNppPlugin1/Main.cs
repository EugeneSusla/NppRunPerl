using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;


namespace NppRunPerl
{
    class Main
    {
        #region " Fields "
        internal const string PluginName = "NppRunPerl";
        internal const string SCRIPT_ROOT_FOLDER = "plugins\\" + PluginName + "\\";
        internal const string SCRIPT_FILE_NAME = "runperl_script.pl";
        internal const string SCRIPT_OUTPUT_FILE_NAME = "runperl_output.txt";
        internal const string SCRIPT_INPUT_FILE_NAME = "runperl_input.txt";
        internal const string SCRIPT_LAUNCHER_FILE_NAME = "runperl_launcher.bat";
        #endregion

        #region " StartUp/CleanUp "
        internal static void CommandMenuInit() {
            int index = 0;
            PluginBase.SetCommand(index++, "Open script", openScript, new ShortcutKey(false, false, false, Keys.None));
            PluginBase.SetCommand(index++, "Run script on selection/file", runScript, new ShortcutKey(false, false, false, Keys.None));
            PluginBase.SetCommand(index++, "Open/Run script", openOrRunScript, new ShortcutKey(false, true, true, Keys.P));
            PluginBase.SetCommand(index++, "---", null);
            PluginBase.SetCommand(index++, "Run selection in cmd", runInCommandLine, new ShortcutKey(false, false, false, Keys.None));
            PluginBase.SetCommand(index++, "Run selection in cmd (echo off)", runInCommandLineEchoOff, new ShortcutKey(false, true, true, Keys.C));
            PluginBase.SetCommand(index++, "---", null);
            PluginBase.SetCommand(index++, "Configure launcher", openLauncher, new ShortcutKey(false, false, false, Keys.None));
        }
        internal static void PluginCleanUp() {}
        #endregion

        #region " Menu functions "

        internal static void openScript() {
            openScriptIfNotYetOpen();
        }
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

    }
}
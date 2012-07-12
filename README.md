NppRunPerl
==========

A Notepad++ plugin to comfortably create and run a perl script to transform the current file or selection. 
-----------------------------------------------------------------------------------------------------------

The user is prompted to implement a perl function(sub) (process : string -> string)
That function will be run on each line of selected text(or whole file, if nothing is selected), and the output will replace the input.

- You need perl installed to use this plugin.
	+ 'perl -v' to see whether you have perl installed.
	+ If not, install perl for windows, e. g. [Strawberry perl](http://strawberryperl.com)
- Copy all the files from bin directory/archive to your notepad++'s plugin folder.
	+ Be sure to use the unicode version of notepad++.
- (Re)start notepad++
- Use Alt+Shift+P to open the perl script, and (once script is open) use same shortcut to run it on the currently active file.
	+ Selecting a fragment before running the script will cause the script to be run on selected fragment only.
	+ More actions/configuration is available in the dedicated 'Plugins' menu entry.
- Additionally you can use Alt+Shift+C to run selected fragment/whole file as a command line script.
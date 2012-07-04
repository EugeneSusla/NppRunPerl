
;makeRelease()
;deploy()

$outputFolderName = "bin"
$inputFolderName = "c:\\users\\eugene\\documents\\visual studio 2010\\Projects\\MyNppPlugin1\\MyNppPlugin1\\bin\\Release"
$pluginsFolderName = "d:\\Program Files\\Notepad++\\unicode\\plugins\\"

Func makeRelease()
	WinClose("Notepad++")
	WinActivate("Visual Studio")
	WinWaitActive("Visual Studio")
	Send("{F7}")
	Sleep(2000)
	FileCopy($inputFolderName & "\\NppRunPerl.dll", $outputFolderName, 1)
	package()
EndFunc

Func package()
	Run(@ComSpec & " /c ""d:\Program Files\7zip\App\7-Zip64\7z"" a -tzip bin.zip bin\*")
EndFunc

Func deploy()
	DirCopy($outputFolderName, $pluginsFolderName, 1)
EndFunc
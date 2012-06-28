
HotKeySet("{F8}", "compileRun")
HotKeySet("{F10}", "try")
Opt("WinTitleMatchMode", 2)	;match windows by title substring


Func compileRun()
	WinClose("Notepad++")
	WinActivate("Visual Studio")
	Send("{F7}")
	Sleep(2000)
	FileCopy("c:\\users\\eugene\\documents\\visual studio 2010\\Projects\\MyNppPlugin1\\MyNppPlugin1\\bin\\Release\\MyNppPlugin1.dll", "d:\\Program Files\\Notepad++\\unicode\\plugins\\", 1)
	Run("notepad++.exe")
	WinWaitActive("Notepad++")
	try()
EndFunc

Func try()
	Send("!P")
	Send("M")
	Send("D")
EndFunc

while (1)
	Sleep(10)
WEnd
;   Visual studio's post-build event seems to fire off prematurely resulting in an invalid dll being copied over.
; That's why I use this little script instead.
; Run with AutoIT.

#include "makeRelease.au3"

HotKeySet("{F8}", "compileRun")
HotKeySet("{F10}", "try")
Opt("WinTitleMatchMode", 2)	;match windows by title substring


Func compileRun()
	makeRelease()
	deploy()
	Run("notepad++.exe")
	WinWaitActive("Notepad++")
	try()
EndFunc

Func try()
	Send("+!P")
EndFunc

while (1)
	Sleep(10)
WEnd
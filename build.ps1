dotnet build  .\HumbleLibrary.csproj
if ($?) {
	Stop-Process -Name "Playnite.DesktopApp" -Force
	if ($?) {
		Start-Sleep 1
	}
	if ($?) {
		Copy-Item -Path ".\bin\Debug\net462\HumbleLibrary.dll" -Destination "C:\Users\Carsten Schipmann\AppData\Roaming\Playnite\Extensions\humble"
		if ($?) {
			Start-Process -FilePath "C:\Program Files (x86)\Playnite\Playnite.DesktopApp.exe"
		}
	}
	
}
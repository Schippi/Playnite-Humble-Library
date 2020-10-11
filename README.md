# Playnite-Humble-Library
Humble Bundle Library Plugin
# dotnet build  .\HumbleBundle.csproj; Stop-Process -Name "Playnite.DesktopApp" -Force; Start-Sleep 1; Copy-Item -Path ".\bin\Debug\net462\HumbleLibrary.dll" -Destination "C:\Users\Carsten Schipmann\AppData\Roaming\Playnite\Extensions\humble"
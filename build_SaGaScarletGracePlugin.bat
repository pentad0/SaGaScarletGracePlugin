echo off

set gameDir="F:\SteamLibrary\steamapps\common\SaGaSCARLETGRACE"
set gameExe="SaGaSCARLETGRACE.exe"

set pluginName="SaGaScarletGracePlugin"

echo on

dotnet build
copy ".\bin\Debug\net35\%pluginName%.dll" "%gameDir:~1,-1%\BepInEx\plugins"
"%gameDir:~1,-1%\%gameExe%"

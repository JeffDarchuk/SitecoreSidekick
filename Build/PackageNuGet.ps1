param($scriptRoot)

$ErrorActionPreference = "Stop"

$programFilesx86 = ${Env:ProgramFiles(x86)}
$msBuild = "$programFilesx86\MSBuild\14.0\bin\msbuild.exe"
$nuGet = "$scriptRoot..\tools\NuGet.exe"
$solution = "$scriptRoot\..\SitecoreSidekick.sln"

& $nuGet restore $solution
& $msBuild $solution /p:Configuration=Release /t:Rebuild /m

$tmAssembly = Get-Item "$scriptRoot\..\Source\SitecoreSidekick\bin\Release\SitecoreSidekick.dll" | Select-Object -ExpandProperty VersionInfo
$targetAssemblyVersion = $tmAssembly.ProductVersion

& $nuGet pack "$scriptRoot\SitecoreSidekick.nuget\SitecoreSidekick.nuspec" -version $targetAssemblyVersion

& $nuGet pack "$scriptRoot\..\Source\SitecoreSidekick\SitecoreSidekick.csproj" -Symbols -Prop "Configuration=Release"
& $nuGet pack "$scriptRoot\..\ScsAuditLog\ScsAuditLog.csproj" -Symbols -Prop "Configuration=Release"
& $nuGet pack "$scriptRoot\..\ScsEditingContext\ScsEditingContext.csproj" -Symbols -Prop "Configuration=Release"
& $nuGet pack "$scriptRoot\..\ContentMigrator\ScsContentMigrator.csproj" -Symbols -Prop "Configuration=Release"
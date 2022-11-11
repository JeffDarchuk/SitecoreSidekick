param($scriptRoot)

$ErrorActionPreference = "Stop"

function Resolve-MsBuild {
	$msb2022 = Resolve-Path "${env:ProgramFiles}\Microsoft Visual Studio\2022\*\MSBuild\*\bin\msbuild.exe" -ErrorAction SilentlyContinue
	if($msb2022) {
		Write-Host "Found MSBuild 2022 (or later)."
		Write-Host $msb2022
		return $msb2022
	}
	$msb2019 = Resolve-Path "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\*\MSBuild\*\bin\msbuild.exe" -ErrorAction SilentlyContinue
	if($msb2019) {
		Write-Host "Found MSBuild 2019 (or later)."
		Write-Host $msb2019
		return $msb2019
	}

	$msBuild2015 = "${env:ProgramFiles(x86)}\MSBuild\14.0\bin\msbuild.exe"

	if(-not (Test-Path $msBuild2015)) {
		throw 'Could not find MSBuild 2015 or later.'
	}

	Write-Host "Found MSBuild 2015."
	Write-Host $msBuild2015

	return $msBuild2015
}

$msBuild = Resolve-MsBuild
$nuGet = "$scriptRoot..\tools\NuGet.exe"
$solution = "$scriptRoot\..\SitecoreSidekick.sln"

& $nuGet restore $solution
& $msBuild $solution /p:Configuration=Release /t:Rebuild /m

$tmAssembly = Get-Item "$scriptRoot\..\Source\SitecoreSidekick\bin\Release\Sidekick.Core.dll" | Select-Object -ExpandProperty VersionInfo
$targetAssemblyVersion = $tmAssembly.ProductVersion

& $nuGet pack "$scriptRoot\SitecoreSidekick.nuget\SitecoreSidekick.nuspec" -version $targetAssemblyVersion

& $nuGet pack "$scriptRoot\..\Source\SitecoreSidekick\Sidekick.Core.csproj" -Symbols -Prop "Configuration=Release" -version $targetAssemblyVersion -Properties id=SitecoreSidekickCore
& $nuGet pack "$scriptRoot\..\ScsAuditLog\Sidekick.AuditLog.csproj" -Symbols -Prop "Configuration=Release" -version $targetAssemblyVersion -Properties id=SitecoreSidekickAuditLog
& $nuGet pack "$scriptRoot\..\ScsEditingContext\Sidekick.EditingContext.csproj" -Symbols -Prop "Configuration=Release" -version $targetAssemblyVersion -Properties id=SitecoreSidekickEditingContext
& $nuGet pack "$scriptRoot\..\ContentMigrator\Sidekick.ContentMigrator.csproj" -Symbols -Prop "Configuration=Release" -version $targetAssemblyVersion -Properties id=SitecoreSidekickContentMigrator
& $nuGet pack "$scriptRoot\..\ScsSitecoreResourceManager\Sidekick.SitecoreResourceManager.csproj" -Symbols -Prop "Configuration=Release" -version $targetAssemblyVersion -Properties id=SitecoreSidekickResourceManager
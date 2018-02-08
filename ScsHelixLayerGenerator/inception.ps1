write-host "Desired namespace e.g. 'MySite.Sidekick.CoolApp'" -foregroundcolor "Cyan"
$Namespace = Read-Host 
write-host "Enter project name without spaces and camelcase e.g. 'CoolApp'" -foregroundcolor "Cyan"
$AppName = Read-Host 
write-host "Pretty human readable app name e.g. 'Cool App!'" -foregroundcolor "Cyan"
$HumanReadableName = Read-Host 
write-host "Two character key related to this module e.g. 'ca'" -foregroundcolor "Cyan"
$AppCode = Read-Host 

$ScriptPath = Split-Path $MyInvocation.MyCommand.Path

function Convert-SidekickFolder{
	param($path)
	foreach ($item in (Get-ChildItem $path -force)){
		if ($item -is [System.IO.DirectoryInfo] -or $item.FullName.EndsWith(".ps1") -or $item.FullName.EndsWIth(".cmd")){
			continue;
		}
		$contents = Get-Content $item.FullName
		if ($contents -like "*AppName*" -or $contents -like "*AppCode*" -or $contents -like "*HumanReadableName" -or $contents -like "*TargetNamespace*")
		{
			write-host ("[Modify] {0}" -f $item.FullName) -foregroundcolor "DarkGreen"
			Set-Content -Path $item.FullName -Value ($contents).replace("AppName", $AppName).replace("AppCode", $AppCode).replace("HumanReadableName", $HumanReadableName).replace("TargetNamespace", $Namespace)
		}
		$fileName = Split-Path $item -leaf
		if ($fileName -like "*AppName*"){
			write-host ("[Rename] {0}" -f $item.FullName) -foregroundcolor "Green"
			$fileName = $filename -replace "AppName", $AppName
			Rename-Item $item.FullName $fileName -Force
		}
		if ($fileName -like "*AppCode*"){
			$fileName = $filename -replace "AppCode", $AppCode
			Rename-Item $item.FullName $fileName -Force
		}
	}
}

Convert-SidekickFolder -path $ScriptPath
Convert-SidekickFolder -path "$ScriptPath\Resources"
Convert-SidekickFolder -path "$ScriptPath\Properties"

pause
$Webroot = "C:\inetpub\wwwroot\dh9.local"

Get-ChildItem "$PSScriptRoot\" -recurse -filter *.config -File | Foreach-Object{
	$fileName = Split-Path $_.FullName -leaf
	if (-Not $_.FullName.Contains("\doc\") -and $fileName.StartsWith("zScs", "CurrentCultureIgnoreCase") -and $fileName.EndsWith(".config", "CurrentCultureIgnoreCase")){
		Write-Host "moving $($_.FullName) to config root" -ForegroundColor Green
		Copy-Item $_.FullName "$(New-Item "$Webroot\App_Config\Include\SidekickTest" -ItemType directory -Force)/$fileName" -Force
	}
}
Get-ChildItem "$PSScriptRoot\bin" | Foreach-Object{
	Write-Host "moving $($_.FullName) to bin root" -ForegroundColor Green
	Copy-Item $_.FullName "$Webroot\bin\$(Split-Path $_.FullName -Leaf)" -Force
}
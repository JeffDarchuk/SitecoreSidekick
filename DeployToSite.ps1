$Webroot = "D:\Clients\Lincare\LinRoot\Docker\Containers\ltsc2019\sitecore-xp0\site-data"
docker exec lincare_cm_1 iisreset
start-sleep -seconds 10
# Get-ChildItem "$PSScriptRoot\" -recurse -filter *.config -File | Foreach-Object{
	# $fileName = Split-Path $_.FullName -leaf
	# if (-Not $_.FullName.Contains("\doc\") -and $fileName.StartsWith("Sidekick", "CurrentCultureIgnoreCase") -and $fileName.EndsWith(".config", "CurrentCultureIgnoreCase") -and -not $fileName.EndsWith(".dll.config", "CurrentCultureIgnoreCase")){
		# Write-Host "moving $($_.FullName) to config root" -ForegroundColor Green
		# Copy-Item $_.FullName "$(New-Item "$Webroot\App_Config\Include\SidekickTest" -ItemType directory -Force)/$fileName" -Force
	# }
# }
Get-ChildItem "$PSScriptRoot\bin" | Foreach-Object{
	Write-Host "moving $($_.FullName) to bin root" -ForegroundColor Green
	if ($_.Name.StartsWith("Sidekick") -or $_.Name.StartsWith("SitecoreSidekick") -or -not (Test-Path "$Webroot\bin\$(Split-Path $_.FullName -Leaf)")){
		Copy-Item $_.FullName "$Webroot\bin\$(Split-Path $_.FullName -Leaf)" -Force
	}
}

# $Webroot = "C:\inetpub\wwwroot\demo2.local"

# Get-ChildItem "$PSScriptRoot\" -recurse -filter *.config -File | Foreach-Object{
	# $fileName = Split-Path $_.FullName -leaf
	# if (-Not $_.FullName.Contains("\doc\") -and $fileName.StartsWith("Sidekick", "CurrentCultureIgnoreCase") -and $fileName.EndsWith(".config", "CurrentCultureIgnoreCase") -and -not $fileName.EndsWith(".dll.config", "CurrentCultureIgnoreCase")){
		# Write-Host "moving $($_.FullName) to config root" -ForegroundColor Green
		# Copy-Item $_.FullName "$(New-Item "$Webroot\App_Config\Include\SidekickTest" -ItemType directory -Force)/$fileName" -Force
	# }
# }
# Get-ChildItem "$PSScriptRoot\bin" | Foreach-Object{
	# Write-Host "moving $($_.FullName) to bin root" -ForegroundColor Green
	# if ($_.Name.StartsWith("Sidekick") -or $_.Name.StartsWith("SitecoreSidekick") -or -not (Test-Path "$Webroot\bin\$(Split-Path $_.FullName -Leaf)")){
		# Copy-Item $_.FullName "$Webroot\bin\$(Split-Path $_.FullName -Leaf)" -Force
	# }
# }

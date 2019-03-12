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

 $webroot = "c:\inetpub\wwwroot\dh9-t.local"

 get-childitem "$psscriptroot\" -recurse -filter *.config -file | foreach-object{
	 $filename = split-path $_.fullname -leaf
	 if (-not $_.fullname.contains("\doc\") -and $filename.startswith("zscs", "currentcultureignorecase") -and $filename.endswith(".config", "currentcultureignorecase")){
		 write-host "moving $($_.fullname) to config root" -foregroundcolor green
		 copy-item $_.fullname "$(new-item "$webroot\app_config\include\sidekicktest" -itemtype directory -force)/$filename" -force
	 }
 }
 get-childitem "$psscriptroot\bin" | foreach-object{
	 write-host "moving $($_.fullname) to bin root" -foregroundcolor green
	 copy-item $_.fullname "$webroot\bin\$(split-path $_.fullname -leaf)" -force
 }


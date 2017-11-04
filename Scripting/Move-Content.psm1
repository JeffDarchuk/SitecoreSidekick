$ErrorActionPreference = 'Stop'
$ScriptPath = Split-Path $MyInvocation.MyCommand.Path
$MicroCHAP = $ScriptPath + '\MicroCHAP.dll'
Add-Type -Path $MicroCHAP

Function Move-Content {
	Param(
		[Parameter(Mandatory=$True)]
		[string]$LocalUrl,
		
		[Parameter(Mandatory=$True)]
		[string]$RemoteUrl,

		[Parameter(Mandatory=$True)]
		[string]$SharedSecret,

		[Parameter(Mandatory=$True)]
		[string[]]$RootIds,
		
		[string]$Database = 'master',

		[switch]$Children,

		[switch]$Overwrite,
		
		[switch]$PullParent,
		
		[switch]$RemoveLocalNotInRemote,
		
		[switch]$EventDisabler,
		
		[switch]$BulkUpdate
	)
	$idsJson = [string]::Join('"]["', $RootIds)
	$requestPayload = @"
	{
		"Ids" : ["$idsJson"],
		"Database" : "$Database",
		"Server" : "$RemoteUrl",
		"Children" : $Children,
		"Overwrite" : $Overwrite,
		"PullParent" : $PullParent,
		"RemoveLocalNotInRemote" : $RemoveLocalNotInRemote,
		"EventDisabler" : $EventDisabler,
		"BulkUpdate" : $BulkUpdate
	}
"@
	$requestPayload = $requestPayload -replace "True", "true" 
	$requestPayload = $requestPayload -replace "False", "false"
	$url = "{0}/scs/cm/cmstartoperation.scsvc" -f $LocalUrl

	# GET AN AUTH CHALLENGE
	$challenge = [guid]::NewGuid().ToString()
	
	# CREATE A SIGNATURE WITH THE SHARED SECRET AND CHALLENGE
	$signatureService = New-Object MicroCHAP.SignatureService -ArgumentList $SharedSecret
	$paramz = New-Object MicroCHAP.SignatureFactor[] 1
	$paramz[0] = New-Object MicroCHAP.SignatureFactor -ArgumentList "payload", $requestPayload
	$signature = $signatureService.CreateSignature($challenge, $url, $paramz)

	Write-Host "Move-Content - Initializing transfer from $RemoteUrl to $LocalUrl"

	# USING THE SIGNATURE, EXECUTE CONTENT MIGRATOR
	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
	$result = Invoke-StreamingWebRequest -Uri $url -Mac $signature.SignatureHash -Nonce $challenge -Parameters $requestPayload

	if($result.TrimEnd().EndsWith('****ERROR OCCURRED****')) {
		throw "Content migration from $ServerUrl to $LocalUrl returned an error. See the preceding log for details."
		Write-Host "Move-Content - Completed with errors"
	}else{
		Write-Host "Move-Content - Initialized, to view progress visit $LocalUrl and open Sitecore Sidekick's content migrator."
	}
}

Function Invoke-StreamingWebRequest($Uri, $MAC, $Nonce, $Parameters) {
	$responseText = new-object -TypeName "System.Text.StringBuilder"
	$request = [System.Net.WebRequest]::Create($Uri)
	$request.Method = "POST"
	$request.Headers["X-MC-MAC"] = $MAC
	$request.Headers["X-MC-Nonce"] = $Nonce
	$request.Timeout = 10800000
	$stream = $request.GetRequestStream()
	$stream.Write([System.Text.Encoding]::ASCII.GetBytes($Parameters), 0, $Parameters.Length)
	$stream.Close()

	$response = $request.GetResponse()
	$responseStream = $response.GetResponseStream()
	$responseStreamReader = new-object System.IO.StreamReader $responseStream
	return $responseText.ToString()
}

Export-ModuleMember -Function Move-Content
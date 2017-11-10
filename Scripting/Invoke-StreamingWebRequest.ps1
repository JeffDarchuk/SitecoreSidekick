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
	
	while(-not $responseStreamReader.EndOfStream) {
		$line = $responseStreamReader.ReadLine()

		if($line.StartsWith('Error:')) {
			Write-Host $line.Substring(7) -ForegroundColor Red
		}
		elseif($line.StartsWith('Warning:')) {
			Write-Host $line.Substring(9) -ForegroundColor Yellow
		}
		elseif($line.StartsWith('Debug:')) {
			Write-Host $line.Substring(7) -ForegroundColor Gray
		}
		elseif($line.StartsWith('Info:')) {
			Write-Host $line.Substring(6) -ForegroundColor White
		}
		else {
			Write-Host $line -ForegroundColor White
		}

		[void]$responseText.AppendLine($line)
	}

	return $responseText.ToString()
}
param(
    [string]$GateHost = '192.168.178.34',
    [int]$Port = 9050
)

function Send-Recv($stream, $json) {
    $b = [Text.Encoding]::UTF8.GetBytes($json + "`r`n`r`n")
    $stream.Write($b, 0, $b.Length)
    Start-Sleep -Milliseconds 800
    $r = New-Object byte[] 131072
    $n = 0
    if ($stream.DataAvailable) { $n = $stream.Read($r, 0, 131072) }
    $resp = [Text.Encoding]::UTF8.GetString($r, 0, $n)
    Write-Host ">> $json"
    Write-Host "<< $resp"
    Write-Host ""
    return $resp
}

$t = New-Object Net.Sockets.TcpClient($GateHost, $Port)
$s = $t.GetStream()

Send-Recv $s '{"CMD":"IBN_PROGRAM_REQ","PROTOCOL":"0.03","TIMESTAMP":"1421948265","NUMBER":40,"PWD":"0000"}'
Start-Sleep -Milliseconds 500
Send-Recv $s '{"CMD":"IBN_LIST_EDIT_REQ","PROTOCOL":"0.03","TIMESTAMP":"1421948265","NUMBER":7,"NAME":"Pauls Zimmer","ICON":34,"VISIBLE":1,"ITEMS":[25]}'
Start-Sleep -Milliseconds 500
Send-Recv $s '{"CMD":"IBN_DONE_REQ","PROTOCOL":"0.03","TIMESTAMP":"1421948265","NUMBER":40}'

$t.Close()

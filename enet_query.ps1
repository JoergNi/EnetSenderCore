param(
    [Parameter(Mandatory=$true)]
    [string]$Cmd,
    [string]$GateHost = '192.168.178.34',
    [int]$Port = 9050,
    [int]$WaitMs = 2000,
    [int]$Channel = -1   # if set, sends SIGN_IN (x2) before and SIGN_OUT after (required for ITEM_VALUE_SET)
)
# Usage examples:
#   .\enet_query.ps1 '{"CMD":"PROJECT_LIST_GET","PROTOCOL":"0.03","TIMESTAMP":"1421948265"}'
#   .\enet_query.ps1 '{"CMD":"BLOCK_RESTORE_REQ","PROTOCOL":"0.03","TIMESTAMP":"1421948265","DATA-NUM":5}'
#   .\enet_query.ps1 '{"CMD":"ITEM_VALUE_SET","PROTOCOL":"0.03","TIMESTAMP":"1421948265","VALUES":[{"NUMBER":18,"STATE":"VALUE_BLINDS","VALUE":50}]}' -Channel 18

[Console]::OutputEncoding = [Text.Encoding]::UTF8

function Send-Msg($stream, $json) {
    $b = [Text.Encoding]::UTF8.GetBytes($json + "`r`n`r`n")
    $stream.Write($b, 0, $b.Length)
    Start-Sleep -Milliseconds 300
    $r = New-Object byte[] 131072
    if ($stream.DataAvailable) { $n = $stream.Read($r, 0, 131072) }
}

$t = New-Object Net.Sockets.TcpClient($GateHost, $Port)
$s = $t.GetStream()

if ($Channel -ge 0) {
    $signIn = '{"CMD":"ITEM_VALUE_SIGN_IN_REQ","PROTOCOL":"0.03","TIMESTAMP":"1421948265","ITEMS":[' + $Channel + ']}'
    Send-Msg $s $signIn
    Send-Msg $s $signIn
}

$msg = $Cmd + "`r`n`r`n"
$b = [Text.Encoding]::UTF8.GetBytes($msg)
$s.Write($b, 0, $b.Length)
Start-Sleep -Milliseconds $WaitMs
$r = New-Object byte[] 131072
$n = $s.Read($r, 0, 131072)
[Text.Encoding]::UTF8.GetString($r, 0, $n)

if ($Channel -ge 0) {
    $signOut = '{"CMD":"ITEM_VALUE_SIGN_OUT_REQ","PROTOCOL":"0.03","TIMESTAMP":"1421948265","ITEMS":[' + $Channel + ']}'
    Send-Msg $s $signOut
}

$t.Close()

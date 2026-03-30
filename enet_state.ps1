param(
    [Parameter(Mandatory=$true)]
    [string]$Channels,   # comma-separated, e.g. "17,18"
    [string]$GateHost = '192.168.178.34',
    [int]$Port = 9050
)
[Console]::OutputEncoding = [Text.Encoding]::UTF8

foreach ($ch in ($Channels -split ',')) {
    $t = New-Object Net.Sockets.TcpClient($GateHost, $Port)
    $s = $t.GetStream()

    $signIn = '{"CMD":"ITEM_VALUE_SIGN_IN_REQ","PROTOCOL":"0.03","TIMESTAMP":"1421948265","ITEMS":[' + $ch + ']}'
    $b = [Text.Encoding]::UTF8.GetBytes($signIn + "`r`n`r`n")
    $s.Write($b, 0, $b.Length)
    $s.Write($b, 0, $b.Length)

    Start-Sleep -Milliseconds 2000
    $r = New-Object byte[] 131072
    $n = $s.Read($r, 0, 131072)
    Write-Host "=== Channel $ch ==="
    [Text.Encoding]::UTF8.GetString($r, 0, $n)

    $signOut = '{"CMD":"ITEM_VALUE_SIGN_OUT_REQ","PROTOCOL":"0.03","TIMESTAMP":"1421948265","ITEMS":[' + $ch + ']}'
    $b2 = [Text.Encoding]::UTF8.GetBytes($signOut + "`r`n`r`n")
    $s.Write($b2, 0, $b2.Length)
    $t.Close()
    Start-Sleep -Milliseconds 3000
}

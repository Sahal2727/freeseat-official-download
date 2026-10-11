$lines = Get-Content "C:\Users\DC\.cline\data\workspaces\chat\FreeSeat\src\FreeSeat.Setup\FreeSeat.iss"
$seen = @{}
$newLines = @()
foreach ($line in $lines) {
    if ($line -match 'FreeSeat.App.dll.*DestDir.*\{app\}.*Flags: ignoreversion') {
        if (-not $seen['dll']) {
            $newLines += $line
            $seen['dll'] = $true
        }
    } else {
        $newLines += $line
    }
}
$newLines | Set-Content "C:\Users\DC\.cline\data\workspaces\chat\FreeSeat\src\FreeSeat.Setup\FreeSeat.iss"
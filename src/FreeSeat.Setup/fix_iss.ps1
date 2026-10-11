$lines = Get-Content "C:\Users\DC\.cline\data\workspaces\chat\FreeSeat\src\FreeSeat.Setup\FreeSeat.iss"
$newLines = @()
foreach ($line in $lines) {
    $newLines += $line
    if ($line -match 'FreeSeat.App.exe.*DestDir.*\{app\}.*Flags: ignoreversion') {
        $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\FreeSeat.App.dll"; DestDir: "{app}"; Flags: ignoreversion'
    }
}
$newLines | Set-Content "C:\Users\DC\.cline\data\workspaces\chat\FreeSeat\src\FreeSeat.Setup\FreeSeat.iss"
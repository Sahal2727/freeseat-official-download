$lines = Get-Content "C:\Users\DC\.cline\data\workspaces\chat\FreeSeat\src\FreeSeat.Setup\FreeSeat.iss"
$newLines = @()
$inFilesSection = $false
$filesSectionDone = $false

foreach ($line in $lines) {
    if ($line -eq '[Files]') {
        $inFilesSection = $true
        $newLines += $line
        continue
    }
    
    if ($inFilesSection -and $line -like '[*]' -and $line -ne '[Files]') {
        # End of Files section - write our clean files section
        if (-not $filesSectionDone) {
            $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\FreeSeat.App.exe"; DestDir: "{app}"; Flags: ignoreversion'
            $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\FreeSeat.App.dll"; DestDir: "{app}"; Flags: ignoreversion'
            $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\FreeSeat.App.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion'
            $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\FreeSeat.App.deps.json"; DestDir: "{app}"; Flags: ignoreversion'
            $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\FreeSeat.Core.dll"; DestDir: "{app}"; Flags: ignoreversion'
            $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\FreeSeat.Infrastructure.dll"; DestDir: "{app}"; Flags: ignoreversion'
            $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\CommunityToolkit.Mvvm.dll"; DestDir: "{app}"; Flags: ignoreversion'
            $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\Microsoft.Extensions.*.dll"; DestDir: "{app}"; Flags: ignoreversion'
            $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\System.Management.dll"; DestDir: "{app}"; Flags: ignoreversion'
            $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion'
            $filesSectionDone = $true
        }
        $inFilesSection = $false
    }
    
    if (-not $inFilesSection) {
        $newLines += $line
    }
}

# If we never hit the next section, add files at end
if ($inFilesSection -and -not $filesSectionDone) {
    $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\FreeSeat.App.exe"; DestDir: "{app}"; Flags: ignoreversion'
    $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\FreeSeat.App.dll"; DestDir: "{app}"; Flags: ignoreversion'
    $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\FreeSeat.App.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion'
    $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\FreeSeat.App.deps.json"; DestDir: "{app}"; Flags: ignoreversion'
    $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\FreeSeat.Core.dll"; DestDir: "{app}"; Flags: ignoreversion'
    $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\FreeSeat.Infrastructure.dll"; DestDir: "{app}"; Flags: ignoreversion'
    $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\CommunityToolkit.Mvvm.dll"; DestDir: "{app}"; Flags: ignoreversion'
    $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\Microsoft.Extensions.*.dll"; DestDir: "{app}"; Flags: ignoreversion'
    $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\System.Management.dll"; DestDir: "{app}"; Flags: ignoreversion'
    $newLines += 'Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion'
}

$newLines | Set-Content "C:\Users\DC\.cline\data\workspaces\chat\FreeSeat\src\FreeSeat.Setup\FreeSeat.iss"
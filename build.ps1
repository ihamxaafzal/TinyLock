$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.8 compiler is required.' }
Push-Location $PSScriptRoot
try {
    & $compiler /nologo /target:winexe /optimize+ /out:lock.exe /win32icon:Script\icon123.ico /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Security.dll Script\TinyLock.cs
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    Write-Output 'Built lock.exe successfully.'
} finally { Pop-Location }


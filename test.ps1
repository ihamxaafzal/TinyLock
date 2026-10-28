$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
    New-Item -ItemType Directory -Force bin | Out-Null
    & $compiler /nologo /target:exe /optimize+ /out:bin\TinyLock.Tests.exe /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Security.dll Script\TinyLock.cs
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
    & .\bin\TinyLock.Tests.exe --self-test
    if ($LASTEXITCODE -ne 0) { throw 'Validation failed.' }
} finally { Pop-Location }


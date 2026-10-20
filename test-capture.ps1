$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not (Test-Path -LiteralPath $compiler)) { $compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$testFolder=Join-Path $PSScriptRoot 'tests\results'
New-Item -ItemType Directory -Force -Path $testFolder | Out-Null
$testExe=Join-Path $testFolder 'CaptureSmoke.exe'
& $compiler /nologo /target:exe /reference:System.Drawing.dll ('/out:'+$testExe) (Join-Path $PSScriptRoot 'tests\CaptureSmoke.cs')
if($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o teste de captura.' }
& $testExe (Join-Path $PSScriptRoot 'dist\HeavyShot.exe')
if($LASTEXITCODE -ne 0) { throw 'A captura do desktop não passou na validação.' }

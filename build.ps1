param([switch]$Test,[switch]$Package)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not (Test-Path -LiteralPath $compiler)) { $compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if(-not (Test-Path -LiteralPath $compiler)) { throw 'Compilador .NET Framework não encontrado.' }
$dist=Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
if(-not (Test-Path -LiteralPath (Join-Path $root 'assets\HeavyShot.ico'))) { & (Join-Path $root 'make-icon.ps1') }
$sources=@(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName)
$compileArgs=@('/nologo','/target:winexe','/platform:anycpu','/optimize+','/utf8output','/warn:4',('/out:'+(Join-Path $dist 'HeavyShot.exe')),('/win32manifest:'+(Join-Path $root 'src\app.manifest')),('/win32icon:'+(Join-Path $root 'assets\HeavyShot.ico')),'/reference:System.dll','/reference:System.Core.dll','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll','/reference:System.Xml.dll')+$sources
& $compiler @compileArgs
if($LASTEXITCODE -ne 0) { throw 'Falha na compilação.' }
Copy-Item -LiteralPath (Join-Path $root 'src\HeavyShot.exe.config') -Destination $dist
if($Test) {
    $testOutput=Join-Path $root 'tests\results'
    $process=Start-Process -FilePath (Join-Path $dist 'HeavyShot.exe') -ArgumentList @('--self-test','--test-output',('"'+$testOutput+'"')) -PassThru -Wait -WindowStyle Hidden
    Get-Content -LiteralPath (Join-Path $testOutput 'test-results.txt')
    if($process.ExitCode -ne 0) { throw 'Testes falharam.' }
}
if($Package) {
    Copy-Item -LiteralPath (Join-Path $root 'PORTABLE.txt') -Destination $dist
    $archive=Join-Path $root 'HeavyShot-0.3.0-portable.zip'
    Compress-Archive -LiteralPath (Join-Path $dist 'HeavyShot.exe'),(Join-Path $dist 'HeavyShot.exe.config'),(Join-Path $dist 'PORTABLE.txt') -DestinationPath $archive -Force
    $hashes=@(Get-FileHash -LiteralPath (Join-Path $dist 'HeavyShot.exe'),$archive -Algorithm SHA256 | ForEach-Object { $_.Hash+'  '+[IO.Path]::GetFileName($_.Path) })
    $hashes | Set-Content -LiteralPath (Join-Path $root 'SHA256SUMS.txt') -Encoding ascii
}
Write-Output ('Build concluído: '+(Join-Path $dist 'HeavyShot.exe'))

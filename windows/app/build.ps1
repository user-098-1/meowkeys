param(
    [string]$Output = (Join-Path (Split-Path $PSScriptRoot -Parent) 'build\键盘喵.exe')
)
$ErrorActionPreference = 'Stop'
. (Join-Path (Split-Path $PSScriptRoot -Parent) 'toolchain.ps1')

$framework = Get-MeowkeysFrameworkDirectory
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($outputPath))
$references = foreach ($name in 'PresentationCore', 'PresentationFramework', 'WindowsBase') {
    '/reference:' + (Join-Path $framework "WPF\$name.dll")
}
$resources = foreach ($name in 'cursor-bird.png', 'cursor-teaser.png') {
    '/res:' + (Join-Path $PSScriptRoot $name) + ',' + $name
}
$sources = (Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' -File | Sort-Object Name).FullName
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /utf8output /reference:System.Xaml.dll `
    ('/win32manifest:' + (Join-Path $PSScriptRoot 'desktop.manifest')) `
    ('/win32icon:' + (Join-Path $PSScriptRoot 'cat.ico')) ('/out:' + $outputPath) @references @resources @sources
if ($LASTEXITCODE -ne 0) { throw "Application build failed ($LASTEXITCODE)." }
Get-Item -LiteralPath $outputPath

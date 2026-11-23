param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'build'),
    [string]$Compiler
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'toolchain.ps1')

if ($Compiler) {
    $nsis = (Get-Item -LiteralPath $Compiler).FullName
} else {
    $nsisCommand = Get-Command makensis.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($nsisCommand) {
        $nsis = $nsisCommand.Source
    } else {
        foreach ($programs in ${env:ProgramFiles(x86)}, $env:ProgramFiles) {
            if (!$programs) { continue }
            $candidate = Join-Path $programs 'NSIS\makensis.exe'
            if (Test-Path -LiteralPath $candidate -PathType Leaf) { $nsis = $candidate; break }
        }
    }
}
if (!$nsis) { throw 'Install NSIS 3 with Modern UI 2, or pass -Compiler with the path to makensis.exe.' }

$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$payload = Join-Path $outputPath '键盘喵.exe'
& (Join-Path $PSScriptRoot 'app\build.ps1') -Output $payload
$version = [Diagnostics.FileVersionInfo]::GetVersionInfo($payload)
$versionText = "$($version.FileMajorPart).$($version.FileMinorPart).$($version.FileBuildPart)"
$setup = Join-Path $outputPath "键盘喵-$versionText-Windows安装包.exe"
$installer = Join-Path $PSScriptRoot 'installer'
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('meowkeys-setup-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($temporary)
$helper = Join-Path $temporary 'SetupTasks.exe'
try {
    $framework = Get-MeowkeysFrameworkDirectory
    & (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /r:Microsoft.CSharp.dll `
        ('/out:' + $helper) (Join-Path $installer 'SetupTasks.cs')
    if ($LASTEXITCODE -ne 0) { throw "Installer helper build failed ($LASTEXITCODE)." }
    & $nsis /V2 ('/DOUTPUT=' + $setup) ('/DPAYLOAD=' + $payload) ('/DHELPER=' + $helper) `
        ('/DCAT_ICON=' + (Join-Path $PSScriptRoot 'app\cat.ico')) `
        ('/DWIZARD=' + (Join-Path $installer 'wizard.bmp')) (Join-Path $installer 'Setup.nsi')
    if ($LASTEXITCODE -ne 0) { throw "Installer build failed ($LASTEXITCODE)." }
} finally {
    [IO.File]::Delete($helper)
    [IO.Directory]::Delete($temporary)
}
Get-Item -LiteralPath $setup

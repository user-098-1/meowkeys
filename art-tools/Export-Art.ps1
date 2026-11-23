[CmdletBinding()]
param(
    [string] $OutputDirectory
)
$ErrorActionPreference = 'Stop'

# Windows PowerShell 5.1 leaves $PSScriptRoot empty inside param default expressions.
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $PSScriptRoot '../macos/resources/Art' }

$source = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../windows/app'))
$destination = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
if (-not (Test-Path -LiteralPath (Join-Path $framework 'csc.exe'))) {
    $framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319'
}
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The sprite exporter requires Windows with .NET Framework 4.x and WPF.'
}

$references = @('WindowsBase', 'PresentationCore', 'PresentationFramework') | ForEach-Object {
    $dll = Get-Item -LiteralPath (Join-Path $framework ('WPF/' + $_ + '.dll'))
    '/reference:' + $dll.FullName
}
$sources = @('DeskStyle', 'DeskArt', 'MouseArt', 'CatArt', 'TuxedoArt') | ForEach-Object {
    (Get-Item -LiteralPath (Join-Path $source ($_ + '.cs'))).FullName
}
$exportSource = (Get-Item -LiteralPath (Join-Path $PSScriptRoot 'ExportArt.cs')).FullName

$tempParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$work = Join-Path $tempParent ('meowkeys-art-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work -ErrorAction Stop | Out-Null
try {
    $compilerOutput = Join-Path $work 'ExportArt.exe'
    & $compiler /nologo /optimize+ /codepage:65001 /reference:System.Xaml.dll /reference:System.Web.Extensions.dll ('/out:' + $compilerOutput) @references @sources $exportSource
    if ($LASTEXITCODE -ne 0) { throw "Artwork exporter compilation failed ($LASTEXITCODE)." }

    $stage = Join-Path $work 'Art'
    & $compilerOutput $stage
    if ($LASTEXITCODE -ne 0) { throw "Artwork rendering failed ($LASTEXITCODE)." }

    [IO.Directory]::CreateDirectory($destination) | Out-Null
    foreach ($image in Get-ChildItem -LiteralPath $stage -Filter '*.png' -File) {
        [IO.File]::Copy($image.FullName, (Join-Path $destination $image.Name), $true)
    }
    [IO.File]::Copy((Join-Path $stage 'atlas.json'), (Join-Path $destination 'atlas.json'), $true)
    # Cursor overlays referenced by the atlas come straight from the Windows app's sprites.
    foreach ($pair in @(@('cursor-bird.png', 'cursor-0.png'), @('cursor-teaser.png', 'cursor-1.png'))) {
        [IO.File]::Copy((Join-Path $source $pair[0]), (Join-Path $destination $pair[1]), $true)
    }
    Write-Output "Assets generated in $destination"
} finally {
    $resolved = (Resolve-Path -LiteralPath $work).ProviderPath
    $parent = [IO.Path]::GetDirectoryName($resolved)
    $leaf = [IO.Path]::GetFileName($resolved)
    if ($parent.TrimEnd([IO.Path]::DirectorySeparatorChar) -eq $tempParent.TrimEnd([IO.Path]::DirectorySeparatorChar) -and
        $leaf -match '^meowkeys-art-[a-f0-9]{32}$') {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

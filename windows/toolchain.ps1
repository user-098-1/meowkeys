function Get-MeowkeysFrameworkDirectory {
    foreach ($edition in 'Framework64', 'Framework') {
        $directory = Join-Path $env:WINDIR "Microsoft.NET\$edition\v4.0.30319"
        $compiler = Join-Path $directory 'csc.exe'
        $wpf = Join-Path $directory 'WPF\PresentationFramework.dll'
        if ((Test-Path -LiteralPath $compiler -PathType Leaf) -and
            (Test-Path -LiteralPath $wpf -PathType Leaf)) {
            return $directory
        }
    }
    throw 'The Windows .NET Framework 4.8 compiler and WPF assemblies are required.'
}

function Build-Project
{
    param([string] $DirectoryName)

    Push-Location $DirectoryName
    & dotnet build -c Release
    if($LASTEXITCODE -ne 0) { exit 1 }    
    Pop-Location
}

function Pack-Project
{
    param([string] $DirectoryName)

    Push-Location $DirectoryName
    $revision = @{ $true = $env:APPVEYOR_BUILD_VERSION; $false = "0.0.1" }[$env:APPVEYOR_BUILD_VERSION -ne $NULL];
    & dotnet pack -c Release -o ..\..\.\artifacts
    if($LASTEXITCODE -ne 0) { exit 1 }    
    Pop-Location
}

function Update-BuildVersion
{
    param([string] $FileName)

    $revision = @{ $true = $env:APPVEYOR_BUILD_VERSION; $false = "0.0.1" }[$env:APPVEYOR_BUILD_VERSION -ne $NULL];	
    $csprojContent = Get-Content $FileName
    $csprojContent | % { $_.Replace("0.0.1", $revision) } | Set-Content $FileName
    Write-Host "Set version of $FileName to $revision"
}

function Test-Project
{
    param([string] $DirectoryName)

    Push-Location $DirectoryName
    & dotnet test -c Release
    if($LASTEXITCODE -ne 0) { exit 2 }
    Pop-Location
}

Push-Location $PSScriptRoot

# Clean
if(Test-Path .\artifacts) { Remove-Item .\artifacts -Force -Recurse }

# Modify project.json
Get-ChildItem -Path .\src -Filter *.csproj -Recurse | ForEach-Object { Update-BuildVersion $_.FullName }

# Package restore
& dotnet restore

# Build/package
Get-ChildItem -Path .\src -Filter *.csproj -Recurse | ForEach-Object { Pack-Project $_.DirectoryName }

# Build examples to ensure they at least build
Get-ChildItem -Path .\examples -Filter *.csproj -Recurse | ForEach-Object { Build-Project $_.DirectoryName }

# Test
Get-ChildItem -Path .\test -Filter *.csproj -Recurse | ForEach-Object { Test-Project $_.DirectoryName }

Pop-Location

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
    & dotnet pack -c Release -o ..\..\.\artifacts --version-suffix $revision
    if($LASTEXITCODE -ne 0) { exit 1 }    
    Pop-Location
}

function Update-ProjectJson
{
    param([string] $DirectoryName)

    $revision = @{ $true = $env:APPVEYOR_BUILD_VERSION; $false = "0.0.1" }[$env:APPVEYOR_BUILD_VERSION -ne $NULL];
	Set-BuildVersion $DirectoryName $revision
	Set-CachingAbstractionVersion $DirectoryName $revision
    
    Push-Location $DirectoryName
    Pop-Location
}

function Set-BuildVersion
{
    param([string] $DirectoryName, [string]$revision)
	
    $projectJson = Join-Path $DirectoryName "project.json"
    $jsonData = Get-Content -Path $projectJson -Raw | ConvertFrom-JSON
	$jsonData.version = $revision
    $jsonData | ConvertTo-Json -Depth 999 | Out-File $projectJson
    Write-Host "Set version of $projectJson to $revision"
}

function Set-CachingAbstractionVersion
{
    param([string] $DirectoryName, [string]$revision)
	
    $projectJson = Join-Path $DirectoryName "project.json"
    $projectName = "Cimpress.Extensions.Http.Caching.Abstractions"
    $jsonData = Get-Content -Path $projectJson -Raw | ConvertFrom-JSON
    if (Get-Member -inputobject $jsonData.dependencies -name $projectName -Membertype Properties) {
	    $jsonData.dependencies.$($projectName) = $revision
        $jsonData | ConvertTo-Json -Depth 999 | Out-File $projectJson
        Write-Host "Set dependency of $projectName to $revision."
    } else {
        Write-Host "No dependency to $projectName."
    }
    
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
Get-ChildItem -Path .\src -Filter *.xproj -Recurse | ForEach-Object { Update-ProjectJson $_.DirectoryName }

# Package restore
& dotnet restore --configfile ./nuget.config

# Build/package
Get-ChildItem -Path .\src -Filter *.xproj -Recurse | ForEach-Object { Pack-Project $_.DirectoryName }

# Build examples to ensure they at least build
Get-ChildItem -Path .\examples -Filter *.xproj -Recurse | ForEach-Object { Build-Project $_.DirectoryName }

# Test
Get-ChildItem -Path .\test -Filter *.xproj -Recurse | ForEach-Object { Test-Project $_.DirectoryName }

Pop-Location

# Execute nuget list and capture the output
$nugetOutput = nuget list Microsoft.WindowsAppSDK `
    -Source "https://pkgs.dev.azure.com/microsoft/ProjectReunion/_packaging/WinAppSDK-Developer-Builds/nuget/v3/index.json" `
    -PreRelease `
    -AllVersions

# Extract the latest version number from the output
$latestVersion = $nugetOutput -split "`n" | `
    Select-String -Pattern 'Microsoft.WindowsAppSDK\s+([0-9]+\.[0-9]+\.[0-9]+-[a-zA-Z0-9]+)' | `
    ForEach-Object { $_.Matches[0].Groups[1].Value } | `
    Sort-Object -Descending | `
    Select-Object -First 1

if ($latestVersion) {
    $WinAppSDKVersion = $latestVersion
    Write-Host "Extracted version: $WinAppSDKVersion"
} else {
    Write-Host "Failed to extract version number from nuget list output"
    exit 1
}

Get-ChildItem -Recurse packages.config | foreach-object {
    $newVersionString = 'package id="Microsoft.WindowsAppSDK" version="' + $WinAppSDKVersion + '"'
    $oldVersionString = 'package id="Microsoft.WindowsAppSDK" version="[-.0-9a-zA-Z]*"'
    $content = Get-Content $_.FullName -Raw
    $content = $content -replace $oldVersionString, $newVersionString
    Set-Content -Path $_.FullName -Value $content
    Write-Host "Modified " $_.FullName 
}

Get-ChildItem -Recurse *.vcxproj | foreach-object {
    $newVersionString = '\Microsoft.WindowsAppSDK.' + $WinAppSDKVersion + '\'
    $oldVersionString = '\\Microsoft.WindowsAppSDK.[-.0-9a-zA-Z]*\\'
    $content = Get-Content $_.FullName -Raw
    $content = $content -replace $oldVersionString, $newVersionString
    Set-Content -Path $_.FullName -Value $content
    Write-Host "Modified " $_.FullName 
}

Get-ChildItem -Recurse *.csproj | foreach-object {
    $newVersionString = 'PackageReference Include="Microsoft.WindowsAppSDK" Version="'+ $WinAppSDKVersion + '"'
    $oldVersionString = 'PackageReference Include="Microsoft.WindowsAppSDK" Version="[-.0-9a-zA-Z]*"'
    $content = Get-Content $_.FullName -Raw
    $content = $content -replace $oldVersionString, $newVersionString
    Set-Content -Path $_.FullName -Value $content
    Write-Host "Modified " $_.FullName 
}
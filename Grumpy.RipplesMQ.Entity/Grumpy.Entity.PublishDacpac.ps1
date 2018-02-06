param (
    [string]$TargetPath,
    [string]$ProjectDir = $null,
    [string]$ProjectName = $null,
    [string]$PublishProfile = $null
)

if (-not($TargetPath))                          { throw "Missing argument -TargetPath" } 
if (-not(Test-Path $TargetPath -PathType Leaf)) { throw "$TargetPath is not a valid" } 

function Get-SqlpackageExe() {
    $Folders = @(
        ".\",
        "$PSScriptRoot",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\140\",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\140\DAC\bin\",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\130\",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\120\DAC\bin\"
    )

    $SqlPackageExe = $false

    foreach ($Folder in $Folders) {
        if (-not($SqlPackageExe) -and (Test-Path "$Folder\sqlpackage.exe"))  {
            $SqlPackageExe = Join-Path $Folder "sqlpackage.exe"
        }
    }

    if (-not($SqlPackageExe)) { 
        throw "Unable to find SqlPackage.exe" 
    }

    return $SqlPackageExe
}

function Extract-Dacpac([string]$DacpacFile, [string]$OutputFolder) {
    if (-not($DacpacFile))                          { throw "Missing argument -DacpacFile" } 
    if (-not($OutputFolder))                        { throw "Missing argument -OutputFolder" } 
    if (-not(Test-Path $DacpacFile -PathType Leaf)) { throw "Unable to find dacpac file $DacpacFile" } 
    if (Test-Path $OutputFolder)                    { throw "Output Folder already found $OutputFolder" } 

    Add-Type -Assembly System.IO.Compression.FileSystem

    $Files = @(
        "model.xml", 
        "origin.xml"
    )

    [System.IO.Compression.ZipFile]::ExtractToDirectory((Convert-Path $DacpacFile), $OutputFolder)

    foreach ($File in $Files) {
        if (!(Test-Path (Join-Path $OutputFolder $File)))  { throw "Invalid dacpac file ($DacpacFile) - $File not Found" }
    }
}

function Publish-Dacpac([string]$DacpacFile, [string]$ServerName, [string]$DatabaseName, [string]$PublishProfile = $null) {
    if (-not($DacpacFile))                                                   { throw "Missing argument -DacpacFile" } 
    if (-not($ServerName))                                                   { throw "Missing argument -ServerName" } 
    if (-not($DatabaseName))                                                 { throw "Missing argument -DatabaseName" } 
    if (-not(Test-Path $DacpacFile -PathType Leaf))                          { throw "Unable to find dacpac file $DacpacFile" } 
    if ($PublishProfile -and -not(Test-Path $PublishProfile -PathType Leaf)) { throw "Unable to find publish profile file $PublishProfile" } 

    $SqlPackageExe   = Get-SqlpackageExe
    
    $ProfileArgument = if ($PublishProfile) { "/Profile:$PublishProfile" } else { "" }

    & $SqlPackageExe /Action:Publish /SourceFile:$DacPacFile /TargetServerName:$ServerName /TargetDatabaseName:$DatabaseName $ProfileArgument

    if ($LASTEXITCODE) { 
        throw "Error publishing dacpac ($DacPacFile) to LocalDB" 
    }
}

function Calc-Hash([string]$DacpacFile) {
    if (-not($DacpacFile))                          { throw "Missing argument -DacpacFile" } 
    if (-not(Test-Path $DacpacFile -PathType Leaf)) { throw "Unable to find dacpac file $DacpacFile" } 

    $DacpacFolder = Join-Path $env:TEMP ((Split-Path -Path $DacpacFile -Leaf) + "_" + [guid]::NewGuid())

    Extract-Dacpac -DacpacFile $DacpacFile -OutputFolder $DacpacFolder

    $Hash = (Get-FileHash (Join-Path $DacpacFolder "model.xml") -Algorithm 'SHA256').Hash

    Remove-Item $DacpacFolder -Force -Recurse

    return $Hash
}

$TargetName   = (Get-ChildItem $TargetPath).BaseName
$TargetDir    = (Get-ChildItem $TargetPath).Directory
$DatabaseName = $TargetName + "_Model"
$ServerName   = "(localdb)\MSSQLLocalDB"
$DacpacFile   = "$TargetDir\$TargetName.dacpac"
$HashFile     = $DacpacFile + ".hash"

$NewHash = Calc-Hash -DacpacFile $DacpacFile

if (Test-Path $HashFile -PathType Leaf) {
    $OldHash = Get-Content $HashFile

    if ($NewHash -ne $OldHash) {
        Remove-Item $HashFile -Force
    }
}

if (-not(Test-Path $HashFile -PathType Leaf)) {
    if (-not($PublishProfile) -and $ProjectDir -and $ProjectName) { 
        $PublishProfile = "$ProjectDir\$ProjectName.publish.xml" 

        if (-not(Test-Path $PublishProfile -PathType Leaf)) {
            $PublishProfile = $null
        }
    }

    Publish-Dacpac -DacpacFile $DacpacFile -ServerName $ServerName -DatabaseName $DatabaseName -PublishProfile $PublishProfile

    Set-Content -Path $HashFile -Value "$NewHash"

    "$DatabaseName on $ServerName updated - To use the new version in Entity Framework Model, use the 'Update Model from database...' feature on the Entity Framework Model"
} else {
    "$DatabaseName on $ServerName already up-to-date"
}

$DatabaseNameFile = "$ProjectDir\" + $DatabaseName + ".entity_database"

if (Test-Path -Path $DatabaseNameFile) {
    Remove-Item $DatabaseNameFile -Force
}

Set-Content -Path $DatabaseNameFile -Value "$ServerName"
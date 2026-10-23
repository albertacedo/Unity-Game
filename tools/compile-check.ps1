# Compile check without opening Unity.
#
# Rebuilds Assets/Scripts against the Unity assemblies already imported into
# Library/, using the .NET SDK. Use it as a fast "does it compile?" gate before
# alt-tabbing into the Editor; Unity still needs to be opened afterwards to
# generate .meta files and to run the game.
#
# Usage (from the repo root):
#   powershell -File tools/compile-check.ps1

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path $PSScriptRoot -Parent
$versionFile = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'
if (-not (Test-Path $versionFile)) {
    throw "ProjectVersion.txt not found. Run this from inside the Unity project."
}

$editorVersion = (Get-Content $versionFile | Where-Object { $_ -match '^m_EditorVersion:' }) -replace '^m_EditorVersion:\s*', ''
$unityData = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion\Editor\Data"
if (-not (Test-Path $unityData)) {
    throw "Unity $editorVersion not found at $unityData. Adjust the path or open the project with Unity Hub."
}

$scriptAssemblies = Join-Path $projectRoot 'Library\ScriptAssemblies'
if (-not (Test-Path $scriptAssemblies)) {
    throw "Library\ScriptAssemblies not found. Open the project in Unity once so packages get compiled."
}

$references = @()
$references += (Get-ChildItem (Join-Path $unityData 'Managed\UnityEngine') -Filter *.dll | Select-Object -ExpandProperty FullName)

foreach ($name in @('UnityEngine.UI.dll', 'Unity.TextMeshPro.dll', 'Unity.InputSystem.dll')) {
    $path = Join-Path $scriptAssemblies $name
    if (Test-Path $path) { $references += $path }
}

$references = $references | Sort-Object -Unique

$buildDir = Join-Path $env:TEMP 'opencode\unity-compile-check'
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null

$referenceXml = ($references | ForEach-Object {
    "    <Reference Include=`"$([System.IO.Path]::GetFileNameWithoutExtension($_))`"><HintPath>$_</HintPath><Private>false</Private></Reference>"
}) -join "`n"

$csproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <Nullable>disable</Nullable>
    <NoWarn>CS0649;CS0169;CS0414</NoWarn>
    <DefineConstants>UNITY_EDITOR;UNITY_6000_3_OR_NEWER;ENABLE_INPUT_SYSTEM;UNITY_STANDALONE_WIN</DefineConstants>
    <AssemblyName>CompileCheck</AssemblyName>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$projectRoot\Assets\Scripts\**\*.cs" />
  </ItemGroup>
  <ItemGroup>
$referenceXml
  </ItemGroup>
</Project>
"@

$csprojPath = Join-Path $buildDir 'CompileCheck.csproj'
Set-Content -Path $csprojPath -Value $csproj -Encoding UTF8

dotnet build $csprojPath -v q --nologo
exit $LASTEXITCODE

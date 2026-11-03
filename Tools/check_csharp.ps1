param([string]$EditorRoot = 'C:/Program Files/Unity/Hub/Editor/2022.3.45f1c1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$checkDir = Join-Path $projectRoot 'Temp/PrototypeChecks'
New-Item -ItemType Directory -Force -Path $checkDir | Out-Null
$compiler = Join-Path $EditorRoot 'Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$mono = Join-Path $EditorRoot 'Data/MonoBleedingEdge/bin/mono.exe'
$coreFiles = @(Get-ChildItem (Join-Path $projectRoot 'Assets/_Game/Scripts/Core') -Filter '*.cs' | ForEach-Object FullName)
$gameplayFiles = @(Get-ChildItem (Join-Path $projectRoot 'Assets/_Game/Scripts/Gameplay') -Filter '*.cs' | ForEach-Object FullName)
$testExe = Join-Path $checkDir 'CoreSmokeTests.exe'
& $mono $compiler /nologo /langversion:9 "/out:$testExe" $coreFiles $gameplayFiles (Join-Path $PSScriptRoot 'CoreSmokeTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Core test compilation failed' }
& $mono $testExe
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
$unityReferences = @(Get-ChildItem (Join-Path $EditorRoot 'Data/Managed/UnityEngine') -Filter '*.dll' | ForEach-Object { '/reference:"' + $_.FullName + '"' })
$standardReferences = @(Get-ChildItem (Join-Path $EditorRoot 'Data/NetStandard/ref/2.1.0') -Filter '*.dll' | ForEach-Object { '/reference:"' + $_.FullName + '"' })
$uiAssembly = Join-Path $projectRoot 'Library/ScriptAssemblies/UnityEngine.UI.dll'
if (!(Test-Path $uiAssembly)) { throw 'Open project in Unity once to import UGUI before runtime compilation check' }
$sources = @(Get-ChildItem (Join-Path $projectRoot 'Assets/_Game/Scripts') -Recurse -Filter '*.cs' | Where-Object { $_.FullName -notmatch '[\\/]Editor[\\/]' } | ForEach-Object { '"' + $_.FullName + '"' })
$response = @('/nologo','/nostdlib+','/target:library','/langversion:9',('/out:"' + (Join-Path $checkDir 'Prototype.dll') + '"'),('/reference:"' + $uiAssembly + '"')) + $standardReferences + $unityReferences + $sources
$responsePath = Join-Path $checkDir 'runtime.rsp'
[System.IO.File]::WriteAllLines($responsePath, $response)
& $mono $compiler /noconfig "@$responsePath"
if ($LASTEXITCODE -ne 0) { throw 'Runtime compilation failed' }
Write-Output 'PASS: runtime C# compilation against installed Unity assemblies'
$editorSources = @(Get-ChildItem (Join-Path $projectRoot 'Assets/_Game/Scripts/Editor') -Filter '*.cs' | ForEach-Object { '"' + $_.FullName + '"' })
$editorResponse = @('/nologo','/nostdlib+','/target:library','/langversion:9',('/out:"' + (Join-Path $checkDir 'Prototype.Editor.dll') + '"'),('/reference:"' + (Join-Path $EditorRoot 'Data/Managed/UnityEditor.dll') + '"'),('/reference:"' + (Join-Path $checkDir 'Prototype.dll') + '"'),('/reference:"' + $uiAssembly + '"')) + $standardReferences + $unityReferences + $editorSources
$editorResponsePath = Join-Path $checkDir 'editor.rsp'
[System.IO.File]::WriteAllLines($editorResponsePath, $editorResponse)
& $mono $compiler /noconfig "@$editorResponsePath"
if ($LASTEXITCODE -ne 0) { throw 'Editor compilation failed' }
Write-Output 'PASS: editor C# compilation'

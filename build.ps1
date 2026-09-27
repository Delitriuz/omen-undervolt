$ErrorActionPreference = 'Stop'
$toolRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source = Join-Path $toolRoot 'src'
$assets = Join-Path $toolRoot 'assets'
$dist = Join-Path $toolRoot 'dist'
$testArtifacts = Join-Path $toolRoot 'tests\artifacts'
$activeApp = Get-Process -Name 'OmenUndervolt' -ErrorAction SilentlyContinue
$appName = if ($activeApp) { 'OmenUndervolt.next.exe' } else { 'OmenUndervolt.exe' }

if (-not (Test-Path -LiteralPath $compiler)) {
    throw '未找到 .NET Framework 4.x C# 编译器。'
}

New-Item -ItemType Directory -Path $dist -Force | Out-Null
New-Item -ItemType Directory -Path $testArtifacts -Force | Out-Null
$references = @(
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Management.dll',
    '/reference:System.Windows.Forms.dll'
)
$appSources = @(
    (Join-Path $source 'AssemblyInfo.cs'),
    (Join-Path $source 'Program.cs'),
    (Join-Path $source 'HpBiosWmi.cs'),
    (Join-Path $source 'SafetyProtocol.cs')
)
& $compiler /nologo /noconfig /optimize+ /platform:x64 /target:winexe `
    "/out:$dist\$appName" "/win32manifest:$toolRoot\app.manifest" "/win32icon:$assets\omen-undervolt.ico" `
    "/resource:$assets\omen-undervolt.png,OmenUndervolt.Assets.OmenLogo.png" `
    "/resource:$assets\omen-undervolt.ico,OmenUndervolt.Assets.OmenIcon.ico" `
    $references $appSources
if ($LASTEXITCODE -ne 0) { throw "GUI 编译失败，退出码 $LASTEXITCODE。" }

$testSources = @(
    (Join-Path $toolRoot 'tests\ProtocolTests.cs'),
    (Join-Path $source 'Program.cs'),
    (Join-Path $source 'HpBiosWmi.cs'),
    (Join-Path $source 'SafetyProtocol.cs')
)
& $compiler /nologo /noconfig /optimize+ /platform:x64 /target:exe '/main:ProtocolTests' `
    "/out:$testArtifacts\ProtocolTests.exe" '/reference:System.dll' '/reference:System.Core.dll' `
    '/reference:System.Drawing.dll' '/reference:System.Management.dll' '/reference:System.Windows.Forms.dll' `
    "/resource:$assets\omen-undervolt.png,OmenUndervolt.Assets.OmenLogo.png" `
    "/resource:$assets\omen-undervolt.ico,OmenUndervolt.Assets.OmenIcon.ico" $testSources
if ($LASTEXITCODE -ne 0) { throw "协议测试编译失败，退出码 $LASTEXITCODE。" }

$readOnlySources = @(
    (Join-Path $toolRoot 'tests\WmiReadOnlyCheck.cs'),
    (Join-Path $source 'HpBiosWmi.cs'),
    (Join-Path $source 'SafetyProtocol.cs')
)
& $compiler /nologo /noconfig /optimize+ /platform:x64 /target:exe `
    "/out:$testArtifacts\WmiReadOnlyCheck.exe" '/reference:System.dll' '/reference:System.Management.dll' $readOnlySources
if ($LASTEXITCODE -ne 0) { throw "只读 WMI 检查器编译失败，退出码 $LASTEXITCODE。" }

Write-Output "已生成 $dist\$appName"

Write-Host "Updating Git submodule."
git submodule update --init --recursive --quiet

Write-Host "Apply patch to SOEM."
$path = "$($PSScriptRoot)/native/SOEM"
Set-Location -Path $path
git apply --stat ./../0001-Tune-SOEM.patch
git apply --check ./../0001-Tune-SOEM.patch
git apply ./../0001-Tune-SOEM.patch

# x86
Write-Host "Creating native x86 project."
$path = "$($PSScriptRoot)/artifacts/bin32"
New-Item -Force -ItemType directory -Path $path
Set-Location -Path $path

if ($IsWindows)
{
	cmake ./../../native -DCMAKE_CONFIGURATION_TYPES:STRING="Debug;Release" -G "Visual Studio 15 2017" 
}
elseif ($IsLinux)
{
    cmake ./../../native -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_FLAGS=-m32 -DCMAKE_CXX_FLAGS=-m32 -DCMAKE_TRY_COMPILE_TARGET_TYPE=STATIC_LIBRARY
}
else 
{
    throw [System.PlatformNotSupportedException]
}

# x64
Write-Host "Creating native x64 project."
$path = "$($PSScriptRoot)/artifacts/bin64"
New-Item -Force -ItemType directory -Path $path
Set-Location -Path $path

if ($IsWindows)
{
	cmake ./../../native -DCMAKE_CONFIGURATION_TYPES:STRING="Debug;Release" -G "Visual Studio 15 2017 Win64"
}
elseif ($IsLinux)
{
    cmake ./../../native -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_FLAGS=-m64 -DCMAKE_CXX_FLAGS=-m64
}
else 
{
    throw [System.PlatformNotSupportedException]
}

# return
Set-Location -Path $PSScriptRoot
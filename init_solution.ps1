Write-Host "Updating Git submodule."
git submodule update --init --recursive --quiet

# load .NET types
$RType = [type]::GetType('System.Runtime.InteropServices.RuntimeInformation, System.Runtime.InteropServices.RuntimeInformation')
$PType = [type]::GetType('System.Runtime.InteropServices.OSPlatform, System.Runtime.InteropServices.RuntimeInformation')

try
{
    if (-not $RType -or -not $PType) { throw }

    $R = $RType
    $P = $PType

    $mWindows = $R::IsOSPlatform($P::Windows)
    $mLinux   = $R::IsOSPlatform($P::Linux)
    $mMacOS   = $R::IsOSPlatform($P::OSX)
}
catch
{
    # fallback for Windows PowerShell 5.1 oder missing types
    $mWindows = $env:OS -eq 'Windows_NT'
    $mLinux   = $false
    $mMacOS   = $false
}

Write-Host "Windows: $mWindows, Linux: $mLinux, macOS: $mMacOS"

# detect architecture once
# try .NET Core API first (PowerShell Core on Linux/macOS)
$type = [type]::GetType('System.Runtime.InteropServices.RuntimeInformation, System.Runtime.InteropServices.RuntimeInformation')

if ($type)
{
    # RuntimeInformation exists
    $arch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
}
else
{
    # Fallback for Windows PowerShell / older .NET
    switch ($env:PROCESSOR_ARCHITECTURE) {
        'AMD64' { $arch = 'X64'    }
        'x86'   { $arch = 'X86'    }
        'ARM64' { $arch = 'Arm64'  }
        'ARM'   { $arch = 'Arm'    }
        default { $arch = $env:PROCESSOR_ARCHITECTURE }
    }
}

Write-Host "Detected architecture: $arch"

# x86 / arm
if ($arch -like 'Arm*')
{
    Write-Host "Creating native arm project."
    $path = "$($PSScriptRoot)/artifacts/arm32"
} else
{
    Write-Host "Creating native x86 project."
    $path = "$($PSScriptRoot)/artifacts/bin32"
}

New-Item -Force -ItemType Directory -Path $path | Out-Null
Set-Location -Path $path

if ($mWindows)
{
	cmake ./../../native `
        -DCMAKE_CONFIGURATION_TYPES:STRING="Debug;Release" `
        -DCMAKE_POLICY_VERSION_MINIMUM='3.5' `
        -G "Visual Studio 16 2019" `
        -A "Win32"
}
elseif ($mLinux)
{
    if ($arch -like 'Arm*')
    {
        # For 32-bit ARM, install the following compilers:
        # sudo apt-get install gcc-arm-linux-gnueabihf g++-arm-linux-gnueabihf

        cmake ./../../native `
            -DCMAKE_C_COMPILER=arm-linux-gnueabihf-gcc `
            -DCMAKE_CXX_COMPILER=arm-linux-gnueabihf-g++ `
            -DCMAKE_BUILD_TYPE=Release `
            -DCMAKE_POLICY_VERSION_MINIMUM='3.5' `
            -DCMAKE_C_FLAGS='-Wno-error=stringop-overflow -Wno-stringop-overflow' `
            -DCMAKE_CXX_FLAGS='-Wno-error=stringop-overflow -Wno-stringop-overflow'
    }
    else
    {
        cmake ./../../native `
            -DCMAKE_BUILD_TYPE=Release `
            -DCMAKE_POLICY_VERSION_MINIMUM='3.5' `
            -DCMAKE_C_FLAGS=-m32 `
            -DCMAKE_CXX_FLAGS=-m32 `
            -DCMAKE_TRY_COMPILE_TARGET_TYPE=STATIC_LIBRARY
    }
}
elseif ($mMacOS)
{
    # The i386 architecture is deprecated for macOS
    throw [System.PlatformNotSupportedException]
}
else
{
    throw [System.PlatformNotSupportedException]
}

# x64 / arm4
if ($arch -like 'Arm*')
{
    Write-Host "Creating native arm64 project."
    $path = "$($PSScriptRoot)/artifacts/arm64"
} else
{
    Write-Host "Creating native x64 project."
    $path = "$($PSScriptRoot)/artifacts/bin64"
}

New-Item -Force -ItemType directory -Path $path
Set-Location -Path $path

if ($mWindows)
{
	cmake ./../../native `
        -DCMAKE_CONFIGURATION_TYPES:STRING="Debug;Release" `
        -DCMAKE_POLICY_VERSION_MINIMUM='3.5' `
        -G "Visual Studio 16 2019" `
        -A "x64"
}
elseif ($mLinux)
{
    if ($arch -like 'Arm*')
    {
        cmake ./../../native `
            -DCMAKE_BUILD_TYPE=Release `
            -DCMAKE_POLICY_VERSION_MINIMUM='3.5' `
            -DCMAKE_C_FLAGS='-Wno-error=stringop-overflow -Wno-stringop-overflow' `
            -DCMAKE_CXX_FLAGS='-Wno-error=stringop-overflow -Wno-stringop-overflow'
    }
    else
    {
        cmake ./../../native `
            -DCMAKE_BUILD_TYPE=Release `
            -DCMAKE_POLICY_VERSION_MINIMUM='3.5' `
            -DCMAKE_C_FLAGS=-m64 `
            -DCMAKE_CXX_FLAGS=-m64
    }
}
elseif ($mMacOS)
{
    cmake ./../../native `
        -DCMAKE_BUILD_TYPE=Release `
        -DCMAKE_POLICY_VERSION_MINIMUM='3.5' `
        -DCMAKE_C_FLAGS=-m64 `
        -DCMAKE_CXX_FLAGS=-m64
}
else
{
    throw [System.PlatformNotSupportedException]
}

# return
Set-Location -Path $PSScriptRoot

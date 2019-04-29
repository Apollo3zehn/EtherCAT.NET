# EtherCAT.NET

[![AppVeyor](https://ci.appveyor.com/api/projects/status/github/apollo3zehn/ethercat.net?svg=true)](https://ci.appveyor.com/project/Apollo3zehn/ethercat-net)

A large amount of the logic of EtherCAT.NET comes from the data acquisition software [OneDAS](https://github.com/OneDAS-Group/OneDAS-Core), where the master has been extensively tested on many slaves from Beckhoff and Gantner. Due to the effort to reduce protocol specific logic within OneDAS and to allow standalone use of the EtherCAT master, EtherCAT.NET has been created. 

EtherCAT.NET itself provides high-level abstraction of the underlying native *Simple Open Source EtherCAT Master* ([SOEM](https://github.com/OpenEtherCATsociety/soem)). To accomplish this, the solution contains another project: SOEM.PInvoke. It comprises the actual native libraries for Windows and Linux and allows to simply P/Invoke into the native SOEM methods. The intention is to provide a managed way to access the native SOEM master. EtherCAT.NET depends on SOEM.PInvoke and adds classes for high-level abstraction.

In its current state, many, but not all planned features are already implemented. Thus only an alpha version is available ([NuGet](https://www.nuget.org/packages/EtherCAT.NET)) up to now. This mainly means that any EtherCAT network can be configured and started, but high-level features like simple configuration of the SDOs are not yet implemented. 

As written in the repository description, this master already supports slave configuration via ESI files. In fact, these files are *required* to allow the master to work. As shown in the sample [sample](https://github.com/Apollo3zehn/EtherCAT.NET/tree/master/sample/SampleMaster), you need to point the configuration to the folder, where the ESI files live. 

A future improvement will be the caching of ESI files to speed-up startup time. As a workaround, put only those ESI files into the folder, which are really needed. 

Another (half-working) feature is to allow the configuration of complex slaves. For example, this master has been sucessfully tested with the Profibus terminal (```EL6731-0010```). TwinCAT allow confguration of this terminal through a special configuration page. Since creating high-level configuration interface for each complex slave is much work, the priority for EtherCAT.NET lies on providing a simple interface to customize SDOs (like in TwinCAT), so that the end user can tune the required settings for any slave in a generic way. 

### Prerequisites (at runtime)

* WinPcap (on Windows)

### Building the solution

#### Prerequisites

* Visual Studio 2019 (on Windows)
* PowerShell Core
* CMake

#### Do the following to build the solution:

1. Execute the `init_solution.ps1` within the root folder (once).
2. On Windows, run (use Visual Studio command prompt if ```msbuild``` is not available in the ```PATH``` variable):
    ```
    msbuild ./artifacts/bin32/SOEM_wrapper/soem_wrapper.vcxproj /p:Configuraton=Release
    msbuild ./artifacts/bin64/SOEM_wrapper/soem_wrapper.vcxproj /p:Configuraton=Release
    ```
3. On Linux, run:
    ```
    make --directory ./artifacts/bin32
    make --directory ./artifacts/bin64
    ```
4. Run ```dotnet build ./src/EtherCAT.NET/EtherCAT.NET.csproj```.
5. Find the resulting packages in ```./artifacts/packages/*```.
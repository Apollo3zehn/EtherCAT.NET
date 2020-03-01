# EtherCAT.NET

[![AppVeyor](https://ci.appveyor.com/api/projects/status/github/apollo3zehn/ethercat.net?svg=true)](https://ci.appveyor.com/project/Apollo3zehn/ethercat-net)

A large amount of the logic of EtherCAT.NET comes from the data acquisition software [OneDAS](https://github.com/OneDAS-Group/OneDAS-Core), where the master has been extensively tested on many slaves from Beckhoff, Gantner and Anybus. Due to the effort to reduce protocol specific logic within OneDAS and to allow standalone use of the EtherCAT master, EtherCAT.NET was born. 

EtherCAT.NET itself provides high-level abstraction of the underlying native *Simple Open Source EtherCAT Master* ([SOEM](https://github.com/OpenEtherCATsociety/soem)). To accomplish this, the solution contains another project: SOEM.PInvoke. It comprises the actual native libraries for Windows and Linux and allows to simply P/Invoke into the native SOEM methods. The intention is to provide a managed way to access the native SOEM master. EtherCAT.NET depends on SOEM.PInvoke and adds classes for high-level abstraction.

In its current state, many, but not all planned features are implemented. Thus, only an alpha version is available ([NuGet](https://www.nuget.org/packages/EtherCAT.NET)) up to now. This mainly means that any EtherCAT network can be configured and started, but high-level features like simple configuration of the SDOs are not yet implemented. 

This master already supports slave configuration via ESI files. In fact, these files are *required* for the master to work. As shown in the [sample](sample/SampleMaster), you need to point the configuration to the folder, where the ESI files live.

Another feature is the configuration of complex slaves. For example, this master has been sucessfully tested with the Profibus terminal (```EL6731-0010```). TwinCAT allows configuration of this terminal through a special configuration page. Since creating high-level configuration interfaces for each complex slave is much work, the priority for EtherCAT.NET lies on providing a simple interface to customize SDOs (like in TwinCAT), so that the end user can tune the required settings for any slave in a generic way. 

## Running the application

> **Linux**: Run the application with root privileges as pointed out [here](https://github.com/OpenEtherCATsociety/SOEM/blob/8832ef0f2fa09fb0b06acaca239b51263ca1878c/doc/soem.dox#L20).

> **Windows**: Install [WinPcap](https://www.winpcap.org/).

## How to use EtherCAT.NET

If you start with the [sample](https://github.com/Apollo3zehn/EtherCAT.NET/tree/master/sample/SampleMaster), make sure to adapt the interface name in ```Program.cs``` to that of your network adapter and to populate the ESI directory with the required ESI files. When you run the sample application, the output will be similar to the following:

![sample-master](https://user-images.githubusercontent.com/20972129/57144734-01a22f80-6dc2-11e9-9b8a-f32d5a8d7b2f.png)

### Generate the list of slaves

The master can be operated without having a list of slaves. In that case, it scans available slaves during startup. But the disadvantage is that the slaves cannot be configured in advance and that no variable references are available. Therefore, there are two ways to generate the slave list as shown here:

1. Create the list manually (it must match with the actually connected slaves)
    ```
    --> not yet possible
    ```
2. Scan the list of connected slaves
    ```cs
    var rootSlaveInfo = EcUtilities.ScanDevices(<network interface name>);
    ```

The returned ```rootSlaveInfo``` is the master itself, which holds child slaves in its ```SlaveInfoSet``` property)
After that, the found slaves should be populated with ESI information:

```cs
rootSlaveInfo.Descendants().ToList().ForEach(current =>
{
    ExtensibilityHelper.CreateDynamicData(settings.EsiDirectoryPath, extensionFactory, current);
});
    
```

### Accessing the slaves

This master works differently to TwinCAT in that the slaves are identified using the *configure slave alias* (CSA) field in the EEPROM (see section 2.3.1 of the [Hardware Data Sheet Section II](https://download.beckhoff.com/download/Document/io/ethercat-development-products/ethercat_esc_datasheet_sec2_registers_2i7.pdf)). Whenever the master finds a slave with ```CSA = 0``` it assigns a new random number. This number can be acquired after the first run by printing the CSA of each slave:

```cs
var message = new StringBuilder();
var slaves = rootSlaveInfo.Descendants().ToList();

message.AppendLine($"Found {slaves.Count()} slaves:");

slaves.ForEach(current =>
{
    message.AppendLine($"{current.DynamicData.Name} (PDOs: {current.DynamicData.PdoSet.Count} - CSA: { current.Csa })");
});

logger.LogInformation(message.ToString().TrimEnd());
```

Now, if the hardware slave order is changed, the individual slaves can be identified by:

```cs
var slaves = rootSlaveInfo.Descendants().ToList();
var EL1008 = slaves.FirstOrDefault(current => current.Csa == 3);
```

Of course, as long as the hardware setup does not change, you can always get a reference to a slave by simple indexing:

```cs
var EL1008 = slaves[1];
```

### Accessing process data objects (PDOs) and variables

When you have a reference to a slave, the PDOs can be accessed via the ```DynamicData``` property:

```cs
var pdos = slaves[0].DynamicData.PdoSet;
var channel0 = pdo[0];
```

Since a PDO is a group of variables, these can be found below the PDO:

```cs
var variables = pdo.VariableSet;
var variable0 = variables[0];
```

A variable holds a reference to a certain address in RAM. This address is found in the property ```variable0.DataPtr```. During runtime (after configuration of the master), this address is set to a real RAM address. So the data can be manipulated using the ```unsafe``` keyword. Here we have a boolean variable, which is a single bit in EtherCAT, and it can be [toggled](https://stackoverflow.com/questions/47981/how-do-you-set-clear-and-toggle-a-single-bit) with the following code:

```cs
unsafe
{
    var myVariableSpan = new Span<int>(variable0.DataPtr.ToPointer(), 1);
    myVariableSpan[0] ^= 1UL << variable0.DataPtr.BitOffset;
}
```

Be careful when using raw pointers, so that you do not modify data outside the array boundaries.

### Running the master

First, an ```EcSettings``` object must be created. The constructor takes the parameters ```cycleFrequency```, ```esiDirectoryPath``` and ```interfaceName```. The first one specifies the cycle time of the master and is important for distributed clock configuration. The last one, ```interfaceName```, is the name of your network adapter.

The ```esiDirectoryPath``` parameter contains a path to a folder containing the ESI files. For Beckhoff slaves, these can be downloaded [here](https://www.beckhoff.de/default.asp?download/elconfg.htm). 
The first startup may take a while since an ESI cache is built to speed-up subsequent starts. Whenever a new and unknown slave is added, this cache is rebuilt.

With the ```EcSettings``` object and a few more types (like ```ILogger```, see the sample), the master can be put in operation using:

```cs
using (var master = new EcMaster(rootSlaveInfo, settings, extensionFactory, logger))
{
    master.Configure();

    while (true)
    {
        /* Here you can update your inputs and outputs. */
        master.UpdateIO(DateTime.UtcNow);
        /* Here you should let your master pause for a while, e.g. using Thread.Sleep or by simple spinning. */
    }
}

```

If you need a more sophisticated timer implementation, take a look to [this one](https://github.com/OneDAS-Group/OneDAS-Core/blob/master/src/OneDas.Core/Engine/RtTimer.cs). It can be used as follows:

```cs
var interval = TimeSpan.FromMilliseconds(100);
var timeShift = TimeSpan.Zero;
var timer = new RtTimer();

using (var master = new EcMaster(settings, extensionFactory, logger))
{
    master.Configure(rootSlaveInfo);
    timer.Start(interval, timeShift, UpdateIO);

    void UpdateIO()
    {
        /* Here you can update your inputs and outputs. */
        master.UpdateIO(DateTime.UtcNow);
    }

    Console.ReadKey();
    timer.Stop();
}

```

## Compiling the application

A single Powershell *Core* script is used for all platforms to initialize the solution. This simplifies CI builds - but requires Powershell Core to be available on the target system. If you don't want to install it, you can extract the information in the script and perform the steps manually.   

### Linux (tested on Ubuntu 18.04 x64)

You need the following tools:

* [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
* [PowerShell Core](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-linux?view=powershell-6)
* [build-essential](https://packages.debian.org/de/sid/build-essential)
* [g++-multilib](https://packages.debian.org/de/sid/g++-multilib)
* [CMake](https://cmake.org)

The solution can then be built as follows:

1. Execute the powershell script once within the root folder:
    ```
    pwsh ./init_solution.ps1
    ```
2. Run ```make``` everytime you make changes to the native code:
    ```
    make --directory ./artifacts/bin32
    make --directory ./artifacts/bin64
    ```
3. Run ```dotnet build``` everytime make changes to the managed code: 
    ```
    dotnet build ./src/EtherCAT.NET/EtherCAT.NET.csproj
    ```
4. Find the resulting packages in ```./artifacts/packages/*```.

### Windows

You need the following tools:

* [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
* [PowerShell Core](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-windows?view=powershell-6)
* [Visual Studio 2019](https://visualstudio.microsoft.com)
* [CMake](https://cmake.org)

The solution can then be built as follows:

1. Execute the powershell script once within the root folder. If you don't want to install Powershell Core, you can adapt the script and replace ```$IsWindows``` with ```$true```.
    ```
    ./init_solution.ps1
    ```
2. Run ```msbuild```* everytime you make changes to the native code:
    ```
    msbuild ./artifacts/bin32/SOEM_wrapper/soem_wrapper.vcxproj /p:Configuration=Release
    msbuild ./artifacts/bin64/SOEM_wrapper/soem_wrapper.vcxproj /p:Configuration=Release
    ```
    (* use Visual Studio Developer Command Prompt if ```msbuild``` is not available in the ```PATH``` variable or compile using Visual Studio directly).
3. Run ```dotnet build``` everytime you make changes to the managed code: 
    ```
    dotnet build ./src/EtherCAT.NET/EtherCAT.NET.csproj
    ```
4. Find the resulting packages in ```./artifacts/packages/*```.

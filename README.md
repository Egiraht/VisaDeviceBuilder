# VisaDeviceBuilder

**VisaDeviceBuilder** is a library for creation of controlling software for laboratory equipment that supports *VISA*
communication standard. The library relies on *IVI Foundation VISA.NET* standard and targets the *.NET 5* framework.

## Summary

The main logical unit of the library is the **VisaDevice** class that represents a software representation of a
particular hardware device. It provides access to the underlying low-level native *VISA.NET* session object through
the **Session** property and uses concepts of asynchronous properties and device actions to control the device.

An asynchronous property (**AsyncProperty**) is a logical representation of a hardware device's parameter of a
particular value type, such as a voltage level expressed in volts as a floating point number, or a text string displayed
on a device's display. Asynchronous properties can be read-only, write-only, or read-write to respect the nature of
reflected parameters.

A device action (**DeviceAction**) is an action that can be performed by a device without returning of any values, such
as a device reset procedure.

Any communication between the **VisaDevice** instance and the corresponding hardware device is performed asynchronously,
so that communication delays of one device likely will not defer access to another one.

**VisaDevice** is the most common object to implement devices supporting various hardware interfaces and control
patterns. Often laboratory equipment supports controlling through text commands (like *SCPI* command language). For such
devices there is a specialized variant of the **VisaDevice** class called **MessageDevice** that allows to send and
receive text messages in an easier way.

Though it is possible to fully manipulate a device using only the concepts described above, the library also contains
additional classes like **VisaDeviceController** that provide additional features for controlling devices, e.g.
performing of full device initialization and de-initialization processes, auto-updating of asynchronous properties,
access to all device communication errors through a single event, etc. The special device builder classes
(**VisaDeviceBuilder** and **MessageDeviceBuilder**) allow to construct customized device representations by just
calling a chain of methods.

## Usage

There are two main ways of creating a new VISA device representation:

* Create a new class that derives from the **VisaDevice** (or **MessageDevice**) base class, declare and initialize new
  asynchronous properties and device actions as readable properties in it, and override necessary methods to add custom
  behaviour if needed,
* Use **VisaDeviceBuilder** (or **MessageDeviceBuilder**) class to construct a new device instance in a chain of class
  methods.

## Requirements

This library requires the *.NET 5* runtime for functioning. The optional **VisaDeviceBuilder.WPF** library is based on
Windows-specific technologies and requires the corresponding OS (*Windows 7 SP1* or newer).

The *IVI Shared Components* and *IVI.NET Shared Components* packages are mandatory to be installed on a system that will
run the software containing the library (not required for compilation). Other vendor-specific software packages like
*NI-VISA* or *Keysight IO libraries Suite* may also be required to communicate with different equipment. *.NET*
assemblies distributed with such packages may need to be directly referenced in a project to access custom *VISA*
resource managers used for establishing sessions with devices.

## Third-party packages

This library uses these open-source third-party *NuGet* packages as dependencies:

* **Kelary.Ivi.Visa** - An unofficial package for the *IVI Foundation VISA.NET Shared Components*.

These packages are used for unit tests coverage only:

* **coverlet.collector**
* **Microsoft.NET.Test.Sdk**
* **Moq**
* **xunit**
* **xunit.runner.visualstudio**

## Links

* **[VisaDeviceBuilder repository](https://github.com/Egiraht/VisaDeviceBuilder/tree/master/VisaDeviceBuilder)**
* **[.NET platform site](https://dotnet.microsoft.com/)**
* **[.NET runtime packages](https://dotnet.microsoft.com/download/dotnet/5.0)**
* **[IVI Foundation site](https://www.ivifoundation.org)**
* **[IVI Shared Components page](https://www.ivifoundation.org/shared_components/Default.aspx)**
* **[Kelary.Ivi.Visa package](https://www.nuget.org/packages/Kelary.Ivi.Visa/)**
* **[Coverlet repository](https://github.com/coverlet-coverage/coverlet)**
* **[Moq repository](https://github.com/moq/moq4)**
* **[xUnit.net site](https://xunit.net)**

## License

This library is licensed under terms and conditions of the **[Mozilla Public Licence Version 2.0](LICENSE.txt)**.

Copyright © 2020-2021 Maxim Yudin (Egiraht)

# DIPOL-UF (DIPOL-3)
[![](https://img.shields.io/badge/DOI-10.3847%2F1538--3881%2Fabc74f-brightgreen)](https://iopscience.iop.org/article/10.3847/1538-3881/abc74f)
[![](https://img.shields.io/badge/BIBCODE-%20%20%20%20%202021AJ....161...20P%20-brightgreen)](https://ui.adsabs.harvard.edu/#abs/2021AJ....161...20P/abstract)
[![](https://img.shields.io/badge/arXiv-2011.02129-brightgreen)](https://arxiv.org/abs/2011.02129)

![dipol-uf-gui](https://user-images.githubusercontent.com/8782986/119700521-fb28f980-be5b-11eb-9c10-2347120588dc.png)

This repository contains software needed for operation of high-precision three-color BVR polarimeter DIPol-UF.
The libraries here are designed to handle low-level communication with Trinamic stepper motors, utlize ANDOR SDK capabilities to control three (or any other number) ANDOR iXon Ultra 897 CCDs, acquire and serailzie obtained images in FITS format.

The current version does not support full range of features of an ANDOR-compatible camera. A subset of regimes needed for DIPol-UF is implemented and tested.
The system is scalable in a sence that it supports RPC communication over the network. Each camera can be controlled by a separate Windows machine, and the number of such machines is limited by the network capabilities (DIPol-UF uses one primary cotntrol PC and to seondary PCs which provide RPC capabilities).
Due to the nature of polarimetric observations, DIPol-UF does not requrie extremely precies triggering mechanisms (a typical exposure time is ~1-60 s with a 0.250 s overhead for discrete plate rotation after each exposure), so no support for external triggers is built in.

The GUI is built using WPF. RPC is implemented using WCF.
The project was initiated well before the release of stable .NET Core (with at least WPF support), so core GUI and RPC features are .NET Frmework 4.8-only.
Auxilary libraries target .NET Standard 2.0 and are fully cross-platform.

An effort is made to solve some of the problems present in code base and to migrate all libraries to .NET Standard 2.0/2.1 with executables targeting .NET 5+.


---
## Project structure

- [**ANDOR-CS**](./src/ANDOR-CS) is the high level wrapper around native ANDOR libs and provides tools to control local ANDOR cameras.

- [**DIPOL-Remote**](./src/DIPOL-Remote) provides (net.tcp) service and client allowing to remotely control camera connected to another computer.

- [**DIPOL-UF**](./src/DIPOL-UF) is the GUI project oriented to end users.

- [**FITS-CS**](./src/FITS-CS) provides IO support for FITS images (only one-image per file, no other extensions).

- [**StepMotor**](./src/StepMotor) provides tools for interaction with Step Motor - device responsible for phase plate rotation during polarimetric observations.

- [**DipolImage**](./src/DipolImage) contains Image class that handles all image transport in a project.

- [**Host**](./src/Host) is a standalone application (.exe) that is launched on each of the secondary PC and allows over-the-network communication with the cameras.

---
## Building and testing
The `ANDOR-CS` project depends on the ANDOR SDK, which is proprietary and is not distributed. 
When obtained, the SDK can be directly referenced in the `*.csproj` instead of a placeholder nuget package.
The ANDOR SDK libraries are architecture sensitive (including the C# wrappers), so take this into account.

The subset of tests of auxilary functions is found in `/tests/`.
The test solution can be executed as is using .NET 5+.
By default, it targets .NET Framewrok 4.8, .NET 5 and `$(DOTNET_EXTRA_TARGET)` environemnt variable, which can be set to target to any other .NET version (e.g., .NET 6 preview).
The tests are then executed as 

```dotnet test ./tests/Tests.sln -c Release -f <TESTED_FRAMEWOR>```,

where `<TESTED_FRAMEWORK>` can be `net4.8`, `net5.0`, etc.

To build `Host` and `DIPOL-UF`, msbuild should be used.
Providing the ANDOR dependencies are added, the GUI can be built as

```msbuild ./src/DIPOL-UF/DIPOL-UF.csproj -p:Configuration=Relase -p:Platform=<PLATFORM>```, 

where `<PLATFORM>` is either `x64` or `x86` (for 32-bit).

Examples of build & test workflows can be found in the [CI scripts](./.github/workflow)

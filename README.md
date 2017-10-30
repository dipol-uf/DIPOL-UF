# DIPOL-3 (DIPOL-UF)
A software package for observations with DIPOL-UF (version 3) polarimeter.
Software is being developed as part of a scientific project done at the [**@TuorlaObservatory**](https://github.com/TuorlaObservatory).

---
## Project structure

- [**ANDOR-CS**](./ANDOR-CS) is the high level wrapper around native ANDOR libs and provides tools to control local ANDOR cameras.

- [**DIPOL-Remote**](./DIPOL-Remote) provides (net.tcp) service and client allowing to remotely control camera connected to another computer.

- [**DIPOL-UF**](./DIPOL-UF) is the GUI project oriented to end users.

- [**FITS-CS**](./FITS-CS) provides IO support for FITS images (only one-image per file, no other extensions).

- [**StepMotor**](./StepMotor) provides tools for interaction with Step Motor - device responsible for plate rotation durin polarimetric observations.

- [**Image**](./Image) contains Image class that handles all image transport in a project.

- [**ImageDisplay**[(./ImageDisplay) is a WPF control that can display [Image](./Image).


- [**Client**](./Client), [**Host**](./Host), [**ImageTest**](./ImageTest) are test projects used at debug phase.

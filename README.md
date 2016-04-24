# PlotterWritterDLL
Source code for libraries &amp; test app for sending HPGL commands to legacy XY pen plotter to USB serial &amp; parallel ports

This solution creates a DLL that offers a CLI interface which wraps code written to communicate with legacy devices attached via USB-to-serial and USB-to-parallel cables.  It is being developed for communicating with a Houston Instruments Image Maker XY pen plotter, compatible with the better-known HP7475A plotter.  It is a work in process and is not yet complete, but what is here is fully functional and has been used it to make the plotter draw circles using the HPGL commands in the test driver app.

The DLL is written in both native C++ and C++/CLI.  The native C++ makes all the Win32 calls and does all the work of communicating with the plotter, while the C++/CLI code wraps the native C++ code and exposes a CLI interface that can be called by .net languages.  In this case the test app is written in C#.

The project PlotterWriterDLL contains all the C++ code used in the solution.  Although not yet complete, it does all the work of locating the USB port and sending data out on it.  It contains an abstract base class from which the wrapper classes for both serial and parallel port communication are derived so that both may be used polymorphically, as is done in the PlotterBuffer project.

PlotterBuffer is intended for use as a buffer for optimizing a series of plot commands in HPGL based on pen color and on distance between start and stop points of the commands to minimize plotter pen travel between drawing tasks.

The last project, PlotterWritterDLLTester, is a simple test app for driving all the active code.  Eventually it will be replaced by a user-friendly graphical app.

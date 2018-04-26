# PlotterWriter Readme

This solution creates a DLL that exposes a CLI interface which wraps code written to communicate with legacy devices attached via USB-to-serial and USB-to-parallel cables.  It has been developed for communicating with a Houston Instruments Image Maker graphic pen plotter, compatible with the better-known HP7475A plotter.  The code is fully functional and supports each HPGL command and instruction that the Image Maker supports, including code to read a series of digitized points and create a series of HPGL instructions to plot those same coordinates.

The DLL is written in both native C++ and C++/CLI.  The native C++ makes all the Win32 calls and does all the work of communicating with the plotter, while the C++/CLI code wraps the native C++ code and exposes a CLI interface that can be called by .net languages.  In this case the test app is written in C#.

The project PlotterDriver contains all the C++ code used in the solution.  It does all the work of locating the USB port, sending data out on it, and reading data from the plotter.  Is also contains a write-only class for sending HPGL to the plotter on the parallel port.  It contains an abstract base class from which the wrapper classes for both serial and parallel port communication are derived so that both may be used polymorphically, as is done in the PlotterBuffer and PlotterTestApp projects.

PlotterBuffer is a class for optimizing a series of plot commands in HPGL based on pen color.  It is not yet complete, but does all the work needed to sort by pen color.  Planned enhancements include optimizing the instruction sequence to minimize pen travel between drawing tasks, and to track all HPGL-controlled status settings such as character size, plot speed, and all others that can be set by commands that don't cause the pen to be moved.

The latest version includes code to draw some complex shapes in including image rotation, sine waves, and Lissajous curves.  It also has code foe drawing images based on string art images found on the Internat.

A real user interface is planned for sometime in the future.

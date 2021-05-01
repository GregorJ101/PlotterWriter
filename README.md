# PlotterWriter Readme

This solution is a complete overhaul of the previous version and incorporates the following changes:
- The C++ DLL has been removed and replaced with all C# code using the .NET framework exclusively
- The unit-test app has been removed and replaced with a comprehensive Console app
- Output to the serial port is multi-threaded
- Code has been added to track output progress
- Code has been added to abort plot and clear the output queue and the plotter buffer
- Code has been added code to write to .HPGL files (viewable using HPGL viewers abailable on the web)

This solution was written to communicate with legacy devices attached via USB-to-serial and USB-to-parallel cables.  It has been developed for communicating with a Houston Instruments Image Maker graphic pen plotter, compatible with the better-known HP7475A plotter.  The code is fully functional and supports each HPGL command and instruction that the Image Maker supports, including code to read a series of digitized points and create a series of HPGL instructions to plot those same coordinates.

The PlotterDriver class contains the code that does all the work of locating the USB serial port, sending data out on it, and reading data from the plotter.  Is also contains a write-only class for sending HPGL to the plotter on the parallel port.

PlotterEngine is a class for optimizing a series of plot commands in HPGL based on pen color.  It does all the work needed to sort by pen color and by shortest travel distance to the next shape to be drawn that uses the currently selected pen.

A short demo video is available on YouTube: https://youtu.be/S7zcH0BfxzU

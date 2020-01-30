# ProConsole
Library to extend the (mainly graphical) capabilities of the Windows console. Not in active development, but updated occasionally as I tinker with other console projects.
## Main features
- 16-bit color mode
- Customizable window header functionality (disable minimize button, etc.)
- Draw images from files (currently only Resources) in up to "4 voxel per 2 character" resolution (transparency supported!)
- Print easily-aligned, multicolor text lines, even over images with fake transparency!
- Metaltext! (dynamic gradient colors)
- Built-in border drawing functions
- Menu system [WIP]
## Installation
Add *ProConsole.dll* as a dependency to your console project. Import the **ProConsole** namespace.
## Usage
ProConsole is not a ready product, so some manual assembly is yet required.

At the start of your **Main** function, use **SetWindowSize** and **SetBufferSize** (from *System.Console*) to set up your "canvas." For programs using ProConsole you should set both to the same value, as you probably don't want to use scrolling. You may also disable the mouse cursor (Console.CursorVisible = False), set your window title, etc.

Call **InitProConsole**. There are three optional arguments:
- *bool* **disableEditing** = *true*; disable text selection in the console window
- *bool* **disableResizing** = *true*; disable resizing of the console window
- *bool* **disableCloseAndMinimize** = *true*; disable the Close and Minimize window buttons

You can now use the other functions.
## Functions
Under construction. Feel free to look at the source meanwhile ;-).

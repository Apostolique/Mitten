# Mitten
Infinite canvas drawing application.

## Showcase

![Showcase](./Images/showcase.gif)

## Builds

Grab the builds <https://apos.itch.io/mitten>.

## Controls

### Draw

* Left click to draw.
* Shift + Left click to draw lines.
* Control + Shift + Left click to change the brush size. Drag left to shrink the size and right to increase it.
* Alt + Hover to select a different color.
* Control + Alt + Hover to select a different background color.
* E to toggle the eraser.

### Camera

* Middle click to drag the camera.
* Scroll wheel to zoom.
* Control + Middle click to zoom. Drag up to zoom in and down to zoom out.
* Dot and Comma to rotate.
* Hold Space use the hyper zoom. Release to go back to the previous position.
* Save the current camera position with Control + 1, Control + 2, all the way to Control + 9.
* Load a saved camera position by pressing 1, 2, ... 9.
* Slot 0 is reserved for toggling back and forth between the current and previous position. You can also toggle using your mouse's extra buttons if you have them.

### Misc

* Control + Z to undo.
* Control + Shift + Z to redo.
* Control + Backspace to undo everything.
* Control + Shift + Backspace to redo everything.
* F11 for the borderless fullscreen mode.
* Alt + Enter for the fullscreen mode.
* M to show or hide the mouse cursor.

## Saved files

Saved next to the application's executable.

* Drawing.json - Your whole canvas is saved there including undo redo and camera position.
* Settings.json - Window settings are saved here. Includes if the app should start in fullscreen, vsync and fixed timestep.

## Restore

```
dotnet restore Platforms/DesktopGL
dotnet restore Platforms/WindowsDX
```

## Run

```
dotnet run --project Platforms/DesktopGL
dotnet run --project Platforms/WindowsDX
```

## Debug

In vscode, you can debug by pressing F5.

## Publish

```
dotnet publish Platforms/DesktopGL -c Release -r win-x64 --output artifacts/windows
dotnet publish Platforms/DesktopGL -c Release -r osx-x64 --output artifacts/osx
dotnet publish Platforms/DesktopGL -c Release -r linux-x64 --output artifacts/linux
```

```
dotnet publish Platforms/WindowsDX -c Release -r win-x64 --output artifacts/windowsdx
```

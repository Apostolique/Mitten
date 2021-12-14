# Mitten
Infinite canvas drawing application.

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

### Misc

* Control + Z to undo.
* Control + Shift + Z to redo.
* F11 for the borderless fullscreen mode.
* Alt + Enter for the fullscreen mode.

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

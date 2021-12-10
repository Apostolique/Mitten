# Mitten
Infinite canvas drawing application.

## Controls

* Left click to draw.
* Shift + Left click to draw lines.
* Control + Scroll wheel to change the line thickness.
* Alt + Hover to select a different color.
* Middle click to drag the camera.
* Scroll wheel to zoom.
* Dot and Comma to rotate.
* Control + Z to undo.
* Control + Shift + Z to redo.
* F11 for the borderless fullscreen mode.
* Alt + Enter for the fullscreen mode.

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

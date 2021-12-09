# MoltenDraw
Infinite canvas drawing application.

## Controls

* Left click to draw.
* Shift + Left click to draw lines.
* Control + Scroll wheel to change the line thickness.
* Middle click to drag the camera.
* Scroll wheel to zoom.
* Dot and Comma to rotate.
* Control + Z to undo.
* Control + Shift + Z to redo.

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
dotnet publish Platforms/DesktopGL -c Release -r win-x64 --output build-windows
dotnet publish Platforms/DesktopGL -c Release -r osx-x64 --output build-osx
dotnet publish Platforms/DesktopGL -c Release -r linux-x64 --output build-linux
```

```
dotnet publish Platforms/WindowsDX -c Release -r win-x64 --output build-windowsdx
```

# Mitten
Infinite canvas drawing application.

## Showcase

![Showcase](./Images/showcase.gif)

## Builds

Grab the builds <https://apos.itch.io/mitten>. It also runs in the browser there.

## Controls

### Draw

* Left click to draw.
* Shift + Left click to draw lines.
* Control + Shift + Left click to change the brush size. Drag left to shrink the size and right to increase it.
* R to unlink the pen and eraser sizes so each tool remembers its own. Press R again to relink them; the current tool's size wins. While unlinked, the eraser cursor shows a second inner ring.
* Save the current tool's size with Control + Shift + 1, all the way to Control + Shift + 9. Load a saved size by pressing Shift + 1, ... Shift + 9.
* Alt + Hover to select a different color.
* Control + Alt + Hover to select a different background color.
* E to toggle the eraser.
* T to toggle the temporary mode. Strokes aren't saved and erase themselves from their starting point shortly after pen-down, retracing the stroke at the speed it was drawn — the oldest ink disappears first. Useful for pointing at things during a presentation. The delay and decay speed can be tuned in Settings.json (`tempDelaySeconds`, `tempDecaySpeed`).

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
* B to box select and edit strokes.

In the browser, F11 and Alt + Enter do nothing and Escape doesn't quit. Use itch.io's own
fullscreen button instead.

## Saved files

Saved next to the application's executable. On macOS they go to
`~/Library/Application Support/Mitten` instead, since the executable lives inside
`Mitten.app` and updating the app would take the bundle's contents with it.

* Drawing.json - Your whole canvas is saved there including undo redo and camera position.
* Settings.json - Window settings are saved here. Includes if the app should start in fullscreen, vsync and fixed timestep.

In the browser there is no directory to write to, so the same files go to the browser's own
storage under a `mitten/` prefix. The drawing goes to IndexedDB and the rest to
localStorage, which caps out around 5 MB and is shared with every other html5 game on
itch.io. Clearing site data for the page clears the drawing with it.

Nothing in a browser corresponds to quitting, so the drawing is written every 30 seconds and
again whenever the page is hidden, rather than on the way out.

## Restore

```
dotnet restore Platforms/DesktopGL
dotnet restore Platforms/WindowsDX
dotnet restore Platforms/BlazorGL.KNI
```

## Run

```
dotnet run --project Platforms/DesktopGL
dotnet run --project Platforms/WindowsDX
```

The browser build has to be tested against published output rather than `dotnet run`. The
font is linked in as a static web asset, which only lands in `wwwroot` on publish, so
`dotnet run` serves a 404 for it and the game dies while loading. Publish it and serve the
directory:

```
dotnet publish Platforms/BlazorGL.KNI -c Release --output artifacts/web
```

## Debug

In vscode, you can debug by pressing F5.

## Publish

```
dotnet publish Platforms/DesktopGL -c Release -r win-x64 --output artifacts/windows
dotnet publish Platforms/DesktopGL -c Release -r linux-x64 --output artifacts/linux
```

```
dotnet publish Platforms/WindowsDX -c Release -r win-x64 --output artifacts/windowsdx
```

The browser build goes up to itch.io's `html5` channel, which serves the contents of
`artifacts/web/wwwroot`:

```
dotnet publish Platforms/BlazorGL.KNI -c Release --output artifacts/web
```

macOS goes through a script, since the build has to end up inside a `.app` bundle:

```
./Platforms/DesktopGL/package-osx.sh 3.1.2 artifacts/osx
```

It publishes `osx-arm64` and `osx-x64`, puts both in `Mitten.app` next to a launcher that
picks one at startup, and ad-hoc signs every binary. You need a Mac to run it. `codesign`
only exists there, and Apple Silicon kills unsigned binaries the moment they launch.

The app isn't notarized. Downloaded from a browser, macOS calls it damaged the first time.
You can open System Settings > Privacy & Security and click "Open Anyway". Through the itch
app it just runs.

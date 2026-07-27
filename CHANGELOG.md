# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

- Nothing yet!

## [3.2.0] - 2026-07-26

### Changed

- Strokes now render as one continuous path instead of a capsule per segment, so joints blend once instead of stacking. F3 toggles back to per-segment capsules.
- Pen pressure now varies a stroke's width smoothly between samples instead of stepping at every segment.
- Updated to Apos.Shapes 0.7.11.

### Fixed

- macOS builds now open on current macOS. They ship as a `Mitten.app` bundle, native on both Apple Silicon and Intel, and render at full resolution on Retina displays.
- On macOS, drawings and settings are saved to `~/Library/Application Support/Mitten` so updating the app can't take them along.

## [3.1.2] - 2026-07-18

### Fixed

- Drawing near a nested cell corner at high zoom (reached by zooming in place) no longer snaps strokes to a grid or collapses them onto the corner: strokes are now re-homed to their frame with exact integer cell arithmetic, and rendering and hit testing follow ink across the corner no matter how deep it is.
- Zooming deep inside existing ink no longer tanks the frame rate: strokes that cover the whole screen now join the occlusion cutoff, so everything underneath them skips its fill instead of stacking hundreds of full-screen quads.
- Strokes anchored across a deep cell corner no longer pop out a few zoom levels in: the tree-query height cap is gone, since past two levels every visible stroke reduces to a full cover or screen-local edge anyway, which is stable at any depth.

## [3.1.1] - 2026-07-12

### Added

- Brush sizes are now saved to a slot so they persist between sessions.

### Changed

- The eraser size is now desynced from the brush size, so each can be adjusted independently.

## [3.1.0] - 2026-07-12

### Added

- Added an edit mode for selecting, moving, and scaling existing strokes.
- Added a temporary mode.
- The mouse now wraps around the edges of the screen while dragging.

## [3.0.1] - 2026-07-11

### Added

- Added tablet support for Linux (pen pressure through XInput2).

## [3.0.0] - 2026-07-11

### Added

- Added true infinite zoom. The canvas is now backed by an infinite frame tree, so you can keep zooming in or out without losing precision.

## [2.3.0] - 2025-10-19

### Changed

- Updated Apos.Shapes.

## [2.2.4] - 2025-08-18

### Added

- Added a way to toggle the mouse.

## [2.2.3] - 2025-08-10

### Changed

- Updated to MonoGame 3.8.4.

## [2.2.2] - 2025-05-11

### Changed

- Updated to MonoGame 3.8.4-preview.2.

## [2.2.1] - 2025-05-11

### Changed

- Rolled back MonoGame to wait for the next release.

## [2.2.0] - 2025-05-11

### Changed

- Updated to MonoGame 3.8.3.

## [2.1.1] - 2024-12-27

### Changed

- Updated to MonoGame 3.8.2.1105.

## [2.1.0] - 2024-12-15

### Added

- Added `UndoAll` and `RedoAll`.
- Added color palette loading from a file.

### Changed

- Renamed `DrawWith` to `StrokeWith`.

## [2.0.7] - 2024-04-10

### Fixed

- Improved line drawing with a tablet.

## [2.0.6] - 2024-04-03

### Fixed

- Made sure the tablet is valid before drawing with it.

## [2.0.5] - 2024-03-06

### Changed

- Refactored the tablet code and enabled line drawing with it.
- Updated Apos.Shapes.

## [2.0.4] - 2024-03-05

### Fixed

- Only close the tablet context if it's set ([#11](https://github.com/Apostolique/Mitten/pull/11)).

## [2.0.3] - 2024-02-27

### Added

- Added the `X` key to drag the canvas.

## [2.0.2] - 2024-02-23

### Fixed

- Moved the device print into a try/catch.

## [2.0.1] - 2024-02-23

### Changed

- Allowed a console window for now.

## [2.0.0] - 2024-02-21

### Added

- Initial support for tablets on Windows (pen pressure through Wintab) ([#10](https://github.com/Apostolique/Mitten/pull/10)).

### Changed

- Updated to Apos.Spatial 0.4.1.

## [1.3.4] - 2023-12-11

### Changed

- Build pipeline caching tweaks (no functional changes).

## [1.3.3] - 2023-12-11

### Changed

- Build pipeline caching tweaks (no functional changes).

## [1.3.2] - 2023-12-11

### Changed

- Use the wine cache for the pipeline.

## [1.3.1] - 2023-11-21

### Added

- Added the version number to releases.

## [1.3.0] - 2023-11-21

### Changed

- Updated to .NET 8 and Apos.Shapes.

## [1.2.3] - 2023-08-22

### Changed

- Updated Apos.Shapes.

## [1.2.2] - 2023-08-12

### Fixed

- Slot 0 now saves the final camera destination.

## [1.2.1] - 2023-08-12

### Added

- Added another mouse button to toggle the camera.

### Changed

- Load the preserved camera zoom on startup.

## [1.2.0] - 2023-08-12

### Added

- Added camera marks ([#6](https://github.com/Apostolique/Mitten/issues/6)).

## [1.1.14] - 2023-07-07

### Changed

- Use a source generator for JSON serialization.

## [1.1.13] - 2023-04-03

### Changed

- Re-release of 1.1.12 (no functional changes).

## [1.1.12] - 2023-04-03

### Added

- Added new TW colors and show the color index.
- Added a background indicator in the color picker.

## [1.1.11] - 2023-03-17

### Added

- Added a showcase gif to the readme.

### Changed

- Updated to MonoGame 3.8.1.303.
- Switched to WindowsDX.

## [1.1.10] - 2022-01-31

### Changed

- Switched to the HiDef profile.

### Fixed

- Attempted to fix the icon build under Linux.

## [1.1.9] - 2021-12-14

### Fixed

- Fixed redoing eraser lines.

## [1.1.8] - 2021-12-14

### Changed

- Reworked hyper zoom.

### Fixed

- Fixed the eraser.
- Fixed eraser line save and load.

## [1.1.7] - 2021-12-13

### Added

- Added the eraser.

## [1.1.6] - 2021-12-13

### Added

- Added hyper zoom.

### Fixed

- Fixed cursor lag.

## [1.1.5] - 2021-12-12

### Added

- Added a zoom line.

### Changed

- Updated to .NET 6.

## [1.1.4] - 2021-12-12

### Added

- Added right mouse to drag the camera.

### Changed

- Changed drag zoom into a mouse control.

## [1.1.3] - 2021-12-11

### Added

- The camera is now saved with the drawing.

## [1.1.2] - 2021-12-11

### Changed

- New icon.

## [1.1.1] - 2021-12-10

### Fixed

- Put a cap on drag zoom.

## [1.1.0] - 2021-12-10

### Added

- Added the ability to pick a background color.
- Added zoom control for tablets.

### Changed

- New way to change the line thickness.

## [1.0.2] - 2021-12-10

### Added

- Added the color picker.

### Fixed

- Fixed colors.

## [1.0.1] - 2021-12-09

### Added

- Added fullscreen and window state saving.
- Set up color save and load.

### Fixed

- Fixed redo.

## [1.0.0] - 2021-12-09

### Added

- Initial release. A drawing app built on MonoGame.
- Adaptive line size and line thickness editing.
- Drawing of both lines and non-line strokes.
- Undo and redo (disabled while drawing).
- Saving drawings to disk.

[Unreleased]: https://github.com/Apostolique/Mitten/compare/v3.2.0...HEAD
[3.2.0]: https://github.com/Apostolique/Mitten/compare/v3.1.2...v3.2.0
[3.1.2]: https://github.com/Apostolique/Mitten/compare/v3.1.1...v3.1.2
[3.1.1]: https://github.com/Apostolique/Mitten/compare/v3.1.0...v3.1.1
[3.1.0]: https://github.com/Apostolique/Mitten/compare/v3.0.1...v3.1.0
[3.0.1]: https://github.com/Apostolique/Mitten/compare/v3.0.0...v3.0.1
[3.0.0]: https://github.com/Apostolique/Mitten/compare/v2.3.0...v3.0.0
[2.3.0]: https://github.com/Apostolique/Mitten/compare/v2.2.4...v2.3.0
[2.2.4]: https://github.com/Apostolique/Mitten/compare/v2.2.3...v2.2.4
[2.2.3]: https://github.com/Apostolique/Mitten/compare/v2.2.2...v2.2.3
[2.2.2]: https://github.com/Apostolique/Mitten/compare/v2.2.1...v2.2.2
[2.2.1]: https://github.com/Apostolique/Mitten/compare/v2.2.0...v2.2.1
[2.2.0]: https://github.com/Apostolique/Mitten/compare/v2.1.1...v2.2.0
[2.1.1]: https://github.com/Apostolique/Mitten/compare/v2.1.0...v2.1.1
[2.1.0]: https://github.com/Apostolique/Mitten/compare/v2.0.7...v2.1.0
[2.0.7]: https://github.com/Apostolique/Mitten/compare/v2.0.6...v2.0.7
[2.0.6]: https://github.com/Apostolique/Mitten/compare/v2.0.5...v2.0.6
[2.0.5]: https://github.com/Apostolique/Mitten/compare/v2.0.4...v2.0.5
[2.0.4]: https://github.com/Apostolique/Mitten/compare/v2.0.3...v2.0.4
[2.0.3]: https://github.com/Apostolique/Mitten/compare/v2.0.2...v2.0.3
[2.0.2]: https://github.com/Apostolique/Mitten/compare/v2.0.1...v2.0.2
[2.0.1]: https://github.com/Apostolique/Mitten/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/Apostolique/Mitten/compare/v1.3.4...v2.0.0
[1.3.4]: https://github.com/Apostolique/Mitten/compare/v1.3.3...v1.3.4
[1.3.3]: https://github.com/Apostolique/Mitten/compare/v1.3.2...v1.3.3
[1.3.2]: https://github.com/Apostolique/Mitten/compare/v1.3.1...v1.3.2
[1.3.1]: https://github.com/Apostolique/Mitten/compare/v1.3.0...v1.3.1
[1.3.0]: https://github.com/Apostolique/Mitten/compare/v1.2.3...v1.3.0
[1.2.3]: https://github.com/Apostolique/Mitten/compare/v1.2.2...v1.2.3
[1.2.2]: https://github.com/Apostolique/Mitten/compare/v1.2.1...v1.2.2
[1.2.1]: https://github.com/Apostolique/Mitten/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/Apostolique/Mitten/compare/v1.1.14...v1.2.0
[1.1.14]: https://github.com/Apostolique/Mitten/compare/v1.1.13...v1.1.14
[1.1.13]: https://github.com/Apostolique/Mitten/compare/v1.1.12...v1.1.13
[1.1.12]: https://github.com/Apostolique/Mitten/compare/v1.1.11...v1.1.12
[1.1.11]: https://github.com/Apostolique/Mitten/compare/v1.1.10...v1.1.11
[1.1.10]: https://github.com/Apostolique/Mitten/compare/v1.1.9...v1.1.10
[1.1.9]: https://github.com/Apostolique/Mitten/compare/v1.1.8...v1.1.9
[1.1.8]: https://github.com/Apostolique/Mitten/compare/v1.1.7...v1.1.8
[1.1.7]: https://github.com/Apostolique/Mitten/compare/v1.1.6...v1.1.7
[1.1.6]: https://github.com/Apostolique/Mitten/compare/v1.1.5...v1.1.6
[1.1.5]: https://github.com/Apostolique/Mitten/compare/v1.1.4...v1.1.5
[1.1.4]: https://github.com/Apostolique/Mitten/compare/v1.1.3...v1.1.4
[1.1.3]: https://github.com/Apostolique/Mitten/compare/v1.1.2...v1.1.3
[1.1.2]: https://github.com/Apostolique/Mitten/compare/v1.1.1...v1.1.2
[1.1.1]: https://github.com/Apostolique/Mitten/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/Apostolique/Mitten/compare/v1.0.2...v1.1.0
[1.0.2]: https://github.com/Apostolique/Mitten/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/Apostolique/Mitten/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/Apostolique/Mitten/releases/tag/v1.0.0

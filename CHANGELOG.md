# Changelog

All notable changes to Expression Tree Explorer are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2026-06-15

### Changed
- Updated the extension icon (added a lambda glyph to the root node).

## [1.0.0] - 2026-06-15

### Added
- Initial release.
- Debugger visualizer for `Expression<TDelegate>` variables (VisualStudio.Extensibility, Remote UI).
- Tree view of the full expression tree with color-coded badges (node kind, `ExpressionType`, static type) and expand/collapse all.
- Detail panel: node properties, copyable debugger watch expression, Copy Watch button, and a link to the MS docs for the node type.
- Source panel: readable C# with tree-to-source span highlighting and format switching (Readable / `ToString` / DebugView).
- End nodes tab: parameters, constants, closed-over variables, and defaults.
- Runtime value extraction for constants and closure variables.
- VS theme-aware styling.
- Support for Visual Studio 2022 (17.9+) and Visual Studio 2026.

[1.0.1]: https://github.com/TomasPecinka/ExpressionTreeExplorer/releases/tag/v1.0.1
[1.0.0]: https://github.com/TomasPecinka/ExpressionTreeExplorer/releases/tag/v1.0.0

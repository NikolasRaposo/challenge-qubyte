# CHANGELOG

All notable changes to this project are documented here.

## [0.2.0] - 2025-11-04
### Added
- Improved ProBuilder support with alignment to the "Element" mode:
  - Move (`G`): Local axis aligned to the element (Face/Edge/Vertex) with UV/Normals/Bounds refresh.
  - Rotate (`R`): Local axis based on the element rotation with corrected angle inversion per axis.
  - Scale (`S`): scaling along the element’s Local axis.
- Axis lock: respects `Element` orientation in ProBuilder context without changing `Tools.pivotRotation`.
- Documentation updated with installation via Git URL and Disk, and quick validation guide.

### Changed
- Internal axis computation improvements using `UnityEngine.ProBuilder.HandleUtility` to obtain element orientation.

### Fixed
- Visual artifacts in UVs and Normals after transforms in Element mode.

## [0.0.1] - 2023-12-22
- Initial release.
# Changelog

## [1.0.0] – 2026-02
### Fixed
- Audio playback now respects ignore lists and privacy settings.
- Improved audio ID and username parsing.
- Reduced memory usage during concurrent playback.

### Added
- Enhanced MyInstants extraction.
- Smart audio caching with automatic cleanup.
- Audio URL validation before library insertion.
- Improved error messages.

### Changed
- Optimized Base64 encoding/decoding.
- Reduced latency when playing cached audio.
- Improved handling of network interruptions.

### Known Issues
- Large audio files (>10MB) may not be cached.
- Cache is cleared on server restart.

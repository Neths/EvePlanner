# Versioning Strategy

This project follows **Semantic Versioning (SemVer)**: `MAJOR.MINOR.PATCH`

## Version Format

```
v1.2.3
 │ │ │
 │ │ └─ PATCH: Bug fixes, minor changes (backward compatible)
 │ └─── MINOR: New features (backward compatible)
 └───── MAJOR: Breaking changes (not backward compatible)
```

## Version Increments

### MAJOR Version (v2.0.0)

Increment when making **incompatible API changes**:
- Database schema breaking changes
- Configuration format changes
- API endpoint changes (removal or parameter changes)
- Behavior changes that break existing functionality

### MINOR Version (v1.1.0)

Increment when adding **new features** in a backward-compatible manner:
- New ESI endpoints collected
- New collector services
- New database tables (additive only)
- New API endpoints
- Performance improvements

### PATCH Version (v1.0.1)

Increment for **backward-compatible bug fixes**:
- Bug fixes
- Security patches
- Minor documentation updates
- Dependency updates (non-breaking)

## Pre-release Versions

For development and testing:

```
v1.0.0-alpha.1    → Early development
v1.0.0-beta.1     → Feature complete, testing
v1.0.0-rc.1       → Release candidate
```

## Version Lifecycle

### Development Phase (0.x.x)

Initial development, API may change:
- `v0.1.0` → Phase 1 MVP (Universe data)
- `v0.2.0` → Phase 2 (Wallet, Assets, Orders)
- `v0.3.0` → Phase 3 (Market data)
- `v0.4.0` → Phase 4 (Industry data)

### Stable Release (1.x.x)

Production-ready:
- `v1.0.0` → First stable release
- `v1.1.0` → New features
- `v1.1.1` → Bug fixes

## Planned Releases

### Phase 1: MVP - Universe Data
**Target:** v0.1.0
- Universe static data collection
- Basic infrastructure
- Docker setup

### Phase 2: Authenticated Data
**Target:** v0.2.0
- SSO authentication
- Wallet data
- Assets with diff tracking
- Orders (character + corp)

### Phase 3: Public Market Data
**Target:** v0.3.0
- Market orders
- Market history
- Multi-region support

### Phase 4: Industry Data
**Target:** v0.4.0
- Industry indices
- Industry jobs
- Full monitoring

### Phase 5: Production Ready
**Target:** v1.0.0
- Complete test coverage
- CI/CD pipeline
- Performance optimizations
- Full documentation
- Stable API

## Version Management

### Tagging

```bash
# Create annotated tag
git tag -a v1.0.0 -m "Release version 1.0.0"

# Push tag to remote
git push origin v1.0.0

# List all tags
git tag -l
```

### CHANGELOG.md

Every release should update `CHANGELOG.md`:

```markdown
## [1.0.0] - 2025-01-15

### Added
- Universe data collector
- SSO authentication
- Asset diff tracking

### Changed
- Improved rate limiting strategy

### Fixed
- Token refresh infinite loop

### Removed
- Deprecated legacy endpoints
```

## Current Version

**v0.0.0** - Initial setup (pre-release)

Next planned release: **v0.1.0** (Phase 1 MVP)

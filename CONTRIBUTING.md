# Contributing Guide

## Git Flow Workflow

This project uses **Git Flow** as the branching strategy.

### Branch Structure

```
main           → Production-ready code (releases only)
develop        → Integration branch (latest development)
feature/*      → New features
release/*      → Release preparation
hotfix/*       → Production hotfixes
```

### Branch Naming Convention

- **Feature branches**: `feature/<issue-number>-<short-description>`
  - Example: `feature/001-universe-data-collector`

- **Release branches**: `release/<version>`
  - Example: `release/1.0.0`

- **Hotfix branches**: `hotfix/<version>-<short-description>`
  - Example: `hotfix/1.0.1-fix-rate-limiting`

### Workflow

#### Starting a New Feature

```bash
# Make sure you're on develop and up to date
git checkout develop
git pull origin develop

# Create a new feature branch
git checkout -b feature/001-universe-data-collector

# Work on your feature...
git add .
git commit -m "feat: implement universe data collector"

# Push your feature branch
git push -u origin feature/001-universe-data-collector
```

#### Completing a Feature

```bash
# Make sure develop is up to date
git checkout develop
git pull origin develop

# Merge feature into develop (use --no-ff to preserve branch history)
git merge --no-ff feature/001-universe-data-collector

# Delete the feature branch
git branch -d feature/001-universe-data-collector
git push origin --delete feature/001-universe-data-collector

# Push develop
git push origin develop
```

#### Creating a Release

```bash
# Create release branch from develop
git checkout develop
git pull origin develop
git checkout -b release/1.0.0

# Update version numbers, CHANGELOG, etc.
# Commit changes
git commit -am "chore: prepare release 1.0.0"

# Push release branch
git push -u origin release/1.0.0

# When ready to release:
# Merge to main
git checkout main
git pull origin main
git merge --no-ff release/1.0.0
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin main --tags

# Merge back to develop
git checkout develop
git merge --no-ff release/1.0.0
git push origin develop

# Delete release branch
git branch -d release/1.0.0
git push origin --delete release/1.0.0
```

#### Creating a Hotfix

```bash
# Create hotfix branch from main
git checkout main
git pull origin main
git checkout -b hotfix/1.0.1-fix-critical-bug

# Fix the issue
git commit -am "fix: resolve critical bug in token refresh"

# Push hotfix branch
git push -u origin hotfix/1.0.1-fix-critical-bug

# Merge to main
git checkout main
git merge --no-ff hotfix/1.0.1-fix-critical-bug
git tag -a v1.0.1 -m "Hotfix version 1.0.1"
git push origin main --tags

# Merge to develop
git checkout develop
git merge --no-ff hotfix/1.0.1-fix-critical-bug
git push origin develop

# Delete hotfix branch
git branch -d hotfix/1.0.1-fix-critical-bug
git push origin --delete hotfix/1.0.1-fix-critical-bug
```

## Commit Message Convention

We follow **Conventional Commits** specification:

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation only changes
- `style`: Code style changes (formatting, missing semi-colons, etc)
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `perf`: Performance improvement
- `test`: Adding or updating tests
- `chore`: Changes to build process or auxiliary tools
- `ci`: Changes to CI configuration files and scripts

### Examples

```bash
feat(collector): add universe data collector service

Implement UniverseTypesCollectorService to fetch all EVE item types
from ESI API and store them in PostgreSQL database.

- Add TypeRepository with Dapper queries
- Add bulk insert optimization
- Add rate limiting with Polly

Closes #001
```

```bash
fix(auth): resolve token refresh infinite loop

The token refresh mechanism was entering an infinite loop when
the refresh token itself was expired.

Fixes #042
```

```bash
docs: update setup instructions in README

Add detailed steps for Docker setup and environment configuration.
```

## Pull Request Process

1. **Create PR from feature branch to develop** (not to main)
2. **Fill PR template** with:
   - Description of changes
   - Related issues
   - Testing performed
   - Screenshots (if UI changes)
3. **Ensure CI passes** (tests, linting, build)
4. **Request review** from at least one team member
5. **Address review comments**
6. **Squash or rebase** if needed to keep history clean
7. **Merge** using `--no-ff` to preserve branch history

## Code Standards

### .NET / C#

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use **nullable reference types**
- Use **async/await** for I/O operations
- Add **XML documentation** for public APIs
- Write **unit tests** for business logic
- Use **dependency injection** consistently

### Database

- Use **migrations** for all schema changes (DbUp)
- Never modify existing migrations
- Write **idempotent** SQL scripts
- Add **indexes** for frequently queried columns
- Document complex queries

### Docker

- Use **multi-stage builds**
- Minimize image size
- Don't include secrets in images
- Use **.dockerignore**

## Development Setup

See [data-collector/README.md](./data-collector/README.md) for detailed setup instructions.

## Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/EveDataCollector.UnitTests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## Questions?

Open an issue or start a discussion on GitHub.

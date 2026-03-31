---
title: CHANGELOG strategy brainstorming
tags:
  - changelog
  - release-flow
  - brainstorming
lifecycle: permanent
createdAt: '2026-03-31T13:40:45.857Z'
updatedAt: '2026-03-31T13:40:45.857Z'
project: https-github-com-servicecomposer-servicecomposer-aspnetcore
projectName: ServiceComposer.AspNetCore
memoryVersion: 1
---
# CHANGELOG strategy brainstorming

## Context

ServiceComposer.AspNetCore maintains multiple major versions in parallel. Features are not backported but patches are (and occasionally features). Release branches are always cut from master, **unless master is already targeting the next major** (in which case the new minor branch is cut from the previous minor's release branch).

## Structure decision: separate files per major (agreed)

A single `CHANGELOG.md` was ruled out because multiple majors are maintained in parallel. Users on v4 shouldn't wade through v5 changes.

## Where do the changelog files live? (undecided)

### Option A: changelogs on master, version-suffixed files (recommended given branching model)

- `CHANGELOG-v1.md`, `CHANGELOG-v2.md` etc. live on master
- Automation always commits to master regardless of which branch the release was tagged on
- When master is on v2 and a v1 patch ships, workflow updates `CHANGELOG-v1.md` on master (harmless commit)
- Single source of truth, always visible on the default branch
- `CHANGELOG.md` on master is an index linking to the version files
- **Pro:** simple automation — extract major from tag, prepend to correct file on master, done
- **Con:** v1 changelog updates committed to master feels slightly odd semantically

### Option B: changelogs on release branches, master is index (original preference, but problematic)

- Each `release-<major.minor>` branch carries its own `CHANGELOG.md`
- `CHANGELOG.md` on master is a short index linking to release branches
- **Problem:** release branches are cut from master (not from previous minor), so changelog inheritance breaks for same-major new-minor releases. `release-1.1` cut from master starts with only the new feature, not the accumulated 1.0.x history.
- Would require either:
  - A workflow that copies the previous minor's changelog when a new release branch is created (complex)
  - Scoping each branch changelog to only that minor line (fragmented history)

### Option C: scoped per-minor changelogs on release branches + master index

- Each release branch CHANGELOG covers only that specific minor line (1.1.0, 1.1.1…)
- No inheritance needed — each branch starts fresh
- Master index links to every release branch per major
- **Con:** "all 1.x history" requires navigating multiple files

## Branching model (key constraint)

- All release branches cut from master, UNLESS master is already targeting the next major
- Changing this rule is not desirable
- This constraint is what makes Option B problematic for same-major new-minor releases

## Automation approach (agreed)

- React to `release: [published]` GitHub event
- Extract major version from release tag
- Prepend release body to the appropriate changelog file
- Backfill script needed to generate initial changelogs from all existing releases

## Next step

Decide between Option A and Option B/C before implementing.

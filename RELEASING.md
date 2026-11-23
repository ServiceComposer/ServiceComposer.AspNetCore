# Releasing

This repository follows the same release process as the other ServiceComposer repository: [ServiceComposer.AspNetCore.Testing](https://github.com/ServiceComposer/ServiceComposer.AspNetCore.Testing/blob/master/RELEASING.md).

## Branches and versions

Versions are calculated by [MinVer](https://github.com/adamralph/minver) from git tags. Nothing else sets a version number.

- **`master`** is where development happens, and it always builds the **next major** that hasn't been branched yet. Its `MinVerMinimumMajorMinor`, in `src/ServiceComposer.AspNetCore/ServiceComposer.AspNetCore.csproj`, is set to that major, so untagged builds are versioned `N.0.0-alpha.0.<height>`.
- **`release-X.Y`** branches stabilize and patch an `X.Y` release. Betas, release candidates, the final release, and patch releases are all tagged on the release branch, never on `master`.

To see which release branches exist, and what each branch builds:

```shell
git branch -r --list 'origin/release-*'
dotnet build src/ServiceComposer.AspNetCore/ServiceComposer.AspNetCore.csproj -t:MinVer -getProperty:MinVerVersion
```

Versions released before this process was adopted were tagged on `master` and may have no release branch. To patch one, create `release-X.Y` from its `X.Y.0` tag.

## Where changes go

1. Every change goes to `master` first, through a pull request.
2. If a change also has to ship in a release that's being prepared, cherry-pick it onto a branch created from `release-X.Y` and open a pull request against `release-X.Y`:

   ```shell
   git switch -c backport-<topic>-to-X.Y origin/release-X.Y
   git cherry-pick -x <commit>...
   ```

   `-x` records the original commit in the message. Include any earlier commits the change depends on.

   Title the pull request `[Backport to <branch>] <what it changes>`, since the release notes are generated from pull request titles. For example, "[Backport to release-5.3] Apply conventions added after MapCompositionHandlers()" rather than "Backport composition over controllers changes to 5.3". Link the original pull requests in the description.
3. Don't backport:
   - Renovate dependency updates, unless the release needs them: release branches keep their own dependency set.
   - Workflows that only run from the default branch, such as the project board one.

Anything merged to `master` after a release branch is created is **not** in that release unless it's backported.

## Starting a new major

To start stabilizing major `X` (for example, the first `X.0.0-beta.1`):

1. On `master`, `MinVerMinimumMajorMinor` should already be `X.0`.
2. Create `release-X.0` from `master` and push it.
3. Open a pull request on `master` that sets `MinVerMinimumMajorMinor` to `X+1`, so `master` moves on to the next major.
4. Tag the first pre-release on `release-X.0`, as described below.

Don't change `MinVerMinimumMajorMinor` on `master` to get a release out of it: releases come from the release branch.

## Starting a new minor

To start a new minor of a major that's already branched:

1. Create `release-X.Y` from the previous minor's release branch and push it.
2. Open a pull request on `release-X.Y` that sets `MinVerMinimumMajorMinor` to `X.Y`. Until the first `X.Y` tag, the latest tag reachable from the branch belongs to the previous minor, so untagged builds would otherwise be versioned as a patch of it.
3. Backport what it should ship, as described above.

## Obsoleting public APIs

Public APIs are removed over two majors, so users get a release in which their code still compiles and tells them what to change:

| Release | The API is |
|---|---|
| A minor of major `N` | `[Obsolete]` with a warning. It keeps working. |
| `N+1.0.0` | `[Obsolete]` with an error. |
| `N+2.0.0` | Removed. |

The obsolete message says what to use instead and when the API changes stage:

```csharp
[Obsolete("Use Bar() instead. Will be treated as an error from version 6.0.0 and removed in version 7.0.0.")]
public void Foo() { }
```

Since `master` builds the next major (`N+1`), obsoleting an API takes two pull requests:

1. On `master`: the API is obsoleted as an error (`[Obsolete("...", error: true)]`). The replacement is added in the same pull request, or before it.
2. On the `release-N.Y` branch of the minor that ships the warning: a backport that adds the replacement, if needed, and obsoletes the API as a warning. It isn't a plain cherry-pick, because the attribute differs.

Then:

- Open an issue in the `N+2.0.0` milestone to remove the API, linking the pull requests.
- Mention the obsoleted API in the release notes of the minor, and in the upgrade guide to `N+1`, in `docs/upgrade-guides`.
- Update the public API approval file, `src/ServiceComposer.AspNetCore.Tests/API/APIApprovals.Approve_API.verified.txt`.
- Keep the tests covering the obsolete API while it's a warning. Suppress the warning only where it's used, with `#pragma warning disable CS0618` and a comment saying why, not with a project-wide `NoWarn`.
- Snippets can't call an API obsoleted as an error: turn them into plain code blocks in the docs, or remove them.

An obsolete warning breaks builds that treat warnings as errors. That's expected in a minor, but call it out in the release notes.

### The error stage

An obsolete error (CS0619) only stops code compiled against `N+1` from calling the API, and it can't be suppressed. Code compiled against `N`, e.g. a library that wasn't rebuilt, can still call it at runtime. In `N+1.0.0`, the API either:

- **Keeps working.** Preferred when that costs nothing, e.g. a renamed method calling its replacement. To keep it covered by tests, move the implementation to an internal method that the obsolete API calls, and make the tests call the internal method.
- **Throws a `NotSupportedException`** saying what to use instead. Preferred when keeping it working means keeping a feature or a code path that `N+1.0.0` otherwise removes. The code behind it is removed in `N+1.0.0`, and only a test checking that it throws is kept, calling it through reflection. The upgrade guide says so.


## Tagging a release

1. Make sure everything that should ship is merged into `release-X.Y`, including backports, and that its CI build is green. A new vulnerability advisory can break a build that used to pass, so if the branch has been quiet, run CI again first:

   ```shell
   gh workflow run ci.yml --ref release-X.Y
   ```

2. A stable release must not depend on a pre-release, and NuGet warns about it when packing (NU5104). If needed, raise the lower bound of the version range to the stable version first.
3. Tag the release branch and push the tag:

   ```shell
   git switch release-X.Y
   git pull
   git tag X.Y.Z            # or X.Y.Z-beta.N, X.Y.Z-rc.N
   git push origin X.Y.Z
   ```

   Tags have no `v` prefix: the CI workflow only runs on tags matching `[0-9]+.[0-9]+.[0-9]+` and `[0-9]+.[0-9]+.[0-9]+-*`.
4. The tag build runs the tests and publishes the packages:
   - pre-release tags (`X.Y.Z-*`) go to the [Feedz.io pre-releases feed](https://f.feedz.io/servicecomposer/pre-releases/nuget/index.json)
   - stable tags (`X.Y.Z`) go to [NuGet.org](https://www.nuget.org/profiles/ServiceComposer)
5. Create the GitHub release from the tag, with notes generated since the previous release:

   ```shell
   gh release create X.Y.Z --verify-tag --generate-notes --notes-start-tag <previous-tag>
   ```

   Add `--prerelease` for pre-release tags, and `--latest=false` when releasing an older major. Then edit the generated notes into the format described in [Release notes](#release-notes).

## Release notes

Readers scan release notes to find out whether a change affects them, and what they have to do. Keep each entry short, and leave the details to the linked issue:

- Group the entries under these headings, in this order, and leave out the empty ones:
  - **Breaking changes**, such as a new minimum .NET version, an obsolete warning that breaks builds treating warnings as errors, or a removed API.
  - **Behavior changes**: anything that changes how an existing app behaves at runtime, even when it fixes a bug.
  - **New features**
  - **Bug fixes**
- Write one sentence per entry, describing what users notice, not how the code changed. Prefix it with the feature, e.g. "Scatter/gather:" or "Composition over controllers:". Group the entries under a sub-heading per feature only when a release has many.
- For breaking and behavior changes, also say what changed compared to before, and end with **What to do:** and the action users need to take, if any. If the upgrade guide covers the change, link to its section instead of repeating it.
- End each entry with the issue and the pull request that shipped the change in this release, in parentheses: `(#1199, #1202)`. For a backport, that's the backport pull request: the issue links the original fix.
- After the entries, add a horizontal rule (`---`) and the **Full Changelog** compare link generated by GitHub.
- End the notes with the pull requests list generated by GitHub, collapsed in a `<details>` section with an `<h3>` summary, so readers can see every pull request by title. Remove the "by @author" parts and the "New Contributors" section, so that editing the notes doesn't notify anyone.

The generated list leaves out pull requests opened by Renovate, and pull requests labeled `skip-release-notes`, as configured in `.github/release.yml`. Label the pull requests that don't matter to users, such as changes to the repository's tooling, with `skip-release-notes`.

For example:

```markdown
## Behavior changes

- Routes with no composition handler are no longer mapped. Before, they ran every matching subscriber and factory on each request, then returned a 404 with a `{}` body. Now requests to them get the routing 404, or reach whatever other endpoint matches, and a warning listing the route and its components is logged at startup. **What to do:** if you see the warning, check the route template for typos. (#1199, #1202)

## Bug fixes

- Scatter/gather: `IgnoreDownstreamRequestErrors` now also ignores timeouts and responses that can't be transformed, not just HTTP error status codes. (#1191, #1194)

---

**Full Changelog**: https://github.com/ServiceComposer/ServiceComposer.AspNetCore/compare/5.3.2...5.3.3

<details>
<summary><h3>All pull requests (2, excluding dependency updates)</h3></summary>

* [Backport to release-5.3] Ignore all downstream errors with IgnoreDownstreamRequestErrors in https://github.com/ServiceComposer/ServiceComposer.AspNetCore/pull/1194
* [Backport to release-5.3] Routes without composition handlers aren't mapped in https://github.com/ServiceComposer/ServiceComposer.AspNetCore/pull/1202

</details>
```

## Releasing across repositories

`ServiceComposer.AspNetCore` and `ServiceComposer.AspNetCore.Testing` are released independently, in any order, and their version numbers aren't aligned. Neither package depends on the other. Only the tests in this repository use `ServiceComposer.AspNetCore.Testing`, from NuGet.org. Renovate only proposes patch updates for it, so update that reference manually to pick up a new minor or major.

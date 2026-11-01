# Releasing

This is the release process for `ServiceComposer.AspNetCore`. [ServiceComposer.AspNetCore.Testing](https://github.com/ServiceComposer/ServiceComposer.AspNetCore.Testing/blob/master/RELEASING.md) uses the same tagging and publishing steps, with its own branches.

## Branches and versions

Versions are calculated by [MinVer](https://github.com/adamralph/minver) from git tags. Nothing else sets a version number.

- **`master`** is where development happens, and it builds the current major. New majors and minors, and their pre-releases, are tagged on `master`. `MinVerMinimumMajorMinor`, in `src/ServiceComposer.AspNetCore/ServiceComposer.AspNetCore.csproj`, is set to that major.
- **`release-X.Y`** branches ship patches for `X.Y`, and further minors of a previous major. The tags for those releases go on the release branch, never on `master`.

MinVer derives untagged versions from the latest tag reachable from the commit. For example, `master` builds `5.2.1-alpha.0.<height>` after `5.2.0`, even if the next release from it is `5.3.0`: the tag decides the version.

To see which release branches exist, and what each branch builds:

```shell
git branch -r --list 'origin/release-*'
dotnet build src/ServiceComposer.AspNetCore/ServiceComposer.AspNetCore.csproj -t:MinVer -getProperty:MinVerVersion
```

## Where changes go

1. Every change goes to `master` first, through a pull request.
2. If a change also has to ship from a release branch, cherry-pick it onto a branch created from `release-X.Y` and open a pull request against `release-X.Y`:

   ```shell
   git switch -c backport-<topic>-to-X.Y origin/release-X.Y
   git cherry-pick -x <commit>...
   ```

   `-x` records the original commit in the message. Include any earlier commits the change depends on.
3. Don't backport:
   - Renovate dependency updates, unless the release needs them.
   - Workflows that only run from the default branch, such as the project board one.

Anything merged to `master` after a release branch is created is **not** in that release unless it's backported.

Renovate only updates `master`, so release branches keep their own dependency set. A new vulnerability advisory for one of those dependencies can fail the build, because NuGet audit warnings are treated as errors. Update the dependency in a pull request against the release branch: there's nothing to cherry-pick if `master` has already moved past the affected version.

## Creating a release branch

- **To patch `X.Y`:** create `release-X.Y` from the `X.Y.0` tag, if it doesn't exist yet.
- **To add a minor to a previous major:** create `release-X.Y` from the previous minor's release branch.

```shell
git switch -c release-X.Y X.Y.0
git push -u origin release-X.Y
```

## Starting a new major

To start major `X` on `master`:

1. Create a release branch for the previous major from `master`, so that major can still ship.
2. Open a pull request on `master` that sets `MinVerMinimumMajorMinor` to `X.0`.
3. Tag pre-releases, then `X.0.0`, on `master`, as described below.

## Tagging a release

1. Pick the branch: `master` for a new major or minor of the current major, `release-X.Y` for everything else.
2. Make sure everything that should ship is merged into it, including backports, and that its CI build is green. Release branches can go quiet for months, and a new advisory can break a build that used to pass, so run CI again first:

   ```shell
   gh workflow run ci.yml --ref release-X.Y
   ```

3. A stable release must not depend on a pre-release. NuGet warns about it when packing (NU5104).
4. Tag the branch and push the tag:

   ```shell
   git switch release-X.Y   # or master
   git pull
   git tag X.Y.Z            # or X.Y.Z-beta.N, X.Y.Z-rc.N
   git push origin X.Y.Z
   ```

   Tags have no `v` prefix. The CI workflow only runs on tags matching `[0-9].[0-9]+.[0-9]` and `[0-9].[0-9]+.[0-9]+-*`, so a stable tag with a two-digit major or patch, such as `5.2.10`, doesn't run it. Update the filters in `.github/workflows/ci.yml` first, on the branch being tagged.
5. The tag build runs the tests and publishes the package:
   - pre-release tags (`X.Y.Z-*`) go to the [Feedz.io pre-releases feed](https://f.feedz.io/servicecomposer/pre-releases/nuget/index.json)
   - stable tags (`X.Y.Z`) go to [NuGet.org](https://www.nuget.org/profiles/ServiceComposer), through [trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing): the workflow file name, `ci.yml`, is part of the nuget.org policy, so renaming the file requires updating the policy.
6. Create the GitHub release from the tag, with notes generated since the previous release on the same line:

   ```shell
   gh release create X.Y.Z --verify-tag --generate-notes --notes-start-tag <previous-tag>
   ```

   Add `--prerelease` for pre-release tags, and `--latest=false` when releasing an older line, such as a `4.3` patch while `5.x` is current. Call out breaking changes, such as a new minimum .NET version, at the top of the notes.

## Releasing across repositories

`ServiceComposer.AspNetCore` and `ServiceComposer.AspNetCore.Testing` are released independently, and their version numbers aren't aligned. Neither package depends on the other: only the tests in this repository use `ServiceComposer.AspNetCore.Testing`, from NuGet.org. Renovate only proposes patch updates for it, so update that reference manually to pick up a new minor or major.

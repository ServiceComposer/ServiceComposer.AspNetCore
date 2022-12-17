---
name: Package release
about: Package release template
title: Release [version]
labels: ''
assignees: ''

---

- Prepare the release
  - [ ] tag the repo (packages will be pushed to [MyGet](https://www.myget.org/feed/Packages/service-composer))
  - Update dependabot configuration to consider any newly created `release-X.Y` branch 
- Release notes:
  - [ ] edit release notes as necessary, e.g. to mention contributors
  - [ ] associate draft with the created tag
  - [ ] publish release notes
- Release
  - [ ] from [MyGet](https://www.myget.org/feed/Packages/service-composer) push to Nuget
- Clean-up
- [ ] close this issue
- [ ] close the milestone

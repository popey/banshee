# Source publication plan

The fork now exists. [Baseline audit](source-baseline-audit.md) verified the
release source and all 24 patches. Hyena is now vendored in the local revival branch with its original license
and pinned revision recorded; the JSON fix is a separate commit.

Agreed 2026-09-25. First step: Alan forks
https://github.com/BansheeMediaPlayer/banshee to `popey/banshee`, retaining
all branches if the fork UI offers that choice. Leave the default branch
unchanged until the revival branch is prepared and validated.

The corresponding GNOME archive is https://gitlab.gnome.org/Archive/banshee.
Read-only `git ls-remote` checks on both repositories returned identical refs:

| Ref | Commit (annotated tags peeled) |
| --- | --- |
| master | b10d3742b6afa02bf3166079658950bea51fd257 |
| stable-2.6 | 585e05477f3b59d33682828526c61bc833b95703 |
| 2.6.2 | 4721061828e7d0de89c649fabfaae4135bb49e50 |
| 2.9.1 | b33ab915185153b79ea2ac518d2d0ec7558cb7db |

This verifies the relevant refs, not every branch in both repositories.
The stable-2.6 tip differs from the release tag. Our 24 patches were prepared
against the 2.6.2 release tarball, so use the exact release tag as the baseline;
do not assume they apply unchanged to the stable branch tip.

## Branches and builds

- Preserve upstream master and release tags unchanged.
- Create `revival/2.6.2` from tag `2.6.2`; make it the default after validation.
  Apply source fixes as reviewable commits, preserving Ubuntu/upstream
  attribution. Add snap packaging, tests and documentation.
- Build directly from the checked-out source rather than unpacking the
  vendored tarball and applying the same patches a second time. Account for
  Git submodules and generated release-tarball files before claiming build
  equivalence. Validate using `snapcraft --use-lxd` and runtime checks.
- Later create `revival/2.9.1` from tag `2.9.1`. Review each fix for applicability
  and port it individually; some fixes may already exist upstream.
- Publish 2.6.2 to stable; test 2.9.1 in beta, then candidate when ready.
  Channel promotion is an explicit release action, not an automatic result
  of pushing a branch.
- Tag exact release commits for Store revision traceability. Keep displayed
  snap versions `2.6.2` and `2.9.1` without local build-counter suffixes.
- Keep one roadmap on the default branch identifying fixes needed on either
  or both maintenance lines.

Exclude `.env`, credentials, profiles, music, downloaded media, generated
binaries, snaps and diagnostic logs. Preserve licenses and patch authorship.
Keep the GNOME archive as a reference remote for provenance and comparison.

This records the plan only: no fork, branch push, default-branch change or
new Store release was performed as part of this planning task.

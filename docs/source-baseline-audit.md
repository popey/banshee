# Fork and release baseline audit

Completed 2026-09-25. This is a source comparison and patch-application check,
not a build or runtime validation of the Git checkout.

## Isolated checkouts

- Cloned `https://github.com/popey/banshee.git` into
  `/tmp/banshee-source-audit`, detached at upstream tag `2.6.2`.
- Commit: `4721061828e7d0de89c649fabfaae4135bb49e50`.
- Fork master and 2.9.1 tag also match the upstream refs recorded in
  [the publication plan](source-publication.md).
- `origin/stable-2.6` has 60 commits after the 2.6.2 tag, including code fixes,
  reversions and translations. It is not the chosen release baseline.
- Extracted a fresh copy of the vendored tarball into
  `/tmp/banshee-tarball-audit/banshee-2.6.2`.
- Tarball SHA256:
  `f77c089b05e3dc956236d13ff02945fe560f56e402df57621a9196de39ba60f8`.

## Hyena dependency

The release pins `src/Hyena` to
`f265cff2967266f7c97f4b3a373083be8a0d811e`. Its `.gitmodules` URL is the obsolete
`git://git.gnome.org/hyena`. Fetched `https://gitlab.gnome.org/Archive/hyena.git`
into the disposable checkout and checked out that exact commit. No upstream
ref or project configuration was changed.

Our `hyena-json-numbers.patch` modifies `Hyena/Hyena.Json/Tokenizer.cs` inside
this dependency. A parent-repository commit alone cannot preserve this edit.
Before preparing publication commits, choose either:

1. Vendor the pinned Hyena source in the revival branch, with provenance and
   license retained, then commit its fix in this repository; or
2. Publish a separate Hyena fork with the fix and update the submodule URL
   and pinned commit.

Vendoring keeps the revival self-contained in one public repository. Whichever
approach is chosen, account for `autogen.sh` invoking `git submodule update
--init` and verify a completely fresh clone builds successfully.

## Comparison and patch trial

Before patches, every file present in both source trees was byte-identical.
Git had 205 additional files, including repository metadata, IDE projects,
platform packaging and development utilities. The release tarball had 461
additional files: 345 translated help files, 101 `Makefile.in` files, and 15
release/build outputs (including configure, ChangeLog and AssemblyInfo.cs).

Applied the 24 patches in `patches/series` independently to both trees using
`patch --batch --forward --fuzz=0 -p1`.

- All 24 applied successfully on each tree.
- No rejected hunks or fuzzy matching.
- The Ayatana patch used line offsets in both trees; this was not specific
  to the Git baseline.
- After patching, all 3,010 shared files were byte-identical, including the
  newly added first-run and indicator source files.

Detailed temporary evidence:

- `/tmp/banshee-baseline-comparison.json`
- `/tmp/banshee-patch-audit.json`
- `/tmp/banshee-patch-audit.log`

## Next gate

Resolve how to publish Hyena, then prepare attributed source commits and
packaging in a local revival branch. Change the build to consume checked-out
source without reapplying the patches. Validate generation of release-only
build files using LXD before claiming equivalence with the published snap.

No publication commits, remote pushes, default-branch changes, builds or
Store releases were made during this audit. Existing working packaging,
vendored archive and installed snap were left unchanged.

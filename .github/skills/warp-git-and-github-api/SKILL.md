---
name: warp-git-and-github-api
description: >-
  Uses WARP GitHub authentication/Octokit clients and local repository metadata,
  composes integration branches in isolated worktrees and diagnoses stale bases,
  selected-source ancestry, tracking-file unions or visible code conflicts.
metadata:
  source: "WARP src\\WarpToolkit.WinForms.Github\\Git\\GitBranchCompositionService.cs; Services"
  revision: "0b5b59bca1cd1ff93c2370ba1c2c9c7f051a63a5"
---

# GitHub clients and data-preserving Git composition

## Critical constraints

- Fetch the remote and validate **every selected source against the current fetched
  base SHA before replay**. Do not trust stale local refs or silently use a local fallback.
- Compose in an isolated worktree, not the user's active checkout. Preserve normal
  Git authentication and explicit force-with-lease when overwriting.
- Never blanket cherry-pick `-X theirs`. Union-and-cleanup cumulative tracking
  files and jointly modified resources; keep code conflicts visible unless an
  explicit logged domain-approved policy applies.
- Structural auto-merge's Roslyn gate validates **syntax only**, not semantic
  preservation. Review warned files and run repository-specific verification.
- Log semantic base/source/replay/union/cleanup/final HEAD/warning/verification
  events, not only raw commands. A push is not a verification result.

## Services and authentication

Package WarpToolkit.WinForms.Github; use a matching published version or explicit
ProjectReference to the checkout. Do not retain historical preview numbers as latest.
Import WarpToolkit.WinForms.Github.Services:

```csharp
builder.Services.AddGitServices();
builder.Services.AddGitHubGcmClientServiceFactory();
```

Local registration provides ILocalGitRepositoryService and
IGitBranchCompositionService; GitHub client registration is separate.
IGitHubClientFactoryService.GetClient returns authenticated Octokit GitHubClient.
GitHubLoginManager.GetClientAsync supports the existing device-code flow;
GitHubDeviceLogin and RepoListView are related auth/UI helpers.
Git services shell out to git and reuse SSH/GCM/gh auth setup-git rather than
Octokit credentials for fetch/push/cherry-pick.
GitRemoteInfo can represent Enterprise-style/non-GitHub remotes; do not assume
every local repository is GitHub.com.
Read `warp-winforms-application-builder` only for host/DI setup.

## Inspection and target naming

Namespace WarpToolkit.WinForms.Github.Git:

```csharp
LocalGitRepositoryInfo repo = await git.GetRepositoryInfoAsync(path);
IReadOnlyList<GitBranchInfo> branches = await git.GetBranchesAsync(repo.RootPath);
GitCommitInfo tip = await git.GetBranchTipAsync(repo.RootPath,
    repo.DefaultBranch ?? "main");
```

Models retain root/origin/current/default branch; GitRemoteInfo host/owner/repository/
URL/RepositoryKey; branch local/remote and latest commit; commit full/abbreviated
SHA, author/committer dates, subject and parent count.

BranchTargetOptions includes BranchSetName, TargetBranchName, NamingMode,
NumberWidth and OverwriteExisting:
Fixed=`<set>/<target>`; DateFolder=`<set>/<yyyy-MM-dd>/<target>`;
NumberedSuffix=`<set>/<target>-01` (or configured digit width).
ResolveTargetBranchNameAsync computes a name without composing.

## Composition workflow

```csharp
BranchCompositionResult result = await composer.ComposeAsync(
    new BranchCompositionRequest
    {
        RepositoryPath = repo.RootPath,
        BaseBranch = "main",
        SourceBranches = ["feature-a", "feature-b"],
        TargetOptions = new BranchTargetOptions
        {
            BranchSetName = "release-test",
            TargetBranchName = "combined",
            NamingMode = TargetBranchNamingMode.NumberedSuffix,
            NumberWidth = 2,
            OverwriteExisting = false
        }
    });
```

Current source fetches/prunes, snapshots refs and performs base ancestry validation.
Its fetched-base resolver can fall back to a local name if the remote ref is absent:
**that behavior does not satisfy a fresh-remote-base guarantee**. Require a verified
remote base for the intended workflow or report the missing prerequisite.
Selected stacked branches have ancestry/order checks; do not reorder them blindly.

Ordinary replay de-duplicates commits and orders **committer date → author date → SHA**.
Merge commits are rejected in ordinary replay; SquashSourceBranches selects a
separate squash plan. Inspect its semantics before claiming merge/mainline support.
Source progress reports plan/replay/publish/completion. Windows long-path support
is checked; report the prerequisite rather than silently change machine settings.

New remote targets use normal push; overwrite uses
`--force-with-lease=refs/heads/<target>:<observedOldSha>`.
Cancellation/lifetime during publishing deserves explicit status handling; do not
report an aborted UI operation as proof no remote branch was written.

## Union, conflicts and verification

Union/cleanup families:
PublicAPI.Shipped.txt, PublicAPI.Unshipped.txt, NativeMethods.txt,
AnalyzerReleases.Shipped.md, AnalyzerReleases.Unshipped.md and .resx files changed
by multiple replays. Keep accumulated declarations/resources while cleaning duplicates
and ordering; same-key resources need semantic comparison, not blind concatenation.

Current source has a structure-aware declaration/block-atomic merge and syntax gate
for source files, with RiskAutoMergedCodeFiles and warnings. This is an explicit
implemented policy, not permission to hide arbitrary conflicts or assume semantic
safety. Require domain approval/review where applicable; unresolved conflicts abort.

Catch BranchCompositionException first (Commit/ConflictedFiles), then GitCommandException
(ExitCode/StandardOutput/StandardError). Surface useful diagnostics; do not swallow.
Composition Log includes BaseSha, ReplayedCommits/source, UnionMergedFiles,
CodeConflictFiles, RiskAutoMergedCodeFiles, CleanupCommits, Warnings and FinalHeadSha.
Current VerificationResult explicitly says repository-specific verification was
**not run** because no command was provided. Run appropriate tests/build/integration
checks separately and record the actual result.

## Final checks

1. Fresh fetched base and every source ancestry are recorded before mutation/replay.
2. No active-checkout mutation, lossy blanket conflict policy or unleased overwrite.
3. Tracking/resources survive; code warnings and real verification remain visible.

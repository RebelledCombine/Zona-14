#!/usr/bin/env bash
# Pulls the latest `devin-tools` branch from origin and copies AGENTS.md +
# .agents/skills/ into the working tree. Files are added to `.git/info/exclude`
# so they are ignored in `master` and survive `git clean -fd` without `-x`.
set -e

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$REPO_ROOT" ]]; then
    echo "update-devin-tools.sh: not inside a git repo — aborting." >&2
    exit 1
fi

cd "$REPO_ROOT"

# Fetch the devin-tools branch from origin. If it does not exist, abort.
if ! git fetch origin devin-tools 2>/dev/null; then
    echo "update-devin-tools.sh: could not fetch origin/devin-tools — aborting." >&2
    exit 1
fi

WORK_DIR="$(mktemp -d)"
# Cleanup on exit.
trap 'git worktree remove "$WORK_DIR" --force 2>/dev/null || true; rm -rf "$WORK_DIR"' EXIT

git worktree add "$WORK_DIR" FETCH_HEAD >/dev/null 2>&1

# Copy AI tooling into the working tree.
mkdir -p "$REPO_ROOT/.agents/skills"
rm -rf "$REPO_ROOT/.agents/skills"
cp -r "$WORK_DIR/.agents/skills" "$REPO_ROOT/.agents/skills"
cp "$WORK_DIR/AGENTS.md" "$REPO_ROOT/AGENTS.md"

# Ensure the files are ignored locally so they are never committed to master.
mkdir -p "$REPO_ROOT/.git/info"
grep -qxF "AGENTS.md" "$REPO_ROOT/.git/info/exclude" 2>/dev/null || echo "AGENTS.md" >> "$REPO_ROOT/.git/info/exclude"
grep -qxF ".agents/skills/" "$REPO_ROOT/.git/info/exclude" 2>/dev/null || echo ".agents/skills/" >> "$REPO_ROOT/.git/info/exclude"

echo "Updated Devin tooling from origin/devin-tools."

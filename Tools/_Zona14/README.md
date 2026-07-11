# `Tools/_Zona14`

Scripts that enforce the Zona-14 coding conventions documented in the root-level [`CONTRIBUTING.md`](../../CONTRIBUTING.md).

## `check-conventions.sh`

PR-diff validator. Runs the checks described in `CONTRIBUTING.md §10`:

1. Namespace–folder alignment (fatal)
2. Upstream-edit `// Zona14:` / `# Zona14:` marker enforcement (fatal; skipped on `[upstream-port]` PRs)
3. Misfiled `_Zona14` namespace guard (fatal)
4. Greenfield-outside-`_Zona14/` warning (non-fatal; skipped on `[upstream-port]` PRs)
5. Key-file delete guard (fatal)
6. Asset `meta.json` `license` / `copyright` enforcement (fatal; allowlist override via `[custom-license]`)
7. No global subscribers to action-attempt events (fatal)
8. YAML data-prototype `categories`/`suffix` guard (fatal)

### Usage

```bash
bash Tools/_Zona14/check-conventions.sh <base-ref> <head-ref>
```

Typical local invocation (before pushing):

```bash
bash Tools/_Zona14/check-conventions.sh origin/master HEAD
```

The pre-commit hook uses the `--cached` form:

```bash
bash Tools/_Zona14/check-conventions.sh --cached
```

The workflow [`.github/workflows/zona14-convention.yml`](../../.github/workflows/zona14-convention.yml) runs the same script on every PR against `master`.

### Environment variables

- `PR_TITLE` — optional. Set by the CI workflow from `github.event.pull_request.title`. Used to detect `[upstream-port]` and `[custom-license]` tags. When unset, only commit messages in `base..head` are inspected for tags.

### Dependencies

- `git`, `grep`, `sed`, `awk` — standard on any Linux/macOS shell.
- `jq` — for parsing `meta.json`. Install with `sudo apt install jq` or `brew install jq`.
- `python3` and `pyyaml` — for the YAML data-prototype guard. Install with `python3 -m pip install -r Tools/_Zona14/requirements.txt`.

### Exit codes

- `0` — pass (possibly with warnings on stderr).
- `1` — at least one fatal check failed.
- `2` — usage error or missing dependency (`jq`).

## `hooks/pre-commit`

Git pre-commit hook that runs `check-conventions.sh` automatically before each commit.

### Install

```bash
git config core.hooksPath Tools/_Zona14/hooks
```

The hook skips gracefully if `jq` is missing (prints a warning instead of blocking the commit).

## `check-yaml-data-prototypes.py`

Python script invoked by `check-conventions.sh` to ensure `categories` and `suffix` are only used on `- type: entity` prototypes. Data prototypes such as `stWarZone`, `stBandShopListings`, `persistentCraftRecipe`, `vendingMachineInventory`, and `shopPreset` do not accept those entity fields and the YAML Linter will reject them.

## `generate-vendor-preset.py`

Generates a `shopPreset` YAML from a CSV or Excel catalog. Expected columns: `id`, `price`, `category`. It validates item IDs against `Resources/Prototypes/_Zona14/` and reports missing IDs.

### Example

```bash
python3 Tools/_Zona14/generate-vendor-preset.py \
  --input vendor.csv \
  --output Resources/Prototypes/_Zona14/ShopPresets/NPCShops/MyVendor.yml \
  --preset-id Z14MyVendorPreset \
  --min-sell-price 1
```

## `update-devin-tools.sh`

Fetches the `devin-tools` branch from origin and copies `AGENTS.md` and `.agents/skills/` into the working tree. Adds those files to `.git/info/exclude` so they are ignored in `master` and not removed by `git clean -fd`.

## `requirements.txt`

Python packages used by the helper scripts (`pyyaml`, `openpyxl`). Install with:

```bash
python3 -m pip install -r Tools/_Zona14/requirements.txt
```

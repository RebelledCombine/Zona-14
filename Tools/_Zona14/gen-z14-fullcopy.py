#!/usr/bin/env python3
"""
Generate full-copy Z14 clones from upstream _Stalker/ files.

Instead of thin parent-wrapper clones, this copies the ENTIRE entity
definition so values can be edited directly without referencing upstream.

Transformations applied to each entity:
  - id: X  →  id: Z14X
  - parent: X  →  parent: Z14X  (only if X is also being cloned)
  - suffix: ...  →  suffix: Z14  (concrete entities only; abstract keep no suffix)
  - categories: [ ... ]  →  categories: [ ..., Zona14 ]  (or added fresh)
  - abstract entities: suffix line removed if present
"""

import re
import os
import sys
import shutil

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ZONA14_BASE = os.path.join(REPO, "Resources/Prototypes/_Zona14")


def find_all_ids_in_file(content):
    """Extract all entity IDs from file content."""
    ids = set()
    for m in re.finditer(r'^\s+id:\s+(\S+)', content, re.MULTILINE):
        ids.add(m.group(1))
    return ids


def process_file(src_path, dst_path, all_cloned_ids):
    """
    Read upstream file, transform to Z14 full copy, write to _Zona14/.
    
    all_cloned_ids: set of ALL upstream IDs being cloned in this batch,
                    used to decide which parent references to update.
    """
    with open(src_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Split into entity blocks and inter-block content
    # An entity block starts with "- type:" at column 0
    lines = content.split('\n')
    output_lines = []
    i = 0

    while i < len(lines):
        line = lines[i]

        # Check if this line starts a new entity block
        if line.startswith('- type:'):
            # Collect the entire entity block
            block_lines = [line]
            i += 1
            while i < len(lines):
                # Next entity or end of file
                if lines[i].startswith('- type:'):
                    break
                block_lines.append(lines[i])
                i += 1

            # Process this entity block
            transformed = transform_entity_block(block_lines, all_cloned_ids)
            output_lines.extend(transformed)
        else:
            # Non-entity line (comments, blank lines between blocks)
            output_lines.append(line)
            i += 1

    result = '\n'.join(output_lines)
    # Ensure file ends with a newline
    if not result.endswith('\n'):
        result += '\n'

    os.makedirs(os.path.dirname(dst_path), exist_ok=True)
    with open(dst_path, 'w', encoding='utf-8') as f:
        f.write(result)


def transform_entity_block(block_lines, all_cloned_ids):
    """Transform a single entity block to Z14 version."""
    # Detect if this is a `- type: entity` block
    first_line = block_lines[0]
    type_match = re.match(r'^- type:\s+(\S+)', first_line)
    if not type_match:
        return block_lines  # not a recognized block, return as-is

    proto_type = type_match.group(1)

    # Only transform entity prototypes
    if proto_type != 'entity':
        # For non-entity types, still prefix ID with Z14 but NO categories/suffix
        return transform_data_block(block_lines, all_cloned_ids)

    # Check if abstract
    is_abstract = any(re.match(r'^\s+abstract:\s+true', l) for l in block_lines)

    result = []
    id_line_idx = None
    had_categories = False
    had_suffix = False

    for idx, line in enumerate(block_lines):
        # Transform id — only top-level entity id (2-space indent)
        id_match = re.match(r'^(  id:\s+)(\S+)(.*)', line)
        if id_match:
            prefix, old_id, rest = id_match.group(1), id_match.group(2), id_match.group(3)
            result.append(f'{prefix}Z14{old_id}{rest}')
            id_line_idx = len(result) - 1
            continue

        # Transform parent — only top-level (2-space indent)
        parent_match = re.match(r'^(  parent:\s+)(\S+)(.*)', line)
        if parent_match:
            prefix, old_parent, rest = parent_match.group(1), parent_match.group(2), parent_match.group(3)
            if old_parent in all_cloned_ids:
                result.append(f'{prefix}Z14{old_parent}{rest}')
            else:
                result.append(line)
            continue

        # Transform categories — only top-level (2-space indent)
        cat_match = re.match(r'^(  categories:\s*\[\s*)(.+?)(\s*\])(.*)', line)
        if cat_match:
            prefix, cats_str, bracket, rest = cat_match.groups()
            cats = [c.strip() for c in cats_str.split(',')]
            if 'Zona14' not in cats:
                cats.append('Zona14')
            result.append(f'  categories: [ {", ".join(cats)} ]')
            had_categories = True
            continue

        # Transform suffix — only top-level (2-space indent)
        suffix_match = re.match(r'^(  suffix:\s+)(.*)', line)
        if suffix_match:
            if is_abstract:
                # Abstract entities: remove suffix entirely
                pass  # skip this line
            else:
                result.append('  suffix: Z14')
            had_suffix = True
            continue

        result.append(line)

    # Inject missing categories and suffix after the id line
    if id_line_idx is not None:
        insertions = []
        if not had_categories:
            insertions.append('  categories: [ Zona14 ]')
        if not had_suffix and not is_abstract:
            insertions.append('  suffix: Z14')

        if insertions:
            for j, ins in enumerate(insertions):
                result.insert(id_line_idx + 1 + j, ins)

    return result


def transform_data_block(block_lines, all_cloned_ids):
    """Transform a non-entity (data) prototype block — only prefix ID, no categories/suffix."""
    result = []
    for line in block_lines:
        # Transform id — only top-level (2-space indent)
        id_match = re.match(r'^(  id:\s+)(\S+)(.*)', line)
        if id_match:
            prefix, old_id, rest = id_match.group(1), id_match.group(2), id_match.group(3)
            result.append(f'{prefix}Z14{old_id}{rest}')
            continue

        # Transform parent if in cloned set — only top-level (2-space indent)
        parent_match = re.match(r'^(  parent:\s+)(\S+)(.*)', line)
        if parent_match:
            prefix, old_parent, rest = parent_match.group(1), parent_match.group(2), parent_match.group(3)
            if old_parent in all_cloned_ids:
                result.append(f'{prefix}Z14{old_parent}{rest}')
            else:
                result.append(line)
            continue

        result.append(line)
    return result


def main():
    import argparse
    parser = argparse.ArgumentParser(description='Generate full-copy Z14 clones')
    parser.add_argument('--base', default='_Stalker',
                        help='Source base directory name (default: _Stalker). '
                             'E.g., _Stalker_EN for EN files.')
    parser.add_argument('--extra-ids-from', nargs='*', default=[],
                        help='Additional directories to scan for IDs (for cross-source parent refs)')
    parser.add_argument('dirs', nargs='+',
                        help='Relative paths under the base directory to process')
    args = parser.parse_args()

    stalker_base = os.path.join(REPO, f"Resources/Prototypes/{args.base}")

    # Collect all source files
    src_files = []
    for rel_dir in args.dirs:
        stalker_dir = os.path.join(stalker_base, rel_dir)
        if not os.path.isdir(stalker_dir):
            print(f"ERROR: {stalker_dir} is not a directory")
            sys.exit(1)
        for root, dirs, files in os.walk(stalker_dir):
            for f in sorted(files):
                if f.endswith('.yml'):
                    src_files.append(os.path.join(root, f))

    print(f"Found {len(src_files)} source files from {args.base}/")

    # Phase 1: Collect ALL IDs across all files
    all_ids = set()
    for src in src_files:
        with open(src, 'r', encoding='utf-8') as f:
            all_ids.update(find_all_ids_in_file(f.read()))

    # Also collect IDs from extra directories (for cross-source parent refs)
    for extra_dir in args.extra_ids_from:
        extra_path = os.path.join(REPO, "Resources/Prototypes", extra_dir)
        if os.path.isdir(extra_path):
            for root, dirs, files in os.walk(extra_path):
                for f in files:
                    if f.endswith('.yml'):
                        with open(os.path.join(root, f), 'r', encoding='utf-8') as fh:
                            all_ids.update(find_all_ids_in_file(fh.read()))

    print(f"Collected {len(all_ids)} entity IDs for parent-reference updates")

    # Phase 2: Process each file
    for src in src_files:
        rel = os.path.relpath(src, stalker_base)
        dst = os.path.join(ZONA14_BASE, rel)
        process_file(src, dst, all_ids)
        print(f"  {rel}")

    print(f"\nDone — {len(src_files)} files written to _Zona14/")


if __name__ == '__main__':
    main()

#!/usr/bin/env python3
"""
Extract specific entities from an upstream file and transform to Z14 full copies.

Usage: extract-z14-entities.py <src-file> <dst-file> <id1> [id2 ...]
"""
import re
import sys
import os


def parse_entities(filepath):
    """Parse file into list of (preceding_text, entity_lines, entity_id) tuples."""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    lines = content.split('\n')
    entities = []
    current_block = []
    preceding = []
    i = 0

    while i < len(lines):
        line = lines[i]
        if line.startswith('- type:'):
            if current_block:
                # Find ID in current block
                eid = None
                for bl in current_block:
                    m = re.match(r'^  id:\s+(\S+)', bl)
                    if m:
                        eid = m.group(1)
                        break
                entities.append((preceding, current_block, eid))
                preceding = []
            current_block = [line]
        elif current_block:
            current_block.append(line)
        else:
            preceding.append(line)
        i += 1

    if current_block:
        eid = None
        for bl in current_block:
            m = re.match(r'^  id:\s+(\S+)', bl)
            if m:
                eid = m.group(1)
                break
        entities.append((preceding, current_block, eid))

    return entities


def transform_entity(block_lines, target_ids, is_entity_type=True):
    """Transform entity block to Z14 version."""
    is_abstract = any(re.match(r'^\s+abstract:\s+true', l) for l in block_lines)

    result = []
    id_line_idx = None
    had_categories = False
    had_suffix = False

    for line in block_lines:
        # Transform id — only top-level entity id (2-space indent)
        id_match = re.match(r'^(  id:\s+)(\S+)(.*)', line)
        if id_match:
            prefix, old_id, rest = id_match.group(1), id_match.group(2), id_match.group(3)
            result.append(f'{prefix}Z14{old_id}{rest}')
            id_line_idx = len(result) - 1
            continue

        # Transform parent — only top-level (2-space indent)
        parent_match = re.match(r'^(  parent:\s+)(.+)', line)
        if parent_match:
            prefix = parent_match.group(1)
            parent_val = parent_match.group(2).strip()
            # Handle list parents like [ BaseStalkerPDA, GeigerCounter ]
            if parent_val.startswith('['):
                # Parse list items
                items = re.findall(r'[\w]+', parent_val)
                new_items = []
                for item in items:
                    if item in target_ids:
                        new_items.append(f'Z14{item}')
                    else:
                        new_items.append(item)
                result.append(f'{prefix}[ {", ".join(new_items)} ]')
            else:
                if parent_val in target_ids:
                    result.append(f'{prefix}Z14{parent_val}')
                else:
                    result.append(line)
            continue

        # Transform categories — only top-level (2-space indent)
        cat_match = re.match(r'^(  categories:\s*\[\s*)(.+?)(\s*\])(.*)', line)
        if cat_match and is_entity_type:
            prefix, cats_str, bracket, rest = cat_match.groups()
            cats = [c.strip() for c in cats_str.split(',')]
            if 'Zona14' not in cats:
                cats.append('Zona14')
            result.append(f'  categories: [ {", ".join(cats)} ]')
            had_categories = True
            continue

        # Transform suffix — only top-level (2-space indent)
        suffix_match = re.match(r'^(  suffix:\s+)(.*)', line)
        if suffix_match and is_entity_type:
            if is_abstract:
                pass  # remove suffix for abstract
            else:
                result.append('  suffix: Z14')
            had_suffix = True
            continue

        result.append(line)

    # Inject missing categories/suffix after id line
    if id_line_idx is not None and is_entity_type:
        insertions = []
        if not had_categories:
            insertions.append('  categories: [ Zona14 ]')
        if not had_suffix and not is_abstract:
            insertions.append('  suffix: Z14')
        for j, ins in enumerate(insertions):
            result.insert(id_line_idx + 1 + j, ins)

    return result


def main():
    if len(sys.argv) < 4:
        print("Usage: extract-z14-entities.py <src-file> <dst-file> <id1> [id2 ...]", file=sys.stderr)
        sys.exit(1)

    src_file = sys.argv[1]
    dst_file = sys.argv[2]
    target_ids = set(sys.argv[3:])

    entities = parse_entities(src_file)

    output_lines = []
    for preceding, block, eid in entities:
        if eid in target_ids:
            # Check if this is a `- type: entity` block
            first_line = block[0]
            is_entity = bool(re.match(r'^- type:\s+entity\s*$', first_line))

            # Add preceding comments (filtered to relevant ones)
            for p in preceding:
                if p.strip():
                    output_lines.append(p)

            transformed = transform_entity(block, target_ids, is_entity)
            output_lines.extend(transformed)
            output_lines.append('')

    result = '\n'.join(output_lines)
    if not result.endswith('\n'):
        result += '\n'

    os.makedirs(os.path.dirname(dst_file), exist_ok=True)
    with open(dst_file, 'w', encoding='utf-8') as f:
        f.write(result)

    print(f"Extracted {len(target_ids)} entities to {dst_file}")


if __name__ == '__main__':
    main()

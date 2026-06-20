#!/usr/bin/env python3
"""
Extract specific entity blocks from a YAML file by ID.
Outputs the full entity definition including all components.

Usage: extract-entities.py <file> <id1> [id2 ...]
  If no IDs given, extracts all entities.
"""
import re
import sys

def extract_entities(filepath, target_ids=None):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    blocks = []
    current_block = []
    preceding_comments = []

    for line in lines:
        stripped = line.rstrip('\n')
        if stripped.startswith('- type:'):
            if current_block:
                blocks.append((preceding_comments, current_block))
            preceding_comments = []
            current_block = [stripped]
        elif current_block:
            current_block.append(stripped)
        else:
            # Lines before any entity block or between blocks
            if stripped.strip() == '' or stripped.strip().startswith('#'):
                preceding_comments.append(stripped)
            else:
                preceding_comments.append(stripped)

    if current_block:
        blocks.append((preceding_comments, current_block))

    for comments, block in blocks:
        # Extract ID from block
        entity_id = None
        for line in block:
            m = re.match(r'^\s+id:\s+(\S+)', line)
            if m:
                entity_id = m.group(1)
                break

        if target_ids is None or entity_id in target_ids:
            # Print preceding comments
            for c in comments:
                print(c)
            # Print block
            for line in block:
                print(line)
            print()

if __name__ == '__main__':
    if len(sys.argv) < 2:
        print("Usage: extract-entities.py <file> [id1 id2 ...]", file=sys.stderr)
        sys.exit(1)

    filepath = sys.argv[1]
    ids = set(sys.argv[2:]) if len(sys.argv) > 2 else None
    extract_entities(filepath, ids)

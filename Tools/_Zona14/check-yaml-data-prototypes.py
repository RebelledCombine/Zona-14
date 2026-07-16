#!/usr/bin/env python3
"""Check that `categories` and `suffix` are only used on entity prototypes.

Data prototypes (e.g. stWarZone, stBandShopListings, persistentCraftRecipe,
vendingMachineInventory, shopPreset) do not define `categories`/`suffix` as
the entity-spawn-menu fields, so putting them there is a YAML-linter error.

Usage:
    python3 check-yaml-data-prototypes.py file1.yml file2.yml ...
"""

import sys
from pathlib import Path
from typing import Any

import yaml
from yaml import MappingNode, SequenceNode, ScalarNode

# Zona14: RobustToolbox uses !type:<TypeName> YAML tags for polymorphic data
# definitions. The standard safe loader does not know them, so we parse them as
# the underlying mapping/sequence/scalar value without interpreting the tag.
def _type_constructor(loader: yaml.SafeLoader, tag_suffix: str, node: yaml.Node) -> Any:
    if isinstance(node, yaml.MappingNode):
        return loader.construct_mapping(node)
    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node)
    return loader.construct_scalar(node)

yaml.SafeLoader.add_multi_constructor("!type:", _type_constructor)

# RobustToolbox uses `!type:<SomeType>` tagged scalars/mappings for type
# discriminators. We do not need the real type, only a valid Python object so
# the YAML parses. Register a multi-constructor that reconstructs the value
# using the appropriate base method.
class Zona14YamlLoader(yaml.SafeLoader):
    pass


def _construct_type_tag(loader: yaml.SafeLoader, suffix: str, node: yaml.Node):
    if isinstance(node, yaml.ScalarNode):
        return loader.construct_scalar(node)
    if isinstance(node, yaml.MappingNode):
        return loader.construct_mapping(node)
    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node)
    return None


Zona14YamlLoader.add_multi_constructor("!type:", _construct_type_tag)

# Prototype types where `categories` is not a valid field at all.
CATEGORIES_FORBIDDEN_TYPES = {
    "stWarZone",
    "stBandShopListings",
    "persistentCraftRecipe",
    "vendingMachineInventory",
}

# Prototype types where `suffix` is not a valid field at all.
SUFFIX_FORBIDDEN_TYPES = CATEGORIES_FORBIDDEN_TYPES | {"shopPreset"}


class Z14SafeLoader(yaml.SafeLoader):
    """SafeLoader that ignores RobustToolbox type tags (e.g. !type:GroupSelector)."""


def _z14_type_constructor(loader: yaml.SafeLoader, tag_suffix: str, node: yaml.Node) -> Any:
    if isinstance(node, MappingNode):
        return loader.construct_mapping(node)
    if isinstance(node, SequenceNode):
        return loader.construct_sequence(node)
    return loader.construct_scalar(node)


Z14SafeLoader.add_multi_constructor("!type:", _z14_type_constructor)


def is_categories_tag(value: Any) -> bool:
    """Return True if `categories` is a list-of-strings entity tag (e.g. [Zona14])."""
    if isinstance(value, str):
        return True
    if isinstance(value, list):
        return all(isinstance(item, str) for item in value)
    return False


def validate_prototype(prototype: Any, file_path: Path, errors: list[str]) -> None:
    if not isinstance(prototype, dict):
        return
    ptype = prototype.get("type")
    if ptype == "entity" or not ptype:
        return

    if "categories" in prototype:
        if ptype in CATEGORIES_FORBIDDEN_TYPES:
            errors.append(f"{file_path}: {ptype} prototype has forbidden field 'categories'")
        elif is_categories_tag(prototype.get("categories")):
            # shopPreset has a `categories` field, but it is a list of objects, not a tag.
            errors.append(f"{file_path}: {ptype} prototype has entity-style 'categories' tag")

    if "suffix" in prototype and ptype in SUFFIX_FORBIDDEN_TYPES:
        errors.append(f"{file_path}: {ptype} prototype has forbidden field 'suffix'")


def validate_file(file_path: Path) -> list[str]:
    errors: list[str] = []
    # Zona14: map files contain engine-specific tags (e.g. !type:SoundPathSpecifier)
    # and are validated by the YAML linter, not this data-prototype check.
    if "Resources/Maps" in str(file_path):
        return errors
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            data = yaml.load(f, Loader=Z14SafeLoader)
    except yaml.YAMLError as exc:
        errors.append(f"{file_path}: YAML parse error: {exc}")
        return errors

    if data is None:
        return errors

    if isinstance(data, list):
        for prototype in data:
            validate_prototype(prototype, file_path, errors)
    elif isinstance(data, dict):
        validate_prototype(data, file_path, errors)

    return errors


def main() -> int:
    if len(sys.argv) < 2:
        print("Usage: check-yaml-data-prototypes.py <file1.yml> [file2.yml ...]", file=sys.stderr)
        return 2

    errors: list[str] = []
    for path in sys.argv[1:]:
        errors.extend(validate_file(Path(path)))

    if errors:
        for err in errors:
            print(err, file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())

#!/usr/bin/env python3
"""Translate Russian YAML name/description fields to English using FTL lookup.
Adds # Zona14: markers to modified files outside _Zona14/."""

import re
import os
import glob
import sys

# Common Russian→English translations for items without FTL coverage
MANUAL_TRANSLATIONS = {
    # Food items
    'консервированная еда': 'canned food',
    'консервированная вишня': 'canned cherries',
    'маринованная вишня': 'pickled cherries',
    'консервированный угорь': 'canned eel',
    'ферментированный угорь': 'fermented eel',
    'кусочки консервированного угря': 'canned eel pieces',
    'маринованные яйца': 'pickled eggs',
    'консервированные рыбные стейки': 'canned fish steaks',
    'ягодное варенье': 'berry jam',
    'свекольный квас': 'beetroot kvass',
    'квас': 'kvass',
    'печоночный тартар': 'liver tartare',
    'топленое сало': 'rendered lard',
    'сосиски': 'sausages',
    'маринованные сосиски': 'pickled sausages',
    'консервированное рыбное филе': 'canned fish fillet',
    'пряное медвежье мясо': 'spiced bear meat',
    'вяленое мясо медведя': 'bear jerky',
    'мясо кабана со специями': 'spiced boar meat',
    'вяленое мясо кабана': 'boar jerky',
    'пряное собачье мясо': 'spiced dog meat',
    'вяленое мясо собаки': 'dog jerky',
    'пряное мясо крысы': 'spiced rat meat',
    'крысиное вяленое мясо': 'rat jerky',
    'пряные щупальца кровососа': 'spiced bloodsucker tentacles',
    'вяленое мясо кровососа': 'bloodsucker jerky',
    'початок кукурузы': 'ear of corn',
    'поварская книга': 'cookbook',
    'тряпка': 'rag',
    'охотничий нож': 'hunting knife',
    'рецепт борща': 'borscht recipe',
    'рецепт каши': 'porridge recipe',
    'рецепт мясной похлёбки': 'meat stew recipe',
    'рецепт жареного мяса': 'fried meat recipe',
    'рецепт жареных грибов': 'fried mushrooms recipe',
    'рецепт грибной похлёбки': 'mushroom stew recipe',
    'рецепт вяленого мяса': 'jerky recipe',
    'рецепт мясных консервов': 'canned meat recipe',
    'рецепт рыбных консервов': 'canned fish recipe',
    'рецепт засахаренных ягод': 'candied berries recipe',
    'рецепт варенья': 'jam recipe',
    'рецепт маринованных яиц': 'pickled eggs recipe',
    'рецепт маринованных сосисок': 'pickled sausages recipe',
    'рецепт фруктового компота': 'fruit compote recipe',
    'рецепт квашенной капусты': 'sauerkraut recipe',
    'рецепт солёных огурцов': 'pickled cucumbers recipe',
    'рецепт топлёного сала': 'rendered lard recipe',
    'рецепт печёночного тартара': 'liver tartare recipe',
    'рецепт пряных щупалец': 'spiced tentacles recipe',
    'рецепт свекольного кваса': 'beetroot kvass recipe',
    'рецепт кваса': 'kvass recipe',
    'рецепт ферментированного угря': 'fermented eel recipe',
    'рецепт копчёной рыбы': 'smoked fish recipe',

    # Equipment/items
    'нашивка': 'patch',
    'сменить нашивку': 'change patch',
    'нашивка "Монолит"': '"Monolith" patch',
    'нашивка Наёмники': 'Mercenary patch',
    'нашивка Военсталкеры': 'Military Stalker patch',
    'нашивка Долг': 'Duty patch',
    'нашивка Свобода': 'Freedom patch',
    'нашивка Бандиты': 'Bandit patch',
    'нашивка Чистое Небо': 'Clear Sky patch',
    'нашивка Серафимы': 'Seraphim patch',
    'нашивка Ренегаты': 'Renegade patch',
    'нашивка Учёные': 'Scientist patch',

    # Detectors/tools
    'Металлодетектор дальнего действия': 'Long-range metal detector',
    'детектор аномалий': 'anomaly detector',
    'детектор': 'detector',
    'генератор': 'generator',
    'рация': 'radio',
    'бинокль': 'binoculars',
    'компас': 'compass',

    # Weapons
    'нож': 'knife',
    'боевой нож': 'combat knife',
    'мачете': 'machete',
    'топор': 'axe',

    # Clothing
    'противогаз': 'gas mask',
    'респиратор': 'respirator',
    'бронежилет': 'body armor',
    'каска': 'helmet',
    'балаклава': 'balaclava',
    'маска': 'mask',
    'перчатки': 'gloves',
    'берцы': 'boots',
    'рюкзак': 'backpack',

    # Misc
    'аптечка': 'medkit',
    'бандаж': 'bandage',
    'антидот': 'antidote',
    'артефакт': 'artifact',
    'аномалия': 'anomaly',
    'патроны': 'ammunition',
    'магазин': 'magazine',
    'обойма': 'clip',
    'граната': 'grenade',

    # Description phrases
    'Один целый початок.': 'One whole ear of corn.',

    # Faction names
    'Бандиты': 'Bandits',
    'Долг': 'Duty',
    'Свобода': 'Freedom',
    'Монолит': 'Monolith',
    'Чистое Небо': 'Clear Sky',
    'Наёмники': 'Mercenaries',
    'Ренегаты': 'Renegades',
    'Серафимы': 'Seraphim',
    'Военсталкеры': 'Military Stalkers',
    'Учёные': 'Scientists',
    'Одиночки': 'Loners',

    # Guard descriptions
    'Охранник фракции': 'Faction guard',
    'Патрульный серафимов, несущий божественную волю через свинец.': 'A Seraphim patrolman, delivering divine will through lead.',
}


def build_ftl_lookup():
    """Build entity_id -> {name, desc} from all en-US FTL files."""
    lookup = {}
    for ftl_file in glob.glob('Resources/Locale/en-US/**/*.ftl', recursive=True):
        with open(ftl_file) as f:
            lines = f.readlines()
        i = 0
        while i < len(lines):
            line = lines[i]
            m = re.match(r'^(ent-\S+)\s*=\s*(.+?)$', line)
            if m:
                entity_id = m.group(1)[4:]  # Remove 'ent-' prefix
                name = m.group(2).strip()
                desc = None
                j = i + 1
                while j < len(lines) and lines[j].startswith('    .'):
                    dm = re.match(r'\s+\.desc\s*=\s*(.+)', lines[j])
                    if dm:
                        desc = dm.group(1).strip()
                    j += 1
                lookup[entity_id] = {'name': name, 'desc': desc}
                i = j
            else:
                i += 1
    return lookup


def has_cyrillic(text):
    return bool(re.search(r'[а-яА-ЯёЁ]', text))


def translate_text(text, ftl_lookup, entity_id, field_type):
    """Translate Russian text to English using FTL or manual dictionary."""
    # Try FTL lookup first
    if entity_id and entity_id in ftl_lookup:
        ftl_entry = ftl_lookup[entity_id]
        if field_type == 'name' and ftl_entry.get('name') and not has_cyrillic(ftl_entry['name']):
            return ftl_entry['name']
        if field_type == 'desc' and ftl_entry.get('desc') and not has_cyrillic(ftl_entry['desc']):
            return ftl_entry['desc']

    # Try manual dictionary
    text_stripped = text.strip().strip('"').strip("'")
    if text_stripped in MANUAL_TRANSLATIONS:
        return MANUAL_TRANSLATIONS[text_stripped]

    # Try case-insensitive match
    text_lower = text_stripped.lower()
    for ru, en in MANUAL_TRANSLATIONS.items():
        if ru.lower() == text_lower:
            return en

    return None


def quote_yaml_value(value):
    """Add YAML quoting if needed."""
    # Need quoting if value starts with quote, has colon+space, or other special chars
    if value.startswith('"') and not value.endswith('"'):
        return "'" + value + "'"
    if value.startswith("'") and not value.endswith("'"):
        return '"' + value + '"'
    if ': ' in value:
        return '"' + value.replace('"', '\\"') + '"'
    if value.startswith('{') or value.startswith('['):
        return '"' + value + '"'
    return value


def process_yaml_file(filepath, ftl_lookup, add_marker=True):
    """Process a single YAML file, translating Russian name/description to English.
    Returns (modified, changes_count)."""
    with open(filepath) as f:
        lines = f.readlines()

    modified = False
    changes = 0
    current_id = None
    current_type = None
    new_lines = []

    for line in lines:
        stripped = line.strip()

        # Track entity type and ID
        if stripped.startswith('- type:'):
            current_type = stripped.split(':', 1)[1].strip()
            current_id = None
        elif stripped.startswith('id:') and current_type:
            current_id = stripped.split(':', 1)[1].strip()

        # Check for Russian name/description
        is_name = stripped.startswith('name:') and has_cyrillic(stripped)
        is_desc = stripped.startswith('description:') and has_cyrillic(stripped)

        if is_name or is_desc:
            field_type = 'name' if is_name else 'desc'
            field_key = 'name' if is_name else 'description'

            # Extract current value
            value_part = stripped.split(':', 1)[1].strip()

            # Try to translate
            translation = translate_text(value_part, ftl_lookup, current_id, field_type)

            if translation:
                # Build the new line preserving indentation
                indent = line[:len(line) - len(line.lstrip())]
                quoted = quote_yaml_value(translation)
                new_line = f"{indent}{field_key}: {quoted}\n"
                new_lines.append(new_line)
                modified = True
                changes += 1
                continue

        new_lines.append(line)

    if modified and add_marker:
        # Add marker as first line
        new_lines.insert(0, "# Zona14: translated Russian names/descriptions to English\n")

    if modified:
        with open(filepath, 'w') as f:
            f.writelines(new_lines)

    return modified, changes


def main():
    ftl_lookup = build_ftl_lookup()
    print(f"FTL lookup: {len(ftl_lookup)} entities")

    base_paths = sys.argv[1:] if len(sys.argv) > 1 else [
        'Resources/Prototypes/_Stalker_EN',
        'Resources/Prototypes/_Stalker',
    ]

    total_files = 0
    total_changes = 0
    remaining_russian = 0

    for base_path in base_paths:
        print(f"\n=== Processing {base_path} ===")
        path_files = 0
        path_changes = 0

        for filepath in sorted(glob.glob(f'{base_path}/**/*.yml', recursive=True)):
            is_zona14 = '/_Zona14/' in filepath
            modified, changes = process_yaml_file(filepath, ftl_lookup, add_marker=not is_zona14)
            if modified:
                path_files += 1
                path_changes += changes
                print(f"  {filepath}: {changes} translations")

        total_files += path_files
        total_changes += path_changes
        print(f"  Subtotal: {path_files} files, {path_changes} changes")

    print(f"\n=== TOTAL: {total_files} files, {total_changes} changes ===")

    # Count remaining Russian
    for base_path in base_paths:
        for filepath in sorted(glob.glob(f'{base_path}/**/*.yml', recursive=True)):
            with open(filepath) as f:
                for line in f:
                    stripped = line.strip()
                    if (stripped.startswith('name:') or stripped.startswith('description:')) and has_cyrillic(stripped):
                        remaining_russian += 1

    print(f"Remaining Russian name/desc fields: {remaining_russian}")


if __name__ == '__main__':
    main()

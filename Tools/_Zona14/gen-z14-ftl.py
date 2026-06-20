#!/usr/bin/env python3
"""
Generate Z14-prefixed FTL locale files for all Z14 entity prototypes.
- Proper Fluent attribute syntax (.desc/.suffix indented under parent)
- Lowercase paths to avoid case collisions
- Reads suffix from YAML prototypes (uses Z14 convention)
- Deduplicates against existing _Zona14 FTL keys
"""
import os
import re
import sys
from pathlib import Path
from collections import defaultdict

# Add script directory to path for imports
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ru_desc_translations import TRANSLATIONS as RU_DESC_TRANSLATIONS

REPO = Path("/home/ubuntu/repos/Zona-14")
PROTO_DIR = REPO / "Resources/Prototypes/_Zona14"
UPSTREAM_DIRS = [
    REPO / "Resources/Prototypes/_Stalker",
    REPO / "Resources/Prototypes/_Stalker_EN",
]
LOCALE_ENUS = REPO / "Resources/Locale/en-US"
LOCALE_RURU = REPO / "Resources/Locale/ru-RU"
OUT_ENUS = LOCALE_ENUS / "_Zona14"
OUT_RURU = LOCALE_RURU / "_Zona14"

def extract_upstream_yaml_ru():
    """Parse upstream _Stalker/_Stalker_EN YAML for original Russian names/descs."""
    data = {}
    for sdir in UPSTREAM_DIRS:
        if not sdir.exists():
            continue
        for root, dirs, files in os.walk(sdir):
            for fname in files:
                if not fname.endswith(('.yml', '.yaml')):
                    continue
                text = (Path(root) / fname).read_text(encoding='utf-8')
                cur_id = None
                cur_name = None
                cur_desc = None
                for line in text.split('\n'):
                    idm = re.match(r'^  id:\s+(.+?)(?:\s*#.*)?$', line)
                    if idm:
                        if cur_id and cur_name:
                            data[cur_id] = {'name': cur_name, 'desc': cur_desc}
                        cur_id = idm.group(1).strip()
                        cur_name = None
                        cur_desc = None
                        continue
                    nm = re.match(r'^  name:\s+(.+)$', line)
                    if nm and cur_id:
                        cur_name = nm.group(1).strip().strip('"').strip("'")
                        continue
                    dm = re.match(r'^  description:\s+(.+)$', line)
                    if dm and cur_id:
                        cur_desc = dm.group(1).strip().strip('"').strip("'")
                        continue
                if cur_id and cur_name:
                    data[cur_id] = {'name': cur_name, 'desc': cur_desc}
    return data


def has_cyrillic(text):
    return any('\u0400' <= c <= '\u04FF' for c in text)


# English-to-Russian translation map for common game terms in entity names
EN_RU_NAME_MAP = {
    # Dogtags
    'bear trophy': 'медвежий трофей',
    'bandit dogtag': 'жетон бандита',
    'bandit pahan dogtag': 'жетон пахана бандитов',
    'stalker dogtag': 'жетон сталкера',
    'official dogtag': 'жетон офицера',
    'military dogtag': 'военный жетон',
    'mercenary dogtag': 'жетон наёмника',
    'scientist dogtag': 'жетон учёного',
    'duty dogtag': 'жетон долговца',
    'freedom dogtag': 'жетон свободовца',
    'monolith dogtag': 'жетон монолитовца',
    'ecologist dogtag': 'жетон эколога',
    'loner dogtag': 'жетон одиночки',
    'renegade dogtag': 'жетон ренегата',
    'press id': 'пресс-удостоверение',
    # Food/consumables
    'cabinet': 'шкаф',
    'cossacks vodka bottle': 'бутылка казачьей водки',
    'jerked bear meat': 'вяленое мясо медведя',
    'jerked boar meat': 'вяленое мясо кабана',
    'jerked dog meat': 'вяленое мясо собаки',
    'jerked rat meat': 'вяленое мясо крысы',
    'jerked sucker meat': 'вяленое мясо кровососа',
    'jerked bloodsucker meat': 'вяленое мясо кровососа',
    'jerked cat meat': 'вяленое мясо кота',
    'jerked flesh meat': 'вяленое мясо плоти',
    'jerked izlom meat': 'вяленое мясо излома',
    'jerked chimera meat': 'вяленое мясо химеры',
    'corn cob': 'кукурузный початок',
    'hrechanyky': 'гречаники',
    'syrnyky': 'сырники',
    'rotten meat': 'гнилое мясо',
    'raw fish steak': 'сырой рыбный стейк',
    'rotten fish': 'гнилая рыба',
    'raw rat meat': 'сырое мясо крысы',
    'raw eel': 'сырой угорь',
    'canned fish fillet': 'консервированное рыбное филе',
    'cooked salo': 'жареное сало',
    'borscht': 'борщ',
    # Medicine
    'barberry-soaked bandage': 'бинт, пропитанный барбарисом',
    'universal-salve-soaked bandage': 'бинт с универсальной мазью',
    'fuflomycin pill pack': 'упаковка фуфломицина',
    'probudophiline syringe': 'шприц пробудофилина',
    # Weapons
    'hunting knife': 'охотничий нож',
    'claymore': 'клеймор',
    'bolt bullet': 'арбалетный болт',
    'fireball': 'огненный шар',
    'ice shard': 'ледяной осколок',
    'lightning bolt': 'молния',
    'disco ball': 'диско-шар',
    'smg': 'ПП',
    # Equipment
    'Cloak Base': 'Базовый плащ',
    'Gas Mask Base': 'Базовый противогаз',
    'Helmet Base': 'Базовый шлем',
    'Holster with Pouches': 'Кобура с подсумками',
    'T1 Armor Base': 'Базовая броня Т1',
    'T1 PvE Armor Base': 'Базовая PvE броня Т1',
    'T1 PvP Armor Base': 'Базовая PvP броня Т1',
    'T2 Armor Base': 'Базовая броня Т2',
    'T2 PvE Armor Base': 'Базовая PvE броня Т2',
    'T2 PvP Armor Base': 'Базовая PvP броня Т2',
    'T3 Armor Base': 'Базовая броня Т3',
    'T3 PvE Armor Base': 'Базовая PvE броня Т3',
    'T3 PvP Armor Base': 'Базовая PvP броня Т3',
    'T4 Armor Base': 'Базовая броня Т4',
    'T4 PvE Armor Base': 'Базовая PvE броня Т4',
    'T4 PvP Armor Base': 'Базовая PvP броня Т4',
    'T5 Armor Base': 'Базовая броня Т5',
    'T5 PvE Armor Base': 'Базовая PvE броня Т5',
    'T5 PvP Armor Base': 'Базовая PvP броня Т5',
    # Devices
    'camera': 'камера',
    'damaged camera': 'повреждённая камера',
    'camera film': 'плёнка для камеры',
    'vintage camera': 'винтажная камера',
    'photograph': 'фотография',
    'Upgraded Barracuda Detector': 'Улучшенный детектор Барракуда',
    # NPCs
    'boss trigger': 'триггер босса',
    'Ecologist Guard Spawner': 'Спавнер охранника Эколога',
    'Banner': 'Знаменосец',
    'Barman': 'Бармен',
    'Kesha': 'Кеша',
    'Max': 'Макс',
    "Vitaly 'Quartermaster'": 'Виталий «Каптёрщик»',
    # Anomalies/Artifacts
    'anomaly': 'аномалия',
    'EMP': 'ЭМИ',
    'glass shards': 'стеклянные осколки',
    'Smoke': 'Дым',
    'Accumulator': 'Аккумулятор',
    'Arf': 'Арф',
    'Conflagration': 'Пожар',
    'Floodlight': 'Прожектор',
    'Thorn': 'Шип',
    # Mutants
    'Psi-Bear': 'Пси-Медведь',
    'Bloodsucker': 'Кровосос',
    'Mannequin': 'Манекен',
    'Oracle': 'Оракул',
    'Zombie': 'Зомби',
    'Izlom': 'Излом',
    'Pseudogiant': 'Псевдогигант',
    'poltergeist': 'полтергейст',
    'Leshiy': 'Леший',
    'Pseudodog': 'Псевдопёс',
    'Psi-Dog': 'Пси-Пёс',
    'Seer': 'Провидец',
    'Snork': 'Снорк',
    'Boar': 'Кабан',
    'Bayun': 'Баюн',
    'Flesh': 'Плоть',
    'apparition': 'призрак',
    # Plants
    'shrub with berries': 'куст с ягодами',
    'grass': 'трава',
    # Misc entities
    'Korund (modified)': 'Корунд (модифицированный)',
    'helmet (modification test)': 'шлем (тест модификации)',
    # War zones
    'Kordon Village War Zone': 'Военная зона Деревня Кордон',
    'Z14KordonATP War Zone': 'Военная зона АТП Кордон',
    'Ferma War Zone': 'Военная зона Ферма',
    'Lenin War Zone': 'Военная зона Ленина',
    'Depo War Zone': 'Военная зона Депо',
}

# Mutant name translations
MUTANT_QUALITY_RU = {
    'Singular': 'Уникальный',
    'Mutated': 'Мутировавший',
    'Adapted': 'Адаптировавшийся',
    'Old': 'Старый',
    'Seasoned': 'Бывалый',
    'Psi-Bear': 'Пси-Медведь',
    'Bear': 'Медведь',
    'Bayun': 'Баюн',
    'Cat': 'Кот',
    'Chimera': 'Химера',
    'Izlom': 'Излом',
    'Mannequin': 'Манекен',
    'Poltergeist': 'Полтергейст',
    'Seer': 'Провидец',
    'Snork': 'Снорк',
    'Bloodsucker': 'Кровосос',
    'Flesh': 'Плоть',
    'Boar': 'Кабан',
    'Dog': 'Собака',
    'Tushkano': 'Тушкан',
    'Rat': 'Крыса',
    'Apparition': 'Призрак',
    'Controller': 'Контролёр',
    'Burer': 'Бюрер',
    'Psydog': 'Псевдопёс',
    'Pseudo-Giant': 'Псевдогигант',
    'Oracle': 'Оракул',
    'Zombie': 'Зомби',
    'Pseudogiant': 'Псевдогигант',
    'Pseudodog': 'Псевдопёс',
    'Psi-Dog': 'Пси-Пёс',
    'Leshiy': 'Леший',
}

# Ammo/bullet term translations
BULLET_TERMS_RU = {
    'bullet': 'пуля',
    'cartridge': 'патрон',
    'shell': 'гильза',
    'ammunition box': 'коробка боеприпасов',
}


def translate_name_to_ru(en_name):
    """Translate an English entity name to Russian using known patterns."""
    nl = en_name.lower().strip()
    # Strip NPC arrow formatting for matching, re-add after
    arrow_suffix = ''
    am = re.match(r'^(.+?)\s*(\[.*\])\s*$', en_name)
    if am:
        clean_name = am.group(1).strip()
        arrow_suffix = ' ' + am.group(2)
    else:
        clean_name = en_name.strip()
    cnl = clean_name.lower()
    # Exact match (case-insensitive)
    for en, ru in EN_RU_NAME_MAP.items():
        if cnl == en.lower():
            return ru + arrow_suffix
    # Bullet/ammo patterns: "bullet (caliber type)"
    bm = re.match(r'^(bullet|cartridge|shell|ammunition box)\s*(\(.+\))$', en_name, re.IGNORECASE)
    if bm:
        term = bm.group(1).lower()
        cal = bm.group(2)
        ru_term = BULLET_TERMS_RU.get(term, term)
        return f'{ru_term} {cal}'
    # Weapon names with quotes — keep model designation, translate common prefix
    wm = re.match(r'^([A-Za-z0-9\-]+)\s+["«](.+)["»]$', en_name)
    if wm:
        return en_name  # Weapon model names stay as-is
    # Mutant names: "Quality Base" pattern
    words = clean_name.split()
    if len(words) >= 2:
        translated = [MUTANT_QUALITY_RU.get(w, w) for w in words]
        if translated != words:
            return ' '.join(translated) + arrow_suffix
    # Single mutant name
    if clean_name in MUTANT_QUALITY_RU:
        return MUTANT_QUALITY_RU[clean_name] + arrow_suffix
    # Trigger patterns: "X trigger (N) (P%) Nm" or "X (N) (P%) Nm"
    tm = re.match(r'^(.+?)\s+trigger\s*(.*)', en_name, re.IGNORECASE)
    if tm:
        base = tm.group(1).strip()
        rest = tm.group(2).strip()
        base_ru = translate_name_to_ru(base) if base else base
        return f'триггер {base_ru} {rest}'.strip()
    # Some triggers don't have the word 'trigger' — pattern: "name (N) (P%) Nm"
    tp = re.match(r'^(.+?)\s+(\(\d+.*)', en_name)
    if tp:
        base = tp.group(1).strip()
        rest = tp.group(2).strip()
        for en_mut, ru_mut in MUTANT_QUALITY_RU.items():
            if base.lower() == en_mut.lower():
                return f'{ru_mut} {rest}'.strip()
    # Spawner patterns
    sm = re.match(r'^(.+?)\s+(?:Guard\s+)?Spawner\s*(.*)', en_name, re.IGNORECASE)
    if sm:
        base = sm.group(1).strip()
        rest = sm.group(2).strip()
        base_ru = translate_name_to_ru(base) if base else base
        return f'спавнер {base_ru} {rest}'.strip()
    # Dogtag patterns
    dm = re.match(r'^(.+?)\s+dogtag$', en_name, re.IGNORECASE)
    if dm:
        base = dm.group(1).strip()
        return f'жетон {base}'
    # Scarf patterns
    if 'scarf' in nl:
        return en_name.replace('scarf', 'шарф').replace('Scarf', 'Шарф')
    # Reagent locale references — keep as-is
    if en_name.startswith('reagent-name-') or en_name.startswith('stack-st-'):
        return en_name
    # "A rifle" type bases
    if nl.startswith('a '):
        return en_name
    return en_name + arrow_suffix


def translate_desc_to_ru(en_desc):
    """Translate an English description to Russian."""
    if not en_desc:
        return en_desc
    desc_map = {
        'A mob trigger for the zone.': 'Триггер мобов для зоны.',
        'A guard spawner.': 'Спавнер охранников.',
        'A mob spawner for the zone.': 'Спавнер мобов для зоны.',
        'A pack spawner for the zone.': 'Спавнер стаи для зоны.',
        'A zone mutant.': 'Мутант зоны.',
        'A common mutant variant.': 'Обычный вариант мутанта.',
        'An uncommon mutant variant.': 'Необычный вариант мутанта.',
        'A rare mutant variant.': 'Редкий вариант мутанта.',
        'A legendary mutant variant.': 'Легендарный вариант мутанта.',
        'A loot container.': 'Контейнер с добычей.',
        'A mutant ability.': 'Способность мутанта.',
        'A piece of zone equipment.': 'Экипировка зоны.',
        'A zone weapon.': 'Оружие зоны.',
        'A consumable item.': 'Расходный предмет.',
        'A zone device.': 'Устройство зоны.',
        'A zone anomaly.': 'Аномалия зоны.',
        'A zone artifact.': 'Артефакт зоны.',
        'A zone entity.': 'Объект зоны.',
    }
    if en_desc in desc_map:
        return desc_map[en_desc]
    # Check comprehensive translation dictionary
    if en_desc in RU_DESC_TRANSLATIONS:
        return RU_DESC_TRANSLATIONS[en_desc]
    return en_desc


def extract_z14_entities():
    """Return dict: z14_id -> {name, description, upstream_id, source_file, suffix}"""
    entities = {}
    for root, dirs, files in os.walk(PROTO_DIR):
        for fname in files:
            if not fname.endswith(('.yml', '.yaml')):
                continue
            fpath = Path(root) / fname
            rel = fpath.relative_to(PROTO_DIR)
            text = fpath.read_text(encoding='utf-8')
            lines = text.split('\n')

            current_id = None
            current_name = None
            current_desc = None
            current_suffix = None

            for line in lines:
                id_match = re.match(r'^  id:\s+(.+?)(?:\s*#.*)?$', line)
                if id_match:
                    if current_id and current_id.startswith('Z14'):
                        entities[current_id] = {
                            'name': current_name,
                            'description': current_desc,
                            'upstream_id': current_id[3:],
                            'source_file': str(rel),
                            'suffix': current_suffix,
                        }
                    current_id = id_match.group(1).strip()
                    current_name = None
                    current_desc = None
                    current_suffix = None
                    continue

                name_match = re.match(r'^  name:\s+(.+)$', line)
                if name_match and current_id:
                    current_name = name_match.group(1).strip().strip('"').strip("'")
                    continue

                desc_match = re.match(r'^  description:\s+(.+)$', line)
                if desc_match and current_id:
                    current_desc = desc_match.group(1).strip().strip('"').strip("'")
                    continue

                suffix_match = re.match(r'^  suffix:\s+(.+?)(?:\s*#.*)?$', line)
                if suffix_match and current_id:
                    current_suffix = suffix_match.group(1).strip()
                    continue

            if current_id and current_id.startswith('Z14'):
                entities[current_id] = {
                    'name': current_name,
                    'description': current_desc,
                    'upstream_id': current_id[3:],
                    'source_file': str(rel),
                    'suffix': current_suffix,
                }

    return entities


def parse_ftl_entities(locale_root):
    """Parse all FTL files. Return dict: entity_id -> {file, name, attrs}"""
    index = {}
    for root, dirs, files in os.walk(locale_root):
        for fname in files:
            if not fname.endswith('.ftl'):
                continue
            fpath = Path(root) / fname
            rel = fpath.relative_to(locale_root)
            if str(rel).startswith('_Zona14'):
                continue

            text = fpath.read_text(encoding='utf-8')
            current_entity = None
            current_name = None
            current_attrs = {}

            for line in text.split('\n'):
                msg_match = re.match(r'^(ent-([A-Za-z0-9_-]+))\s*=\s*(.+)$', line)
                if msg_match:
                    if current_entity and current_entity not in index:
                        index[current_entity] = {
                            'file': str(rel),
                            'name': current_name,
                            'attrs': dict(current_attrs),
                        }
                    current_entity = msg_match.group(2)
                    current_name = msg_match.group(3).strip()
                    current_attrs = {}
                    continue

                attr_match = re.match(r'^[ \t]+\.(\w+)\s*=\s*(.+)$', line)
                if attr_match and current_entity:
                    current_attrs[attr_match.group(1)] = attr_match.group(2).strip()
                    continue

                if line.strip() == '' or line.startswith('#'):
                    if current_entity and current_entity not in index:
                        index[current_entity] = {
                            'file': str(rel),
                            'name': current_name,
                            'attrs': dict(current_attrs),
                        }
                    current_entity = None

            if current_entity and current_entity not in index:
                index[current_entity] = {
                    'file': str(rel),
                    'name': current_name,
                    'attrs': dict(current_attrs),
                }

    return index


def get_existing_z14_keys(locale_root):
    """Get all ent- keys already defined in _Zona14 FTL files."""
    keys = set()
    z14_dir = locale_root / "_Zona14"
    if not z14_dir.exists():
        return keys
    for root, dirs, files in os.walk(z14_dir):
        for fname in files:
            if not fname.endswith('.ftl'):
                continue
            text = (Path(root) / fname).read_text(encoding='utf-8')
            for m in re.finditer(r'^(ent-[A-Za-z0-9_-]+)\s*=', text, re.MULTILINE):
                keys.add(m.group(1))
    return keys


def generate_desc_from_name(z14_id, name):
    """Generate a short description for entities that have no upstream or YAML desc."""
    eid = z14_id.upper()
    if name:
        nl = name.lower()
    else:
        nl = ''

    if 'TRIGGER' in eid or 'trigger' in nl:
        return 'A mob trigger for the zone.'
    if 'GUARDSPAWNER' in eid or 'guard spawner' in nl:
        return 'A guard spawner.'
    if 'SPAWNER' in eid or 'spawner' in nl:
        return 'A mob spawner for the zone.'
    if 'PACK' in eid and ('TRIGGER' in eid or 'SPAWNER' in eid):
        return 'A pack spawner for the zone.'
    if 'MOB' in eid and 'MUTANT' in eid:
        for quality in ['Legendary', 'Rare', 'Uncommon', 'Common']:
            if quality in z14_id:
                article = 'An' if quality[0] in 'AEIOUaeiou' else 'A'
                return f'{article} {quality.lower()} mutant variant.'
        return 'A zone mutant.'
    if 'LOOTBOX' in eid or 'loot' in nl:
        return 'A loot container.'
    if 'ACTION' in eid:
        return 'A mutant ability.'
    if 'CLOTHING' in eid:
        return 'A piece of zone equipment.'
    if 'WEAPON' in eid or 'GUN' in eid:
        return 'A zone weapon.'
    if 'CONSUMABLE' in eid or 'FOOD' in eid or 'DRINK' in eid:
        return 'A consumable item.'
    if 'DEVICE' in eid or 'DETECTOR' in eid:
        return 'A zone device.'
    if 'ANOMALY' in eid:
        return 'A zone anomaly.'
    if 'ARTIFACT' in eid:
        return 'A zone artifact.'
    return 'A zone entity.'


def generate_desc_from_name_ru(z14_id, name):
    """Generate a short Russian description for entities without one."""
    eid = z14_id.upper()
    if name:
        nl = name.lower()
    else:
        nl = ''

    if 'TRIGGER' in eid or 'trigger' in nl:
        return 'Триггер мобов для зоны.'
    if 'GUARDSPAWNER' in eid or 'guard spawner' in nl:
        return 'Спавнер охранников.'
    if 'SPAWNER' in eid or 'spawner' in nl:
        return 'Спавнер мобов для зоны.'
    if 'PACK' in eid and ('TRIGGER' in eid or 'SPAWNER' in eid):
        return 'Спавнер стаи для зоны.'
    if 'MOB' in eid and 'MUTANT' in eid:
        quality_map = {
            'Legendary': 'Легендарный',
            'Rare': 'Редкий',
            'Uncommon': 'Необычный',
            'Common': 'Обычный',
        }
        for quality_en, quality_ru in quality_map.items():
            if quality_en in z14_id:
                return f'{quality_ru} вариант мутанта.'
        return 'Мутант зоны.'
    if 'LOOTBOX' in eid or 'loot' in nl:
        return 'Контейнер с добычей.'
    if 'ACTION' in eid:
        return 'Способность мутанта.'
    if 'CLOTHING' in eid:
        return 'Экипировка зоны.'
    if 'WEAPON' in eid or 'GUN' in eid:
        return 'Оружие зоны.'
    if 'CONSUMABLE' in eid or 'FOOD' in eid or 'DRINK' in eid:
        return 'Расходный предмет.'
    if 'DEVICE' in eid or 'DETECTOR' in eid:
        return 'Устройство зоны.'
    if 'ANOMALY' in eid:
        return 'Аномалия зоны.'
    if 'ARTIFACT' in eid:
        return 'Артефакт зоны.'
    return 'Объект зоны.'


def write_ftl_entry(z14_id, name, attrs):
    lines = [f"ent-{z14_id} = {name}"]
    for attr_name in sorted(attrs):
        lines.append(f"    .{attr_name} = {attrs[attr_name]}")
    return '\n'.join(lines)


def generate_ftl_files(z14_entities, enus_index, ruru_index):
    # Get keys already in _Zona14 FTL (system strings, etc.)
    existing_enus = get_existing_z14_keys(LOCALE_ENUS)
    existing_ruru = get_existing_z14_keys(LOCALE_RURU)

    stats = {'enus_copied': 0, 'ruru_copied': 0, 'enus_new': 0, 'enus_skipped_dup': 0, 'ruru_new': 0}

    enus_by_file = defaultdict(list)
    ruru_by_file = defaultdict(list)
    no_ftl_entities = []
    ruru_no_ftl_entities = []

    for z14_id, info in sorted(z14_entities.items()):
        upstream_id = info['upstream_id']

        # Build Z14 attrs — override suffix with YAML value
        # IMPORTANT: if we include .suffix we MUST also include .desc,
        # otherwise the engine errors with "No value: ent-ID.desc".
        def make_z14_attrs(upstream_attrs, yaml_suffix, yaml_desc, z14_id, name):
            attrs = dict(upstream_attrs)
            if yaml_suffix:
                attrs['suffix'] = yaml_suffix
            elif 'suffix' in attrs:
                attrs['suffix'] = 'Z14'
            # Ensure .desc exists when .suffix is present
            if 'suffix' in attrs and 'desc' not in attrs:
                if yaml_desc:
                    attrs['desc'] = yaml_desc
                else:
                    attrs['desc'] = generate_desc_from_name(z14_id, name)
            return attrs

        # en-US
        if f"ent-{z14_id}" in existing_enus:
            stats['enus_skipped_dup'] += 1
        elif upstream_id in enus_index:
            entry = enus_index[upstream_id]
            attrs = make_z14_attrs(entry['attrs'], info['suffix'],
                                   info['description'], z14_id, entry['name'])
            # Use lowercase path to avoid case collisions
            src_file = str(entry['file']).lower()
            enus_by_file[src_file].append((z14_id, entry['name'], attrs))
            stats['enus_copied'] += 1
        elif info['name']:
            no_ftl_entities.append((z14_id, info))
            stats['enus_new'] += 1

        # ru-RU
        if f"ent-{z14_id}" in existing_ruru:
            pass
        elif upstream_id in ruru_index:
            entry = ruru_index[upstream_id]
            attrs = make_z14_attrs(entry['attrs'], info['suffix'],
                                   info['description'], z14_id, entry['name'])
            src_file = str(entry['file']).lower()
            ruru_by_file[src_file].append((z14_id, entry['name'], attrs))
            stats['ruru_copied'] += 1
        else:
            # No upstream ru-RU — will generate later
            ruru_no_ftl_entities.append((z14_id, info))

    # Write en-US
    enus_files = 0
    for src_file, entries in sorted(enus_by_file.items()):
        out_path = OUT_ENUS / src_file
        out_path.parent.mkdir(parents=True, exist_ok=True)
        blocks = [write_ftl_entry(z, n, a) for z, n, a in entries]
        out_path.write_text('\n\n'.join(blocks) + '\n', encoding='utf-8')
        enus_files += 1

    # Write en-US for YAML-only entities
    new_by_dir = defaultdict(list)
    for z14_id, info in no_ftl_entities:
        src_dir = str(Path(info['source_file']).parent).lower()
        new_by_dir[src_dir].append((z14_id, info))

    for src_dir, entries in sorted(new_by_dir.items()):
        out_path = OUT_ENUS / "entities" / src_dir / "entities.ftl"
        out_path.parent.mkdir(parents=True, exist_ok=True)
        blocks = []
        for z14_id, info in entries:
            attrs = {}
            if info['description']:
                attrs['desc'] = info['description']
            if info['suffix']:
                attrs['suffix'] = info['suffix']
                # Ensure .desc exists when .suffix is present
                if 'desc' not in attrs:
                    attrs['desc'] = generate_desc_from_name(z14_id, info['name'])
            blocks.append(write_ftl_entry(z14_id, info['name'], attrs))
        if blocks:
            out_path.write_text('\n\n'.join(blocks) + '\n', encoding='utf-8')
            enus_files += 1

    # Write ru-RU (upstream-copied)
    ruru_files = 0
    for src_file, entries in sorted(ruru_by_file.items()):
        out_path = OUT_RURU / src_file
        out_path.parent.mkdir(parents=True, exist_ok=True)
        blocks = [write_ftl_entry(z, n, a) for z, n, a in entries]
        out_path.write_text('\n\n'.join(blocks) + '\n', encoding='utf-8')
        ruru_files += 1

    # Write ru-RU for entities without upstream ru-RU
    # Use upstream YAML Russian names when available, translate otherwise
    upstream_ru = extract_upstream_yaml_ru()
    ruru_new_by_dir = defaultdict(list)
    for z14_id, info in ruru_no_ftl_entities:
        upstream_id = info['upstream_id']
        # Get English name for fallback
        if upstream_id in enus_index:
            en_name = enus_index[upstream_id]['name']
        else:
            en_name = info['name']
        if not en_name:
            continue
        # Determine Russian name and description from upstream YAML
        ru_desc = None
        if upstream_id in upstream_ru:
            udata = upstream_ru[upstream_id]
            if has_cyrillic(udata['name']):
                ru_name = udata['name']
            else:
                ru_name = translate_name_to_ru(en_name)
            # Always try upstream Russian desc regardless of name language
            if udata.get('desc') and has_cyrillic(udata['desc']):
                ru_desc = udata['desc']
        else:
            ru_name = translate_name_to_ru(en_name)
        src_dir = str(Path(info['source_file']).parent).lower()
        ruru_new_by_dir[src_dir].append((z14_id, ru_name, ru_desc, info))

    for src_dir, entries in sorted(ruru_new_by_dir.items()):
        out_path = OUT_RURU / "entities" / src_dir / "entities.ftl"
        out_path.parent.mkdir(parents=True, exist_ok=True)
        blocks = []
        for z14_id, ru_name, ru_desc, info in entries:
            attrs = {}
            if ru_desc and has_cyrillic(ru_desc):
                attrs['desc'] = ru_desc
            elif info['description']:
                attrs['desc'] = translate_desc_to_ru(info['description'])
            if info['suffix']:
                attrs['suffix'] = info['suffix']
                if 'desc' not in attrs:
                    attrs['desc'] = generate_desc_from_name_ru(z14_id, ru_name)
            blocks.append(write_ftl_entry(z14_id, ru_name, attrs))
        if blocks:
            out_path.write_text('\n\n'.join(blocks) + '\n', encoding='utf-8')
            ruru_files += 1
    stats['ruru_new'] = len(ruru_no_ftl_entities)

    return stats, enus_files, ruru_files


def main():
    print("Step 1: Extracting Z14 entity IDs from prototypes...")
    z14_entities = extract_z14_entities()
    print(f"  Found {len(z14_entities)} Z14 entities")

    print("Step 2: Parsing en-US FTL files...")
    enus_index = parse_ftl_entities(LOCALE_ENUS)
    print(f"  Indexed {len(enus_index)} upstream entities")

    print("Step 3: Parsing ru-RU FTL files...")
    ruru_index = parse_ftl_entities(LOCALE_RURU)
    print(f"  Indexed {len(ruru_index)} upstream entities")

    matched_enus = sum(1 for z in z14_entities if z14_entities[z]['upstream_id'] in enus_index)
    matched_ruru = sum(1 for z in z14_entities if z14_entities[z]['upstream_id'] in ruru_index)
    print(f"\n  Coverage: {matched_enus} en-US, {matched_ruru} ru-RU out of {len(z14_entities)}")

    print("\nStep 4: Generating Z14 FTL files...")
    stats, enus_files, ruru_files = generate_ftl_files(z14_entities, enus_index, ruru_index)

    print(f"\n=== Results ===")
    print(f"  en-US copied from upstream: {stats['enus_copied']}")
    print(f"  en-US generated from YAML:  {stats['enus_new']}")
    print(f"  en-US skipped (duplicate):  {stats['enus_skipped_dup']}")
    print(f"  ru-RU copied from upstream: {stats['ruru_copied']}")
    print(f"  ru-RU generated (new):      {stats['ruru_new']}")
    print(f"  en-US files written: {enus_files}")
    print(f"  ru-RU files written: {ruru_files}")


if __name__ == '__main__':
    main()

#!/usr/bin/env python3
"""Comprehensive translator for _Stalker/ Russian YAML fields.
Uses FTL lookup, pattern matching, and manual dictionary."""

import re
import os
import glob
import sys

# === COMPREHENSIVE MANUAL TRANSLATIONS ===
TRANSLATIONS = {
    # Characters/Stats
    'Сила': 'Strength',
    'Ловкость': 'Dexterity',
    'Выносливость': 'Endurance',
    'Знания': 'Knowledge',
    'Внимательность': 'Attention',
    'Псионика': 'Psionics',
    'Карма': 'Karma',

    # Guards
    'Охранник бандитов': 'Bandit guard',
    'Охранник бандитов.': 'Bandit guard.',
    'Охранник чистого неба': 'Clear Sky guard',
    'Охранник чистого неба.': 'Clear Sky guard.',
    'Охранник долга': 'Duty guard',
    'Охранник долга.': 'Duty guard.',
    'Охранник нейтралов': 'Neutral guard',
    'Охранник нейтралов.': 'Neutral guard.',
    'Охранник свободы': 'Freedom guard',
    'Охранник военных сталкеров': 'Military Stalker guard',
    'Охранник военных сталкеров.': 'Military Stalker guard.',
    'Охранник серафимов': 'Seraphim guard',
    'Свободовец': 'Freedom member',
    'Неизвестный': 'Unknown',
    'Неизвестный монолитовец': 'Unknown Monolith member',
    'Патрульный Серафимов': 'Seraphim patrolman',
    'Патрульный Серафимов, приносит божью волю свинцом.': 'A Seraphim patrolman, delivering divine will through lead.',
    'Херувим серафимов': 'Seraphim cherub',
    'Сталкер Ополченец': 'Stalker Militiaman',
    'постовой базовый': 'basic sentry',
    'постовой СОП': 'SOP sentry',
    'Рядовой Оковчук': 'Private Okovchuk',
    'Элита': 'Elite',
    'Огромный опасный медведь': 'Huge dangerous bear',
    'Опасная псевдособака': 'Dangerous pseudodog',
    'Проводник': 'Guide',
    'Атом': 'Atom',
    'Вова Вист': 'Vova Vist',
    'Сидорович Реплика': 'Sidorovich Replica',
    'Меченый': 'Marked One',
    'Меченый Spawner': 'Marked One Spawner',
    'Меченый T4': 'Marked One T4',
    'Меченый T4 Spawner': 'Marked One T4 Spawner',
    'Мэдвэдь': 'Medved',
    'Кабан': 'Boar',
    'Кровосос': 'Bloodsucker',
    'Полтейргест': 'Poltergeist',
    'Леший': 'Leshy',
    'Конь': 'Horse',
    'зомбированный': 'zombified',
    'Бумбокс': 'Boombox',

    # Weapons
    'АК-104': 'AK-104',
    'АК-104 (модифицированный)': 'AK-104 (modified)',
    'АК-105 Болотная тварь': 'AK-105 Swamp Beast',
    'АК-12': 'AK-12',
    'АК-12 серафимов': 'AK-12 Seraphim',
    'АК-15': 'AK-15',
    'АК-74М T4.5': 'AK-74M T4.5',
    'АКС-74 T3': 'AKS-74 T3',
    'АКС-74У T1': 'AKS-74U T1',
    'АПС T2': 'APS T2',
    'AN-94 «Абакан»': 'AN-94 "Abakan"',
    'ГШ-18': 'GSh-18',
    'Бизон T2': 'Bizon T2',
    'Кедр T2': 'Kedr T2',
    'Мосина T2': 'Mosin T2',
    'МР-133 T2': 'MP-133 T2',
    'МР-153 T3': 'MP-153 T3',
    'ТОЗ-34 T1': 'TOZ-34 T1',
    'Обрез T1': 'Sawed-off T1',
    'РПК-16': 'RPK-16',
    'РПК-16 "Осень': 'RPK-16 "Autumn"',
    'РПК-74 T3': 'RPK-74 T3',
    'РПГ-7': 'RPG-7',
    'РПО-А "Шмель': 'RPO-A "Bumblebee"',
    'СР-1 тактический': 'SR-1 Tactical',
    'СР-2 «Вереск»': 'SR-2 "Veresk"',
    'СР-2 «Вереск» серафимов': 'SR-2 "Veresk" Seraphim',
    'СР-2М «Вереск» 9х21': 'SR-2M "Veresk" 9x21',
    'СР-2М «Вереск» серафимов 9x21': 'SR-2M "Veresk" Seraphim 9x21',
    'Сайга 7.62 T1.5': 'Saiga 7.62 T1.5',
    'Циклоп T2': 'Cyclops T2',
    'ВСВ Глухарь T2': 'VSV Grouse T2',
    'Финка ЛГБТ': 'Finka LGBT',
    'стационарный одноразовый РП-74': 'stationary disposable RP-74',
    'бритвенно-острый метательный нож': 'razor-sharp throwing knife',
    'метательный нож': 'throwing knife',
    'нож сектантов': 'cultist knife',
    'cаперская лопатка': 'sapper shovel',
    'кувалда': 'sledgehammer',
    'коса': 'scythe',
    'коса греха': 'scythe of sin',
    'святой кинжал греха': 'holy dagger of sin',
    'святой меч греха': 'holy sword of sin',
    'святой меч инквизиции': 'holy inquisition sword',
    'клеймор': 'claymore',
    'Бензопила': 'Chainsaw',
    'бензопила': 'chainsaw',
    'топорик': 'hatchet',
    'секатор': 'pruning shears',
    'маленькая тяпка': 'small hoe',

    # Ammunition
    'магазин 5,45х39 (30)': 'magazine 5.45x39 (30)',

    # Grenades/Explosives
    'граната M84': 'M84 grenade',
    'граната РГД-5': 'RGD-5 grenade',
    'граната Ф1': 'F1 grenade',
    'голубая граната GL-1': 'blue GL-1 grenade',
    'зелёная граната GL-1': 'green GL-1 grenade',
    'оранжевая граната GL-1': 'orange GL-1 grenade',
    'динамитная шашка с фитилем': 'dynamite stick with fuse',
    'динамитная шашка без фитиля': 'dynamite stick without fuse',
    'динамит с двумя шашками': 'dynamite with two sticks',
    'динамит с тремя шашками': 'dynamite with three sticks',
    'динамит с четвермя шашками': 'dynamite with four sticks',
    'взрывчатка FL-2': 'FL-2 explosive',
    'самодельная взрывчатка с таймером': 'homemade timed explosive',
    'растяжка': 'tripwire',
    'Я МИНА': 'I AM MINE',
    'капкан': 'bear trap',
    'взрыв': 'explosion',
    'взрыв (миномёт)': 'explosion (mortar)',
    'вызов миномётного обстрела': 'mortar strike call',
    'вызов интенсивного миномётного обстрела': 'intensive mortar strike call',
    'вызов обстрела 152мм': 'call for 152mm bombardment',
    'вызов обстрела РСЗО': 'MLRS strike call',
    'сигнальная шашка': 'signal flare',
    'Вспышка': 'Flash',

    # Armor/Clothing
    '6Б2 T2': '6B2 T2',
    'АИ-2 T1': 'AI-2 T1',
    'Аномальная куртка T1': 'Anomalous Jacket T1',
    'Болотный плащ T3': 'Swamp Cloak T3',
    'Варяг-4 T4': 'Varyag-4 T4',
    'Витязь T3': 'Vityaz T3',
    'Витязь T3.5': 'Vityaz T3.5',
    'Восток T2': 'Vostok T2',
    'Дождевик T1': 'Raincoat T1',
    'Заря T2': 'Zarya T2',
    'Заря-3 T2': 'Zarya-3 T2',
    'Заря-7 T2': 'Zarya-7 T2',
    'Зенит T3': 'Zenit T3',
    'Искра T2': 'Iskra T2',
    'К. наём сталкера T3': 'Merc. stalker vest T3',
    'Кевларовый плащ T3': 'Kevlar Cloak T3',
    'Кинолог T2': 'Kinolog T2',
    'Кираса T2': 'Cuirass T2',
    'Кожанный плащ T1': 'Leather Cloak T1',
    'Кольчужная куртка T1': 'Chainmail Jacket T1',
    'Корунд (модифицированный)': 'Korund (modified)',
    'Корунд T3': 'Korund T3',
    'ПСЗ-7 T3': 'PSZ-7 T3',
    'Пионер T4': 'Pioneer T4',
    'Плащ СЕВА T3': 'SEVA Cloak T3',
    'Плащ исследователя T1': 'Explorer Cloak T1',
    'РХБЗ T2': 'CBRN T2',
    'Сева T3': 'Seva T3',
    'Сито T1': 'Sito T1',
    'Слик T3': 'Slick T3',
    'СШ-68 T1': 'SSh-68 T1',
    'Степь-4 T3': 'Step-4 T3',
    'Туземец T3': 'Tuzemets T3',
    'Турист T2': 'Tourist T2',
    'Ул. Куртка T1': 'Imp. Jacket T1',
    'Щиток-2 T2': 'Shield-2 T2',
    'Шлем (тест модификаций)': 'Helmet (upgrade test)',
    'оранжевая футболка': 'orange t-shirt',
    'защитные очки': 'safety goggles',
    'гарнитура': 'headset',

    # Backpacks/Bags
    'вещмешок T1': 'duffel bag T1',
    'тактический рюкзак T2': 'tactical backpack T2',
    'армейский рюкзак T3': 'army backpack T3',
    'рюкзак Заря T3': 'Zarya backpack T3',
    'походный рюкзак T3': 'hiking backpack T3',
    'армейский такт. рюкзак T4': 'army tactical backpack T4',
    'проф. рюкзак T4': 'professional backpack T4',
    'синий армейский рюкзак T4': 'blue army backpack T4',
    'подсумок новичка T1': "beginner's pouch T1",
    'поясная сумка ПСЗ T2': 'PSZ belt pouch T2',
    'поясная сумка Заря T2': 'Zarya belt pouch T2',
    'поясная сумка Берилл-5М T2.5': 'Berill-5M belt pouch T2.5',
    'двойная поясная сумка Заря T3.5': 'double Zarya belt pouch T3.5',

    # Medical
    'бинт T1': 'bandage T1',
    'мазь T1': 'ointment T1',
    'наб. ушибов T1': 'bruise kit T1',
    'аптечка CSM T1.5': 'CSM medkit T1.5',
    'йодрадин T1.5': 'iodoradine T1.5',
    'пакет крови T2': 'blood pack T2',
    'армейская апт. T2': 'army medkit T2',
    'научная апт. T3': 'science medkit T3',
    'аптечка Grizzly T3': 'Grizzly medkit T3',
    'пробудофилин T3': 'probudophilin T3',
    'продв. апт. T4': 'advanced medkit T4',
    'аптечка «LAR»': '"LAR" medkit',
    'фуфломицин T4.5': 'fuflomycin T4.5',
    'продвинутая аптечка': 'advanced medkit',
    'база стимпака': 'stimpak base',
    'военный стимулятор': 'military stimulant',
    'научный стимулятор': 'science stimulant',
    'самопальный стимулятор': 'homemade stimulant',
    'стимулятор монолита': 'Monolith stimulant',
    'упаковка йодорадина': 'iodoradine pack',
    'упаковка пробудофилина': 'probudophilin pack',
    'упаковка фуфломицина': 'fuflomycin pack',
    'шприц йодорадина': 'iodoradine syringe',
    'шприц пробудофилина': 'probudophilin syringe',

    # Artifacts
    'Колобок': 'Kolobok',
    'Гроздь': 'Cluster',
    'Выверт': 'Vyvert',
    'Медуза': 'Jellyfish',
    'Пламя': 'Flame',
    'Огненный Шар': 'Fireball',
    'Огненный шар': 'Fireball',
    'Ночная звезда': 'Night Star',
    'Солнце': 'Sun',
    'Плёнка': 'Film',
    'Скала': 'Rock',
    'Грязный снег': 'Dirty Snow',
    'Пожарище': 'Conflagration',
    'Корень': 'Root',
    'Кровь Камня': 'Stone Blood',
    'Глаз': 'Eye',
    'Глаз Плоти': 'Flesh Eye',
    'Слизь': 'Slime',
    'Цитоплазма': 'Cytoplasm',
    'Распятие': 'Crucifix',
    'ЛЭП': 'Power Line',
    'эпицентр': 'epicentre',

    # Food/Ingredients
    'Ломоть мяса': 'Meat slice',
    'сырое мясо кабана': 'raw boar meat',
    'сырое мясо пса': 'raw dog meat',
    'сырое паучье мясо': 'raw spider meat',
    'сырое сало': 'raw lard',
    'стейк из мяса паука': 'spider meat steak',
    'стейк из мяса пса': 'dog meat steak',
    'шашлык из мяса кабана': 'boar meat kebab',
    'приготовленное сало': 'cooked lard',
    'печёный картофель': 'baked potato',
    'бутерброд c колбасой': 'sausage sandwich',
    'буханка хлеба': 'loaf of bread',
    'кусок хлеба': 'piece of bread',
    'кусок колбасы': 'piece of sausage',
    'колбаса «Зыряновская»': '"Zyryanovskaya" sausage',
    'колбаса «Практическая»': '"Prakticheskaya" sausage',
    'каннабис сатива': 'cannabis sativa',
    'бошечки': 'buds',
    'сушенные бошечки': 'dried buds',
    'измельченные бошки': 'ground buds',
    'картофельные очистки': 'potato peels',
    'навоз': 'manure',
    'навозное ведро': 'manure bucket',
    'порох': 'gunpowder',
    'вода': 'water',
    'почва': 'soil',

    # Tools/Items
    'ведро': 'bucket',
    'отвёртка': 'screwdriver',
    'плоскогубцы': 'pliers',
    'циркулярная пила': 'circular saw',
    'факел': 'torch',
    'Лопата для тайников': 'Cache shovel',
    'ботанический справочник': 'botanical guide',
    'старая бумага': 'old paper',
    'кровавая бумага': 'bloody paper',
    'документы': 'documents',
    'флешка': 'flash drive',
    'карта': 'map',
    'зеркало': 'mirror',
    'защитный кейс': 'protective case',
    'защитный кейс T1.5': 'protective case T1.5',
    'оружейный кейс': 'weapon case',
    'ржавый кейс': 'rusty case',
    'кейс': 'case',

    # Containers
    'контейнер артефактов T1': 'artifact container T1',
    'продв. контейнер артефактов T4': 'advanced artifact container T4',
    'контейнер для артефактов': 'artifact container',
    'продвинутый контейнер для артефактов': 'advanced artifact container',
    'хранилище': 'storage',
    'ящик с турелью': 'turret crate',
    'синий шкаф': 'blue locker',

    # Stamps/Documents
    'печать ООН': 'UN stamp',
    'печать агента': 'agent stamp',
    'печать военного': 'military stamp',
    'печать заказчика': 'client stamp',
    'печать наёмника': 'mercenary stamp',
    'печать полевого Командира': 'field commander stamp',
    'печать ученого': 'scientist stamp',
    'Паспорт "Ганза': '"Hanza" Passport',
    'Паспорт Беларуси': 'Belarus Passport',
    'Паспорт Казахстана': 'Kazakhstan Passport',
    'Паспорт России': 'Russia Passport',
    'Паспорт Украины': 'Ukraine Passport',
    'удостоверение СБУ': 'SBU ID card',
    'пропуск в ЧЗО': 'Exclusion Zone pass',
    'пропуск на военную базу': 'military base pass',
    'разрешение на боевое оружие': 'combat weapon permit',
    'охотничья лицензия': 'hunting license',
    'Важная карта': 'Important map',

    # Faction skin sets
    'Набор «Воля»': '"Volya" Set',
    'Набор Аномалиста': 'Anomalist Set',
    'Набор Долга': 'Duty Set',
    'Набор Жаб': 'Frog Set',
    'Набор Завета # aka Zavet': 'Covenant Set',
    'Набор Монолита': 'Monolith Set',
    'Набор ООН #aka UN': 'UN Set',
    'Набор Паломника': 'Pilgrim Set',
    'Набор Поиска': 'Search Set',
    'Набор Полиции': 'Police Set',
    'Набор Ренегата': 'Renegade Set',
    'Набор Серафима': 'Seraphim Set',
    'Набор Чистого Неба #aka CN': 'Clear Sky Set',
    'Набор бандита': 'Bandit Set',
    'Набор военных': 'Military Set',
    'Набор дял скинов': 'Skin Set',
    'Набор наёмников # aka Merc': 'Mercenary Set',
    'Набор нейтралов': 'Neutral Set',
    'Набор отступников': 'Renegade Set',
    'Набор проекта': 'Project Set',

    # Trade categories
    'Автоматы (NATO)': 'Assault Rifles (NATO)',
    'Автоматы (СССР)': 'Assault Rifles (USSR)',
    'Аномалии': 'Anomalies',
    'Аптечки': 'Medkits',
    'Боеприпасы': 'Ammunition',
    'Боеприпасы I': 'Ammunition I',
    'Боеприпасы II': 'Ammunition II',
    'Боеприпасы III': 'Ammunition III',
    'Боеприпасы IV': 'Ammunition IV',
    'Боеприпасы V': 'Ammunition V',
    'Бронежилеты': 'Body Armor',
    'Броня': 'Armor',
    'Винтовочные': 'Rifle Ammo',
    'Детекторы аномалий': 'Anomaly Detectors',
    'Детекторы артефактов': 'Artifact Detectors',
    'Дробовики': 'Shotguns',
    'Дробовые': 'Shotgun Ammo',
    'Инструменты': 'Tools',
    'Кейсы': 'Cases',
    'Контейнеры артефактов': 'Artifact Containers',
    'Магазины': 'Magazines',
    'Медицина': 'Medicine',
    'Металлоискатели': 'Metal Detectors',
    'Оружие': 'Weapons',
    'ПП и PDW': 'SMGs and PDWs',
    'Пистолетные': 'Pistol Ammo',
    'Пистолеты и револьверы': 'Pistols and Revolvers',
    'Плащи': 'Cloaks',
    'Подсумки': 'Pouches',
    'Препараты': 'Drugs',
    'Прочее': 'Miscellaneous',
    'Рюкзаки': 'Backpacks',
    'Снайперки и DMR': 'Snipers and DMR',
    'Снаряжение': 'Equipment',
    'Специальные': 'Special',
    'Спецкостюмы': 'Special Suits',
    'Удочки': 'Fishing Rods',
    'Шлемы': 'Helmets',
    'Улучшения': 'Upgrades',

    # Upgrades
    'Аномальная пропитка': 'Anomalous Impregnation',
    'Вставка бронепластины': 'Armor Plate Insert',
    'Дополнительный слой кевлара': 'Additional Kevlar Layer',
    'Керамическая бронеплита': 'Ceramic Armor Plate',
    'Подгонка ствольной группы': 'Barrel Group Fitting',
    'Полировка затвора': 'Bolt Polishing',
    'Стабилизатор отдачи': 'Recoil Stabilizer',
    'Усиленный газоотвод': 'Enhanced Gas Block',
    'Усиленный кевлар': 'Reinforced Kevlar',
    'Базовая медицина': 'Basic Medicine',
    'Базовое': 'Basic',
    'Продвинутое': 'Advanced',
    'Конвертация материалов': 'Material Conversion',
    'Разбор': 'Disassembly',
    'Разбор артефактов': 'Artifact Disassembly',

    # Levels
    'Уровень I': 'Level I',
    'Уровень II': 'Level II',
    'Уровень III': 'Level III',
    'Уровень IV': 'Level IV',
    'Уровень V': 'Level V',

    # Blueprints/Recipes
    'Рецепт проффесионального рюкзака (Т6)': 'Professional backpack recipe (T6)',
    'Рецепт проффесионаольного зеленого рюкзака (Т5)': 'Professional green backpack recipe (T5)',
    'Рецепт проффесионаольного серого рюкзака (Т5)': 'Professional gray backpack recipe (T5)',

    # Misc items
    'Балон для огнемёта': 'Flamethrower canister',
    'Деревенский туалет': 'Village toilet',
    'грязный унитаз': 'dirty toilet',
    'деревянный стул': 'wooden chair',
    'старый деревянный стул': 'old wooden chair',
    'диван': 'sofa',
    'старая лампа': 'old lamp',
    'донат': 'donation',
    'Мусорный пластиковый спавнер': 'Trash plastic spawner',
    'химические отходы': 'chemical waste',
    'шеврон монолита': 'Monolith chevron',
    'Рупор Долга': 'Duty Loudspeaker',
    'доска контрактов': 'contract board',
    'светящаяся уведомляшка для греха': 'glowing notification for sin',
    'спавнер военного снабжения': 'military supply spawner',
    'спавнер снабжения ООН': 'UN supply spawner',
    'Долг Детекшн Сталкеров': 'Duty Stalker Detection',
    'Долг Пропоганда': 'Duty Propaganda',
    'База Бомбы': 'Bomb Base',

    # Tiles
    'Жёлтая трава Stalker': 'Yellow grass Stalker',
    'Зелёная трава Stalker': 'Green grass Stalker',
    'Красная трава Stalker': 'Red grass Stalker',
    'тёмный щебень Stalker': 'dark gravel Stalker',
    'щебень Stalker': 'gravel Stalker',
    'дорога Stalker': 'road Stalker',
    'песок Stalker': 'sand Stalker',
    'Небольшой базальтовый куб': 'Small basalt cube',

    # Quest/Contract descriptions
    'Добудь Выверт': 'Obtain Vyvert',
    'Добудь Медузу': 'Obtain Jellyfish',
    'Вернуть ящик с болота': 'Retrieve crate from swamp',
    'Доставить ящик к покупателю': 'Deliver crate to buyer',
    'Хвосты слепых псов': 'Blind dog tails',

    # Descriptions
    'Американская светозвуковая граната. Специальное средство несмертельного действия, оказывающие на человека светозвуковое и осколочное воздействие.':
        'American flashbang grenade. A special non-lethal device that affects a person with flash-bang and fragmentation effects.',
    'Взрывчатка делает БУМ!': 'Explosives go BOOM!',
    'Вы видите отражение себя, а вы стильный сталкер...': 'You see your reflection, and you are a stylish stalker...',
    'Вызывает миномётный огонь дымовыми боеприпасами.': 'Calls mortar fire with smoke ammunition.',
    'Глаза Плоти для борща. Вкусно пальчики оближешь.': "Flesh eyes for borscht. Delicious, you'll lick your fingers.",
    'Динамит с двумя шашками, привязанными с помощью изоленты, представляет собой усовершенствованную версию классического динамита, которая повышает его разрушительную мощь и эффективность в боевых условиях.':
        'Dynamite with two sticks tied with electrical tape, an improved version of classic dynamite that increases its destructive power and combat effectiveness.',
    'Динамитная шашка с фитилем представляет собой компактное оружие, способное принести разрушительный эффект.':
        'A dynamite stick with a fuse is a compact weapon capable of devastating effect.',
    'Для использования вставьте ключ шифрования.': 'Insert an encryption key to use.',
    'Достал Леший... отобрал ведро грибов!!!': 'That damn Leshy... stole a bucket of mushrooms!!!',
    'Ещё более разрушительная и мощная версия взрывного оружия, которое может быть использовано для проведения диверсионных операций.':
        'An even more destructive and powerful version of explosive weaponry that can be used for sabotage operations.',
    'Запоминающее устройство, использующее в качестве носителя флеш-память, и подключаемое к компьютеру или иному считывающему устройству по интерфейсу. Может стоить немалых денег.':
        'A storage device using flash memory, connected to a computer or other reading device via interface. Can be worth a lot of money.',
    'Зафиксированная на материальном носителе информация в виде текста, может представлять из себя ценный предмет.':
        'Information recorded on a physical medium in text form, may be a valuable item.',
    'Зафиксированная на материальном носителе информация в виде текста, может представлять из себя ценный предмет. На папке имеется надпись СЕКРЕТНО.':
        'Information recorded on a physical medium in text form, may be a valuable item. The folder is marked SECRET.',
    'Защитят глаза от пыли и мелких осколков, а также от ярких вспышек.':
        'Protects eyes from dust and small fragments, as well as from bright flashes.',
    'Какая то тварь завелась не понятная': 'Some unknown creature has appeared',
    'Калибровочная карта потерялась, а мне срочно требуеться... Акуратнее там псевдо собаки.':
        'The calibration card got lost, and I urgently need it... Watch out for pseudodogs.',
    'Клиент так и не забрал свой груз, а теперь там ошивается стая матерых кабанов. Отправляйся на место, разберись с кабанами и притащи ящик обратно к автомату.':
        "The client never picked up their cargo, and now a pack of mature boars is lurking there. Head to the location, deal with the boars and bring the crate back to the vending machine.",
    'Мутная вода Зоны.': 'Murky water of the Zone.',
    'Не стоит, садится на него.. я думаю.': "I don't think you should sit on it.",
    'Нужен выверт для мази. А то спина болит у уважаемого Жабы.': "Need a Vyvert for the ointment. The respected Frog's back hurts.",
    'Нужна медуза для ожерелья. Зарю не забудь одеть.': "Need a Jellyfish for the necklace. Don't forget to put on the Zarya.",
    'Нужно донести ящик к покупателю. Не задавай вопросов.': "Need to deliver the crate to the buyer. Don't ask questions.",
    'ОНА СОЖРАЛА ДАНИЛУ, УБЕЙ ТВАРЬ И ПРИНЕСИ ДОКАЗАТЕЛЬСТВА.': 'SHE ATE DANILA, KILL THE CREATURE AND BRING PROOF.',
    'Обычная футболка для жизни, покрашена в оранжевый цвет.': 'A regular t-shirt for everyday life, dyed orange.',
    'Одна нога здесь, другая там.': 'One foot here, another there.',
    'Она же "Лимонка", не ультимативное, но крайне мощное оружие, имеющее основным поражающим фактором осколки.':
        'Also known as "Limonka", not an ultimate weapon, but extremely powerful with fragmentation as its primary damage factor.',
    'Очень опасен, не идите в одиночку.': 'Very dangerous, do not go alone.',
    'Небольшой балон с топливом для огнемёта': 'A small canister with fuel for a flamethrower',
    'Представляет из себя коробку, используемую для хранения и транспортировки ценных вещей.':
        'A box used for storing and transporting valuable items.',
    'Приятная на ощупь бумажная игральная карта выпуска NanoTrasen.':
        'A pleasant-to-touch paper playing card manufactured by NanoTrasen.',
    'Приятный на ощупь небольшой куб из базальта.': 'A pleasant-to-touch small basalt cube.',
    'Продвинутый медицинский набор, разработанный исключительно для выведения токсинов и восстановления ЦНС.':
        'Advanced medical kit designed exclusively for toxin removal and CNS restoration.',
    'Самая последняя версия качественной взрывчатки на таймере и на датчике управления, поражает врага мощной взрывной волной.':
        'The latest version of quality explosives on a timer and control sensor, hitting the enemy with a powerful blast wave.',
    'Самодельная взрывчатка на таймере, сложная конструкция ведь вам пришлось переделать детектор аномалий в таймер. Осторожно, он не настроен корректно.':
        'Homemade timed explosive, a complex construction since you had to convert an anomaly detector into a timer. Careful, it is not calibrated correctly.',
    'Смертельно опасен... в одиночку не идите.': 'Deadly dangerous... do not go alone.',
    'Совершенный медицинский набор «LAR», разработанный специально для работы в тяжёлых условиях Зоны. Предоставляет полный комплекс помощи при всех типах аномального поражения, а также поражения ЦНС и внутренних органов. Не выводит радиацию.':
        '"LAR" advanced medical kit, designed specifically for harsh Zone conditions. Provides a full range of treatment for all types of anomalous damage, as well as CNS and internal organ damage. Does not remove radiation.',
    'Советская наступательная ручная граната, относится к противопехотным осколочным ручным гранатам дистанционного действия наступательного типа.':
        'Soviet offensive hand grenade, classified as an anti-personnel fragmentation hand grenade of the offensive type.',
    'То что может взоваться если её попытаться сломать.': 'Something that can explode if you try to break it.',
    'Труба в форме усечённого конуса, предназначенная для направленной передачи звука.':
        'A truncated cone-shaped tube designed for directed sound transmission.',
    'Убей кабана': 'Kill the boar',
    'Убей медведэ... он сожрал мой мед': 'Kill the bear... it ate my honey',
    'Упаковка таблеток йодорадина, каждая таблетка весит 10 грамм. Поможет вывести радионуклиды из организма. Принимать до 30 грамм за раз!':
        'A pack of iodoradine tablets, each tablet weighs 10 grams. Helps remove radionuclides from the body. Take up to 30 grams at a time!',
    'Упаковка таблеток пробудофилина, каждая таблетка весит 10 грамм. Поможет возобновить кровообращение и дыхание. Принимать до 20 грамм за раз!':
        'A pack of probudophilin tablets, each tablet weighs 10 grams. Helps restore circulation and breathing. Take up to 20 grams at a time!',
    'Упаковка таблеток фуфломицина, каждая таблетка весит 10 грамм. Поможет уверить себя в своей неуязвимости для пси-волн. Принимать до 10 грамм за раз!':
        'A pack of fuflomycin tablets, each tablet weighs 10 grams. Helps convince yourself of your invulnerability to psi-waves. Take up to 10 grams at a time!',
    'Хвосты собак на лекарство.': 'Dog tails for medicine.',
    'Хозяйственный металлический шкафчик неплохо сохранился для своих лет, а также достаточно вместителен.':
        'A household metal locker that has survived well for its age and is quite spacious.',
    'Шеврон сорванный с бойца монолита.': 'A chevron torn from a Monolith fighter.',
    'Яркие химические отходы Зоны.': 'Bright chemical waste of the Zone.',
    'как отсрочка смерти.': 'like a stay of death.',
    'доска с заказами на поставки, охоту, ремонт и особые цели':
        'a board with orders for deliveries, hunting, repairs and special targets',
    'Любой рюкзак (т4) - 1шт, шкура Т2 - 5шт, набор базовых ниток T1 - 2шт, артефакт выверт - 3шт.':
        'Any backpack (T4) - 1pc, T2 hide - 5pcs, basic thread set T1 - 2pcs, Vyvert artifact - 3pcs.',
    'Любой рюкзак (т5) - 1шт, крепкая шкура T3 - 1шт, набор базовых ниток T1 - 2шт, артефакт Амёба - 2шт.':
        'Any backpack (T5) - 1pc, T3 sturdy hide - 1pc, basic thread set T1 - 2pcs, Amoeba artifact - 2pcs.',

    # Spawner-related
    'тайник спавнер невидимый (25 %)': 'invisible cache spawner (25%)',
    'тайник спавнер невидимый (10 %)': 'invisible cache spawner (10%)',
    'тайник спавнер невидимый (5 %)': 'invisible cache spawner (5%)',
    'тайник спавнер невидимый (1 %)': 'invisible cache spawner (1%)',
    'тайник спавнер трава (10 %)': 'grass cache spawner (10%)',
    'тайник спавнер трава (5 %)': 'grass cache spawner (5%)',
    'тайник спавнер трава (1 %)': 'grass cache spawner (1%)',
    'куст (1-4) (50%)': 'bush (1-4) (50%)',

    # PDA names
    'мина ПФМ-1 "Лепесток': 'PFM-1 "Petal" mine',
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
                entity_id = m.group(1)[4:]
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


# Pattern-based translations
TRIGGER_WORDS = {
    'базовый триггер': 'basic trigger',
    'триггер кабанов': 'boar trigger',
    'триггер матёрых кабанов': 'mature boar trigger',
    'триггер старых кабанов': 'old boar trigger',
    'триггер псевдопёс': 'pseudodog trigger',
    'триггер оракл': 'oracle trigger',
    'триггер псевдогигант': 'pseudogiant trigger',
    'триггер бюрер': 'burer trigger',
    'триггер голиаф': 'goliath trigger',
    'триггер зомби': 'zombie trigger',
    'триггер крыс': 'rat trigger',
    'триггер кровосос': 'bloodsucker trigger',
    'триггер леший': 'leshy trigger',
    'триггер плоть': 'flesh trigger',
    'триггер нейтрал плоть': 'neutral flesh trigger',
    'триггер паук': 'spider trigger',
    'триггер куст': 'bush trigger',
    'триггер снорк': 'snork trigger',
    'триггер тушкан': 'tushkan trigger',
    'триггер слепых псов': 'blind dog trigger',
    'триггер гоша': 'Gosha trigger',
    'триггер контролёр': 'controller trigger',
    'триггер пси-пёс': 'psi-dog trigger',
    'триггер редкий моб': 'rare mob trigger',
    'триггер рогоносец': 'horned trigger',
    'триггер босс': 'boss trigger',
    'триггер спавна агрессивного уёбка': 'aggressive mutant spawn trigger',
    'триггер спавна монолитовцев': 'Monolith spawn trigger',
    'триггер спавна патрульного Долга': 'Duty patrol spawn trigger',
    'триггер спавна патрульного бандитов': 'Bandit patrol spawn trigger',
    'триггер спавна патрульного военсталов': 'Military Stalker patrol spawn trigger',
    'триггер спавна патрульного нейтралов': 'Neutral patrol spawn trigger',
    'триггер спавна патрульного свободы': 'Freedom patrol spawn trigger',
    'триггер спавна патрульного серафимов': 'Seraphim patrol spawn trigger',
    'триггер спавна патрульного чистого неба': 'Clear Sky patrol spawn trigger',
    'триггер спавна постового СОП': 'SOP sentry spawn trigger',
    'триггер спавна постового сталкера': 'Stalker sentry spawn trigger',
    'триггер людей для греха': 'people trigger for sin',
    'триггер Скрытого Торговца': 'Hidden Trader trigger',
    'Триггер Светлячков (3-7) (100%)': 'Firefly Trigger (3-7) (100%)',
    'Триггер грузовиков (20%)': 'Truck Trigger (20%)',
    'Триггер кейса с чертежами (10%)': 'Blueprint Case Trigger (10%)',
    'Триггер кислотных Т3 стали и стекла (20%)': 'Acid T3 Steel and Glass Trigger (20%)',
    'Триггер контролёра с собаками (100%)': 'Controller with Dogs Trigger (100%)',
    'Триггер машин (20%)': 'Vehicle Trigger (20%)',
    'Триггер стаи пауков Лапачей (100%)': 'Lapach Spider Pack Trigger (100%)',
    'Триггер стаи слепых псов (100%)': 'Blind Dog Pack Trigger (100%)',
}

DOLL_TRANSLATIONS = {
    'кукла Долговец (переименуй меня)': 'Duty doll (rename me)',
    'кукла Командир Ополчение (переименуй меня)': 'Militia Commander doll (rename me)',
    'кукла Монолита (переименуй меня)': 'Monolith doll (rename me)',
    'кукла ОКСОП (переименуй меня)': 'OKSOP doll (rename me)',
    'кукла ОКСОП глава (переименуй меня)': 'OKSOP leader doll (rename me)',
    'кукла ООНовец (переименуй меня)': 'UN doll (rename me)',
    'кукла Ополчение (переименуй меня)': 'Militia doll (rename me)',
    'кукла СБУ (переименуй меня)': 'SBU doll (rename me)',
    'кукла Сталкера (переименуй меня)': 'Stalker doll (rename me)',
    'кукла аномалист (переименуй меня)': 'anomalist doll (rename me)',
    'кукла бандит (переименуй меня)': 'bandit doll (rename me)',
    'кукла греховец (переименуй меня)': 'sinner doll (rename me)',
    'кукла декан (переименуй меня)': 'dean doll (rename me)',
    'кукла жаба (переименуй меня)': 'frog doll (rename me)',
    'кукла заветовец (переименуй меня)': 'covenant doll (rename me)',
    'кукла наёмник (переименуй меня)': 'mercenary doll (rename me)',
    'кукла последний день (переименуй меня)': 'last day doll (rename me)',
    'кукла ректор (переименуй меня)': 'rector doll (rename me)',
    'кукла ренегат (переименуй меня)': 'renegade doll (rename me)',
    'кукла свободовец (переименуй меня)': 'Freedom doll (rename me)',
    'кукла серафим (переименуй меня)': 'Seraphim doll (rename me)',
    'кукла учёный (переименуй меня)': 'scientist doll (rename me)',
    'кукла циркач (переименуй меня)': 'circus doll (rename me)',
    'кукла член Чистого Неба (переименуй меня)': 'Clear Sky member doll (rename me)',
    'кукла член проекта (переименуй меня)': 'project member doll (rename me)',
}


def translate_text(text, ftl_lookup, entity_id, field_type):
    """Translate Russian text to English."""
    # Try FTL lookup first
    if entity_id and entity_id in ftl_lookup:
        ftl_entry = ftl_lookup[entity_id]
        if field_type == 'name' and ftl_entry.get('name') and not has_cyrillic(ftl_entry['name']):
            return ftl_entry['name']
        if field_type == 'desc' and ftl_entry.get('desc') and not has_cyrillic(ftl_entry['desc']):
            return ftl_entry['desc']

    # Strip YAML quotes
    clean = text.strip().strip('"').strip("'")

    # Try direct dictionary
    if clean in TRANSLATIONS:
        return TRANSLATIONS[clean]

    # Try doll translations
    if clean in DOLL_TRANSLATIONS:
        return DOLL_TRANSLATIONS[clean]

    # Try trigger patterns
    if clean in TRIGGER_WORDS:
        return TRIGGER_WORDS[clean]

    # Pattern: replace Cyrillic м (meters) → m at end of numbers
    result = re.sub(r'(\d+)м', r'\1m', clean)

    # Pattern: trigger with params
    for ru, en in sorted(TRIGGER_WORDS.items(), key=lambda x: -len(x[0])):
        if result.startswith(ru.replace('м', 'm')):
            rest = result[len(ru.replace('м', 'm')):]
            result = en + rest
            break
        if clean.startswith(ru):
            rest = clean[len(ru):]
            rest = re.sub(r'(\d+)м', r'\1m', rest)
            result = en + rest
            break

    if not has_cyrillic(result):
        return result

    # Try case-insensitive match
    clean_lower = clean.lower()
    for ru, en in TRANSLATIONS.items():
        if ru.lower() == clean_lower:
            return en

    return None


def quote_yaml_value(value):
    """Add YAML quoting if needed."""
    if value.startswith('"') and not value.endswith('"'):
        return "'" + value + "'"
    if value.startswith("'") and not value.endswith("'"):
        return '"' + value + '"'
    if ': ' in value:
        return '"' + value.replace('"', '\\"') + '"'
    if value.startswith('{') or value.startswith('['):
        return '"' + value + '"'
    return value


def process_yaml_file(filepath, ftl_lookup):
    """Process a YAML file, translating Russian name/description."""
    with open(filepath) as f:
        lines = f.readlines()

    modified = False
    changes = 0
    current_id = None
    current_type = None
    new_lines = []

    for line in lines:
        stripped = line.strip()

        if stripped.startswith('- type:'):
            current_type = stripped.split(':', 1)[1].strip()
            current_id = None
        elif stripped.startswith('id:') and current_type:
            current_id = stripped.split(':', 1)[1].strip()

        is_name = stripped.startswith('name:') and has_cyrillic(stripped)
        is_desc = stripped.startswith('description:') and has_cyrillic(stripped)

        if is_name or is_desc:
            field_type = 'name' if is_name else 'desc'
            field_key = 'name' if is_name else 'description'
            value_part = stripped.split(':', 1)[1].strip()

            translation = translate_text(value_part, ftl_lookup, current_id, field_type)

            if translation:
                indent = line[:len(line) - len(line.lstrip())]
                quoted = quote_yaml_value(translation)
                new_line = f"{indent}{field_key}: {quoted}\n"
                new_lines.append(new_line)
                modified = True
                changes += 1
                continue

        new_lines.append(line)

    if modified:
        # Add marker if not already present
        has_marker = any('# Zona14:' in l for l in new_lines)
        if not has_marker:
            new_lines.insert(0, "# Zona14: translated Russian names/descriptions to English\n")

        with open(filepath, 'w') as f:
            f.writelines(new_lines)

    return modified, changes


def main():
    ftl_lookup = build_ftl_lookup()
    print(f"FTL lookup: {len(ftl_lookup)} entities")

    base_paths = sys.argv[1:] if len(sys.argv) > 1 else ['Resources/Prototypes/_Stalker']

    total_files = 0
    total_changes = 0

    for base_path in base_paths:
        print(f"\n=== Processing {base_path} ===")
        for filepath in sorted(glob.glob(f'{base_path}/**/*.yml', recursive=True)):
            modified, changes = process_yaml_file(filepath, ftl_lookup)
            if modified:
                total_files += 1
                total_changes += changes
                print(f"  {filepath}: {changes} translations")

    print(f"\n=== TOTAL: {total_files} files, {total_changes} changes ===")

    # Count remaining
    remaining = 0
    remaining_items = []
    for base_path in base_paths:
        for filepath in sorted(glob.glob(f'{base_path}/**/*.yml', recursive=True)):
            with open(filepath) as f:
                for line in f:
                    stripped = line.strip()
                    if (stripped.startswith('name:') or stripped.startswith('description:')) and has_cyrillic(stripped):
                        remaining += 1
                        val = stripped.split(':', 1)[1].strip()[:60]
                        if len(remaining_items) < 30:
                            remaining_items.append(val)

    print(f"\nRemaining Russian: {remaining}")
    if remaining_items:
        for item in remaining_items:
            print(f"  {item}")


if __name__ == '__main__':
    main()

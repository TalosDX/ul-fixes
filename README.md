# UL Fixes — Undead Legacy quality-of-life fixes

Small mods for **7 Days to Die V2.6** with **Undead Legacy 2.7.40**. Each one is independent — install only what you want.

| Mod | What it does | Who installs it |
|---|---|---|
| [Sleeping Trigger Zombies](#sleeping-trigger-zombies) | POI "trigger" zombies that pop in awake and run at you | Host / server only |
| [UL Scope Fixes](#ul-scope-fixes) | UL picture-in-picture scopes: dark at night, middle-click zoom, zombies vanishing at max zoom, weapon too high | Every player (client-side) |
| [UL Party Research Craft](#ul-party-research-craft) | Recipes researched by any party member are craftable by everyone in the party | Every player (client-side) |
| [Keep Action Skills on Death](#keep-action-skills-on-death) | Removes only the action-skill loss from UL's death penalty | Host / server |
| [Restore tvCRT_PlayerOff](#restore-tvcrt_playeroff) | Lets the FNS "Exham Priory" POIs load with UL | Host and every client |
| [Sniper Ammo Books for .50 Cal](#sniper-ammo-books-for-50-cal) | Sniper book "AP/HP Ammo" perks also boost .50 cal rounds | Host / server (clients optional, for the tooltip) |
| [Honest Tooltips](#honest-tooltips) | Tooltips and descriptions that state wrong numbers (armor noise, durations, books, skills, mods) | Host and every client |

Русская версия — [ниже](#русский).

## Installation

1. Download the repository (**Code → Download ZIP**) or a release archive.
2. Copy the folder(s) you want from `Mods/` into your game's `Mods` folder
   (e.g. `...\steamapps\common\7 Days To Die\Mods\`).
3. Start the game. Undead Legacy ships BepInEx, so nothing else is needed (EAC must be off, as for UL itself).

All mods except Restore tvCRT store nothing in your save —
you can add or remove them at any time. Restore tvCRT is the exception (see its section).

---

## Sleeping Trigger Zombies

`Mods/zzzUL_SleepingTriggers`

Many POIs have sleeper volumes of type **Trigger**: they stay empty until you open a certain
door, grab loot or step into an invisible trigger volume. Then the zombies spawn and, one second later,
the game force-wakes them all and sets you as their target. You can never clear them in advance.

What the mod changes:

- **Trigger volumes spawn on approach** (≈8 blocks), like every other volume — as normal sleepers you can sneak up on.
- **A trigger no longer force-wakes zombies.** Instead the already spawned ones get the vanilla
  stealth check (the same one used when you walk into an "Attack" volume):
  - standing — you are always detected;
  - crouching — detected only within `lerp(3, 15 m, LightMultiplier)`: no own light source and stealth
    perks (Stealth skill tree, Sneaking action skill, Recon class, Night Stalker book, armor mod) shrink it to ~3–5 m;
  - detected zombies wake up and attack, the others become light sleepers.
  So ambushes in closets and sealed rooms still jump out on a noisy player.
- **Volumes the trigger spawns for the first time** (you never got close to them) appear asleep.
- **Sirens / alarms** (POI volume scripts playing `alarm`, `siren`, `buzzer`, `security` sounds — prison,
  army camp, hotel, bunker, factory, skyscrapers…): every zombie of the triggered volumes wakes with
  `WakeChance` (default 50 %) **without** a target on you; the rest become light sleepers.

Config: `BepInEx/config/talos.sleepingtriggers.cfg` — `WakeChance`, `SoundKeywords`.
Log: every trigger writes `Trigger by …` / `Siren by …` to `BepInEx/LogOutput.log`.

Multiplayer: spawning is server logic, install on the host (P2P) or server only. Clients don't need it.

---

## UL Scope Fixes

`Mods/zzzUL_ScopeExposureFix`

Undead Legacy renders optics with a separate picture-in-picture camera. This mod fixes four issues with it:

1. **Scope is much darker at night than the normal view.** UL adds post-processing to the scope camera but
   never connects it to the game's volumes, so the scope gets no auto exposure (eye adaptation) and no
   brightness/color grading. The mod gives it the player camera's volumes.
2. **Middle mouse button zoomed the whole screen, not the scope.** It now steps the scope magnification with
   UL's own rule (min → −5° FOV per press → max → back to min). Fixed-magnification scopes ignore it.
3. **Zombie disappears at maximum magnification** and comes back when you zoom out. UL's occlusion override
   checks one renderer pivot against the scope frustum with a margin that ignores the FOV; at 8–16× the pivot
   falls outside and the zombie is hidden. The margin now scales with magnification (no change at normal FOV).
4. **Per-weapon hip position.** Some weapons sit too high when not aiming. The mod lowers the listed weapons
   only while you are *not* aiming, so scope alignment is untouched. Default list: Ranger Rifle (M1A),
   lowered 6 cm. Tune in game with the console (F1), holding the weapon:
   - `fpvoffset` — show the offset of the held weapon
   - `fpvoffset 0 -0.08 0` — set it (metres, y < 0 = lower), saved to `fpv_offsets.txt`
   - `fpvoffset reset` — remove it

Covers every UL PIP scope: small (4×), medium (4–8×), large (8–16×) scope mods and the built-in scopes of the AUG
and M202. Your NVG keeps working through the scope (it is a full-screen effect).

Not a bug: vanilla fades zombies out beyond 90 m (120 m while aiming), so even a 16× scope shows nothing farther.

Each fix is patched separately; if a UL update changes the code, only that fix disables itself with a warning in the log.

---

## UL Party Research Craft

`Mods/zzzULPartyResearch`

Undead Legacy checks recipe research only for the player who crafts. UL's research screen already knows what your
party members have researched (`ULM_Research.KnownByPartyList`), but crafting ignores it. This mod adds a postfix to
UL's recipe gate (`ULM_Recipe.IsUnlockedByReseachOrDefault`): if the recipe is locked for you but researched by a
party member, it becomes craftable while you are grouped.

- Covers the recipe gate only (workbenches, backpack crafting). Block upgrades (e.g. wood frames) still check your own research.
- Everything is found by reflection; if a UL update renames something, the mod logs `missing required members; not patching`
  and does nothing.
- Client-side: each player who wants it installs it.

## Keep Action Skills on Death

`Mods/zzzUL_KeepActionSkills` (XML only)

Removes just the effect group in `buffDeathFoodDrinkAdjust` that applies `actionSkillDeathPenalty` (UL's action-skill
loss on Moderate/Standard death penalty). XP loss, Near Death Trauma and the Standard respawn stat reduction stay.

## Restore tvCRT_PlayerOff

`Mods/zzUL_RestoreTvCRT` (XML only)

Undead Legacy removes the `tvCRT_PlayerOff` block. The POI pack *Vanilla Block POIs by FNS* uses it in
`vb_FNS_Exham_Priory`, so those POIs fail to load ("Could not load prefab"). The mod re-adds the block as UL's own
CRT TV (not craftable). Install on the host and on every client. **Do not remove it once the POIs have generated**
in your world — the placed blocks would become unknown.

## Sniper Ammo Books for .50 Cal

`Mods/zzzUL_Sniper50CalBooks` (XML only)

The Sniper book perks 6 and 7 (+10% entity damage for AP / HP ammo) are wired only to 7.92mm rounds, so the M82 (.50 cal)
gets nothing from them. The mod appends the same effect groups to `ulmAmmo50CalRoundAP` / `ulmAmmo50CalRoundHP` and
updates the book tooltips (English, Russian). Stores nothing in the save.

## Honest Tooltips

`Mods/zzzUL_TooltipFixes` (XML only, gameplay unchanged)

Undead Legacy 2.7.40 tooltips and descriptions checked against the actual effects; only the text/display is changed:

- **Armor**: noise and stamina penalty were never shown (UL removed the display values). Medium armor: +10% noise and -5.6%
  running stamina regen per piece, heavy: +20% / -9%. Now visible on all 108 affected pieces.
  Light armor has no noise or stamina penalty, so it shows none.
- **Character window**: new "Noise" row under jump height with your current noise multiplier (100% = normal),
  including armor, mods and sneak perks (crouch bonuses count while crouching).
- **Character window**: new "Stamina while running" row (per second, as the game computes it while you sprint):
  armor penalties, armor perks, drugs and buffs included. Needs the bundled `HonestTooltips.dll`
  (native mod API, no BepInEx; EAC off, as Undead Legacy already requires).
- **Consumable durations**: the tooltip prints the raw number with " min", but some values were in seconds
  (jar teas/coffee/beer showed "360 min") or stale (several candies said 5 or 10 min for a 6 min buff). All set to the real
  buff duration in minutes.
- **Candy and drinks**, hidden or wrong lines:
  - Eye Kandy: +5 / +10% loot stage was never shown (name mismatch + missing translation);
  - Atom Junkies: +50% explosive block damage added;
  - Steroids and Fort Bites: passive thirst +450%, Recog: +720% (shown now);
  - Jail Breakers: lockpick durability +200% and "lockpicks don't break";
  - Hackers / Rock Busters: -5% harvesting XP; Mega Crush: +120% water used per stamina;
  - Moonshine and Painkillers no longer show damage mitigation that UL disabled; Fort Bites 40% (showed 50%);
  - rotting flesh dysentery risk 15% (showed 12%); coffee jar shows its cold resistance;
  - plastic-bottle beer, coffee and mineral water now use the proper tooltip (their buffs were hidden).
- Knives, machetes and bows: the sneak attack bonus (x2 / x4) was hidden, now shown.
- Mods: AR/MG damage gloves +5–50% (showed 4.5–45%), broadcast radio +1–9 m (showed 0.1–9); now shown: Improved Sneaking
  noise / crouch speed / enemy search duration, cooling mesh and insulated liner fire+electric damage resist, charge extender
  max charges, supercharger engine torque, muzzle brake horizontal recoil and handling; Stealth Boots got their tooltip back.
- Moonshine heals 20 HP/s for ~60 s (showed +600).
- Miner helmet used a mod tooltip without armor stats; missing translations for loot stage, wetness and fertility added.
- Magazine Extender shows the real +30–60%; the seed-loot helmet affix shows its real value (+200–2000%, as configured by UL).
- Books: Fireman's Almanac 1 is +2 axe damage (not +10%), Sniper 4 is 10% faster reload (not 15%).
- UL skill tree: Assault class mining tools +5% (not 10%), Shotgun Handling +10%/level (not 5%), Club combo +100% at every level
  (levels 2–3 add nothing), Medic EXP +20%/level (not 25%), Hold Breath no longer promises +40 s underwater (not implemented).
- Action skills: Blades text listed attack speed and meat harvesting that don't exist (it is damage, block damage,
  dismemberment up to 25% and head damage up to 50%); Athletics is up to 25% (not 20%).
- Russian texts: mods with quality-scaled bonuses showed fixed old numbers (Hunter, Diamond Tip, Fireman's Axe, Structural Brace,
  Tempered Blade, Reserve Fuel Tank, Bellows, gloves damage); crop seeds no longer promise a 50% seed return.

Stores nothing in the save. Localization and tooltips are client-side, so install on the host and every client.

---

## Building from source

Requires the .NET SDK. The projects reference the game's DLLs; pass your game folder if it differs:

```
dotnet build src/SleepingTriggers -c Release -p:G="D:\SteamLibrary\steamapps\common\7 Days To Die"
dotnet build src/ScopeFixes -c Release -p:G="D:\SteamLibrary\steamapps\common\7 Days To Die"
```

Tested on 7 Days to Die V2.6 (b14) + Undead Legacy 2.7.40, P2P co-op.

License: [MIT](LICENSE).

---

## Русский

Небольшие моды для **7 Days to Die V2.6** с **Undead Legacy 2.7.40**. Каждый независим — ставь только нужные.

| Мод | Что исправляет | Кому ставить |
|---|---|---|
| Sleeping Trigger Zombies | зомби из триггер-зон, которые появляются сразу проснувшимися и бегут на тебя | только хосту / серверу |
| UL Scope Fixes | прицелы UL: тёмные ночью, зум на колесико, пропадание зомби на макс. кратности, слишком высоко поднятое оружие | каждому игроку |
| UL Party Research Craft | рецепты, исследованные любым членом группы, может крафтить вся группа | каждому игроку |
| Keep Action Skills on Death | убирает из штрафа за смерть UL только потерю экшен-навыков | хосту / серверу |
| Restore tvCRT_PlayerOff | позволяет загружаться POI «Exham Priory» из пака FNS | хосту и всем клиентам |
| Sniper Ammo Books for .50 Cal | перки книги «Снайпер» на ББ/Эксп. патроны работают и для .50 | хосту / серверу (клиентам — для подсказки) |

### Установка

1. Скачай репозиторий (**Code → Download ZIP**) или архив из релиза.
2. Скопируй нужные папки из `Mods/` в папку `Mods` игры.
3. Запусти игру. BepInEx уже идёт в составе Undead Legacy (EAC выключен, как и для самого UL).

Все моды, кроме Restore tvCRT, ничего не пишут в сохранение — их можно ставить и удалять в любой момент.

### Sleeping Trigger Zombies

Во многих POI есть зоны спавна типа «триггер»: они пустые, пока ты не откроешь нужную дверь, не возьмёшь лут
или не наступишь на невидимую зону. Тогда зомби появляются, и через секунду игра будит их всех и натравливает на тебя.
Зачистить их заранее невозможно.

- **Триггер-зоны спавнятся при приближении** (~8 блоков), как обычные, — спящими, к ним можно подкрасться.
- **Триггер больше не будит зомби принудительно.** Вместо этого уже появившиеся проходят ванильную проверку скрытности:
  стоя тебя замечают всегда; присев — только ближе `lerp(3, 15 м, LightMultiplier)`, без своего фонаря и с навыками скрытности
  это ~3–5 м. Заметившие просыпаются и атакуют, остальные спят чутко. Засады из шкафов и стен работают, если шуметь.
- **Зоны, которые триггер спавнит впервые** (ты к ним не подходил), появляются спящими.
- **Сирены / тревоги** (скрипты зон со звуками `alarm`, `siren`, `buzzer`, `security`): каждый зомби с шансом `WakeChance`
  (50 %) просыпается **без** цели на тебя, остальные спят чутко.

Настройки: `BepInEx/config/talos.sleepingtriggers.cfg`. Каждое срабатывание пишется в `BepInEx/LogOutput.log`.

### UL Scope Fixes

1. **Прицел ночью намного темнее обычного вида** — камера прицела не получала автоэкспозицию и яркость. Исправлено.
2. **Средняя кнопка мыши приближала весь экран, а не прицел** — теперь меняет только кратность оптики по правилу UL.
3. **Зомби пропадает на максимальном приближении** — проверка видимости UL не учитывала кратность. Исправлено.
4. **Положение оружия вне прицеливания.** Для «Винтовки рейнджера» (M1A) оружие опущено на 6 см, при прицеливании всё как было.
   Подстройка в консоли (F1) с оружием в руках: `fpvoffset`, `fpvoffset 0 -0.08 0`, `fpvoffset reset`.

Работает для всех PIP-прицелов UL (малый 4×, средний 4–8×, большой 8–16×, встроенные у AUG и M202). ПНВ через прицел работает.

Не баг: игра растворяет зомби дальше 90 м (120 м при прицеливании), поэтому даже в 16× дальше никого не видно.

### UL Party Research Craft

UL проверяет исследование рецепта только у того, кто крафтит. Мод добавляет к проверке UL: если рецепт закрыт для тебя,
но исследован кем-то из группы, его можно крафтить, пока вы в группе. Касается только рецептов (верстаки, крафт в рюкзаке),
улучшение блоков (например, деревянный каркас) по-прежнему требует собственного исследования. Ставить каждому игроку.

### Keep Action Skills on Death

Убирает из штрафа за смерть UL только потерю экшен-навыков. Потеря опыта, «Травма на грани смерти» и сниженные статы
при возрождении остаются. Только XML, ставить на хост.

### Restore tvCRT_PlayerOff

UL удаляет блок `tvCRT_PlayerOff`, а POI `vb_FNS_Exham_Priory` из пака *Vanilla Block POIs by FNS* его использует и не
загружается. Мод возвращает блок как обычный телевизор UL (не крафтится). Ставить на хост и всем клиентам.
**Не удаляй мод после того, как эти POI сгенерировались в мире.**

### Sniper Ammo Books for .50 Cal

Пункты 6 и 7 книги «Снайпер» (+10% урона бронебойными / экспансивными) в UL привязаны только к патронам 7.92 мм,
M82 (.50) от них ничего не получала. Мод добавляет те же бонусы патронам .50 и обновляет текст книги. Только XML,
в сейв ничего не пишет.

### Honest Tooltips

Только текст и подсказки, механика не меняется. Подсказка брони теперь показывает шум и штраф выносливости (средняя +10% шума за вещь,
тяжёлая +20%, у лёгкой шума нет). В окне персонажа добавлена строка «Шумность» — текущий множитель шума (100% — норма) с учётом брони, модов и навыков скрытности. Строка «Выносливость при беге» — сколько выносливости в секунду тратится на бег прямо сейчас (броня, перки брони, препараты); её считает `HonestTooltips.dll` из мода (без BepInEx). Длительность еды и препаратов показывается правильно (у чая/кофе/пива в банках было «360 min», у части конфет — устаревшие 5–10 мин). У конфет и напитков показаны скрытые эффекты: драже удачи (+5/+10% этап добычи), Atom Junkies (урон по блокам), жажда от стероидов, Fort Bites и Recog, отмычки Jail Breakers, −5% опыта у Hackers/Rock Busters, расход воды у Mega Crush; у самогона и обезболивающего убрано несуществующее смягчение урона; напитки в пластиковых бутылках получили нормальную подсказку. У ножей, мачете и луков показан урон исподтишка (+200% / +400%). Модификации: перчатки на автоматы +5–50% (было 4,5–45%), радио +1–9 м (было 0,1–9), показаны скрытые эффекты «Улучшенной скрытности», охлаждающей сетки и утеплителя (защита от огня и тока), расширителя заряда, турбины, дульного тормоза; «Бесшумным ботинкам» возвращена подсказка. Самогон лечит 20 ед./с ~60 с (было «+600»).
Исправлены числа в книгах (Альманах пожарного 1, Снайпер 4), дереве навыков UL (Штурмовик, дробовики, комбо дубинкой, медик,
задержка дыхания), экшен-навыках (клинки, атлетика), удлинителе магазина, гнилом мясе, Fort Bites, обезболивающем и русских
описаниях модификаций с разбросом по качеству. Ставить хосту и всем клиентам, в сейв ничего не пишет.

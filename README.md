# UL Fixes — Undead Legacy quality-of-life fixes

Small mods for **7 Days to Die V2.6** with **Undead Legacy 2.7.40**. Each one is independent — install only what you want.

| Mod | What it does | Who installs it |
|---|---|---|
| [Sleeping Trigger Zombies](#sleeping-trigger-zombies) | POI "trigger" zombies that pop in awake and run at you | Host / server only |
| [UL Scope Fixes](#ul-scope-fixes) | UL picture-in-picture scopes: dark at night, middle-click zoom, zombies vanishing at max zoom, weapon too high | Every player (client-side) |
| [UL Party Research Craft](#ul-party-research-craft) | Recipes researched by any party member are craftable by everyone in the party | Every player (client-side) |
| [Keep Action Skills on Death](#keep-action-skills-on-death) | Removes only the action-skill loss from UL's death penalty | Host / server |
| [Restore tvCRT_PlayerOff](#restore-tvcrt_playeroff) | Lets the FNS "Exham Priory" POIs load with UL | Host and every client |

Русская версия — [ниже](#русский).

## Installation

1. Download the repository (**Code → Download ZIP**) or a release archive.
2. Copy the folder(s) you want from `Mods/` into your game's `Mods` folder
   (e.g. `...\steamapps\common\7 Days To Die\Mods\`).
3. Start the game. Undead Legacy ships BepInEx, so nothing else is needed (EAC must be off, as for UL itself).

Sleeping Trigger Zombies, UL Scope Fixes, Party Research and Keep Action Skills store nothing in your save —
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

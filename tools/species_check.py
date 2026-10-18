"""Validate the species data files in game/data/species/ (schema: docs/05_FISH_AND_ECOLOGY.md section 2).

Run from the repo root:  python tools/species_check.py

Checks every game/data/species/*.json for:
  - the required keys and nesting of the schema
  - id matches the file name, tier is S/1/2/3
  - at least two sources, each with name, url and retrieved date
  - known spot ids, lure and bait keys, bite and fight style keys, locomotion modes
  - 0-1 ranges on weights, hex colours in body.pattern
  - spawn temperature inside the species' lethal range
  - the length-weight relation at lengthCm.max is within +-35% of recordWeightKg

Prints a table of id, W(lengthCm.max) = a * L^b, recordWeightKg and the ratio. Exits 1 on any error.
Standard library only (Python 3.11).
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SPECIES_DIR = ROOT / "game" / "data" / "species"

RATIO_TOLERANCE = 0.35

SPOTS = {
    "station_pond", "spring_run", "suwannee", "waccasassa_flats", "homosassa_tarpon",
    "nature_coast_ledges", "middle_grounds",
}
TIERS = {"S", "1", "2", "3"}
LOCOMOTION = {
    "anguilliform", "subcarangiform", "carangiform", "thunniform", "amiiform", "balistiform",
    "labriform", "rajiform", "pleuronectiform", "benthic",
}
LURES = {
    "soft_plastic_worm", "jig", "spinnerbait", "crankbait", "topwater_walker", "popper", "frog",
    "swimbait", "jerkbait", "inline_spinner", "spoon", "streamer_fly", "popper_fly",
    # additions beyond the section-2 example (see game/data/species/README.md)
    "rope_lure",
}
BAITS = {
    "live_minnow", "wild_shiner", "nightcrawler", "red_worm", "cricket", "live_shrimp", "cut_bait",
    "chicken_liver", "stink_bait", "dough", "bread", "corn", "live_crayfish", "fiddler_crab", "barnacle",
}
BITE_STYLES = {"inhale", "nibble", "slam", "run", "lift", "shortStrike", "sip", "crash", "pickupDrop"}
FIGHT_STYLES = {
    "run", "dive", "bulldog", "circle", "jump", "headshake", "roll", "structureDash", "sulk", "tow",
    "boatRun", "surfaceSkitter",
}
STRUCTURE = {"weeds", "wood", "docks", "lilyPads", "rock", "dropOff", "openWater"}
DIEL = {"dawn", "day", "dusk", "night"}
SEASONS = {"winter", "spring", "summer", "autumn"}
WEATHER = {"overcast", "rain", "postColdFront", "risingWaterTemp"}
MARKING_TYPES = {
    "bars", "stripe", "spots", "blotched_band", "vermiculation", "eye_spot", "fin_edge",
    "opercular_flap", "iridescent_band",
}
HEX = re.compile(r"^#[0-9a-fA-F]{6}$")

# Required keys, as nested dicts; a value of None means "key must exist" (any value, null allowed).
REQUIRED = {
    "id": None, "commonName": None, "scientificName": None, "family": None, "tier": None, "sources": None,
    "water": {"salinityPpt": None, "temperatureC": {"lethalMin": None, "optimum": None, "lethalMax": None},
              "oxygenMinMgL": None, "clarityToleranceM": None},
    "habitat": {"spots": None, "depthM": None, "structure": None},
    "size": {"lengthCm": {"typical": None, "max": None},
             "lengthWeight": {"a": None, "b": None, "units": None, "source": None},
             "growth": {"maxAgeYears": None, "source": None},
             "recordWeightKg": None},
    "activity": {"diel": None, "lunar": None, "tide": None, "seasonal": None, "weather": None},
    "diet": {"items": None, "preyLengthMaxFrac": None},
    "feeding": {"mode": None, "biteStyles": None, "holdTimeS": None, "followChance": None,
                "speedPreferenceMps": None, "lureAttraction": None, "baitAttraction": None},
    "senses": {"vision": None, "spectral": {"r": None, "g": None, "b": None}, "lowLight": None,
               "lateralLine": None, "hearing": None, "smell": None, "electro": None, "lineShy": None,
               "spookiness": None},
    "behaviour": {"schooling": None, "territorial": None,
                  "spawn": None, "migration": None},
    "fight": {"powerCoeff": None, "stamina": {"anaerobic": None, "aerobic": None, "recovery": None},
              "styles": None, "mouth": {"hardness": None, "gapeFrac": None, "teeth": None, "gillPlateCut": None}},
    "body": {"locomotion": None, "spineNodes": None, "stiffness": None, "shapeRef": None,
             "pattern": {"dorsal": None, "flank": None, "belly": None, "markings": None, "fins": None,
                         "eye": None, "variants": None},
             "variants": None},
    "regulations": {"minLengthCm": None, "bagLimit": None, "closedSeason": None, "protected": None,
                    "invasive": None, "basedOn": None, "basedOnDate": None},
    "economy": {"edible": None, "pricePerKg": None, "marketDemand": None},
    "ledger": {"facts": None, "research": None, "wrenNote": None},
}


class Checker:
    def __init__(self, sid: str):
        self.sid = sid
        self.errors: list[str] = []
        self.warnings: list[str] = []

    def err(self, msg: str) -> None:
        self.errors.append(f"{self.sid}: {msg}")

    def warn(self, msg: str) -> None:
        self.warnings.append(f"{self.sid}: {msg}")

    def required(self, data: dict, spec: dict, path: str = "") -> None:
        for key, sub in spec.items():
            where = f"{path}.{key}" if path else key
            if not isinstance(data, dict) or key not in data:
                self.err(f"missing key '{where}'")
                continue
            if isinstance(sub, dict):
                self.required(data[key], sub, where)

    def unit(self, value, where: str) -> None:
        if not isinstance(value, (int, float)) or not 0.0 <= value <= 1.0:
            self.err(f"{where} = {value!r} is not a number in 0-1")

    def unit_map(self, mapping, where: str, allowed: set | None = None) -> None:
        if not isinstance(mapping, dict):
            self.err(f"{where} is not an object")
            return
        for k, v in mapping.items():
            if allowed is not None and k not in allowed:
                self.err(f"{where}: unknown key '{k}'")
            self.unit(v, f"{where}.{k}")

    def range2(self, value, where: str) -> bool:
        ok = (isinstance(value, list) and len(value) == 2 and all(isinstance(x, (int, float)) for x in value)
              and value[0] <= value[1])
        if not ok:
            self.err(f"{where} = {value!r} is not an ordered [min, max] pair")
        return ok


def check_species(path: Path) -> tuple[Checker, tuple | None]:
    sid = path.stem
    c = Checker(sid)
    try:
        d = json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:  # noqa: BLE001
        c.err(f"invalid JSON: {exc}")
        return c, None

    c.required(d, REQUIRED)
    if c.errors:
        return c, None

    if d["id"] != sid:
        c.err(f"id '{d['id']}' does not match file name '{sid}'")
    if str(d["tier"]) not in TIERS:
        c.err(f"tier '{d['tier']}' is not one of S/1/2/3")

    # sources
    src = d["sources"]
    if not isinstance(src, list) or len(src) < 2:
        c.err("needs at least 2 sources")
    else:
        for i, s in enumerate(src):
            for k in ("name", "url", "retrieved"):
                if not s.get(k):
                    c.err(f"sources[{i}] missing '{k}'")
            if s.get("url") and not str(s["url"]).startswith("https://"):
                c.err(f"sources[{i}].url is not an https URL")

    # water
    w = d["water"]
    c.range2(w["salinityPpt"], "water.salinityPpt")
    t = w["temperatureC"]
    if c.range2(t["optimum"], "water.temperatureC.optimum"):
        if not t["lethalMin"] < t["optimum"][0] <= t["optimum"][1] < t["lethalMax"]:
            c.err("water.temperatureC: need lethalMin < optimum < lethalMax")
    c.range2(w["clarityToleranceM"], "water.clarityToleranceM")

    # habitat
    h = d["habitat"]
    if not h["spots"]:
        c.err("habitat.spots is empty")
    for s in h["spots"]:
        if s not in SPOTS:
            c.err(f"habitat.spots: unknown spot '{s}'")
    c.range2(h["depthM"], "habitat.depthM")
    c.unit_map(h["structure"], "habitat.structure", STRUCTURE)

    # size and the length-weight sanity check
    sz = d["size"]
    c.range2(sz["lengthCm"]["typical"], "size.lengthCm.typical")
    lw = sz["lengthWeight"]
    lmax = sz["lengthCm"]["max"]
    rec = sz["recordWeightKg"]
    row = None
    if all(isinstance(x, (int, float)) for x in (lw["a"], lw["b"], lmax, rec)) and lw["a"] > 0 and rec > 0:
        w_kg = lw["a"] * lmax ** lw["b"] / 1000.0
        ratio = w_kg / rec
        row = (sid, lmax, w_kg, rec, ratio)
        if abs(ratio - 1.0) > RATIO_TOLERANCE:
            c.err(f"W(lengthCm.max) = {w_kg:.2f} kg is {ratio:.2f}x recordWeightKg {rec} (allowed +-35%)")
        if sz["lengthCm"]["typical"][1] > lmax:
            c.err("size.lengthCm.typical exceeds max")
    else:
        c.err("size: a, b, lengthCm.max and recordWeightKg must be positive numbers")
    g = sz["growth"]
    if ("Linf_cm" in g) != ("k" in g):
        c.err("size.growth: give both Linf_cm and k, or neither")
    if "Linf_cm" not in g and not g.get("notes"):
        c.err("size.growth: Linf_cm/k omitted without a 'notes' explanation")

    # activity
    a = d["activity"]
    c.unit_map(a["diel"], "activity.diel", DIEL)
    if set(a["diel"]) != DIEL:
        c.err("activity.diel needs dawn, day, dusk and night")
    c.unit(a["lunar"], "activity.lunar")
    if a["tide"] is not None:
        c.unit_map(a["tide"], "activity.tide", {"rising", "falling", "slack"})
    c.unit_map(a["seasonal"], "activity.seasonal", SEASONS)
    for k, v in a["weather"].items():
        if k not in WEATHER:
            c.err(f"activity.weather: unknown key '{k}'")
        if not isinstance(v, (int, float)) or not -1.0 <= v <= 1.0:
            c.err(f"activity.weather.{k} = {v!r} is not a modifier in -1..1")

    # diet and feeding
    items = d["diet"]["items"]
    c.unit_map(items, "diet.items")
    total = sum(items.values()) if isinstance(items, dict) else 0
    if not 0.9 <= total <= 1.1:
        c.err(f"diet.items sum to {total:.2f} (expected about 1)")
    c.unit(d["diet"]["preyLengthMaxFrac"], "diet.preyLengthMaxFrac")
    f = d["feeding"]
    c.unit_map(f["biteStyles"], "feeding.biteStyles", BITE_STYLES)
    c.range2(f["holdTimeS"], "feeding.holdTimeS")
    c.unit(f["followChance"], "feeding.followChance")
    c.range2(f["speedPreferenceMps"], "feeding.speedPreferenceMps")
    c.unit_map(f["lureAttraction"], "feeding.lureAttraction", LURES)
    c.unit_map(f["baitAttraction"], "feeding.baitAttraction", BAITS)

    # senses
    s = d["senses"]
    for k in ("vision", "lowLight", "lateralLine", "hearing", "smell", "electro", "lineShy", "spookiness"):
        c.unit(s[k], f"senses.{k}")
    c.unit_map(s["spectral"], "senses.spectral", {"r", "g", "b"})

    # behaviour
    b = d["behaviour"]
    c.unit(b["territorial"], "behaviour.territorial")
    sp = b["spawn"]
    if sp is not None:
        for k in ("season", "tempC", "habitat", "mode", "biteMotive"):
            if k not in sp:
                c.err(f"behaviour.spawn missing '{k}'")
        if sp.get("tempC") is not None and c.range2(sp["tempC"], "behaviour.spawn.tempC"):
            if not t["lethalMin"] < sp["tempC"][0] and sp["tempC"][1] < t["lethalMax"]:
                c.err("behaviour.spawn.tempC is outside the lethal range")

    # fight
    fi = d["fight"]
    if not isinstance(fi["powerCoeff"], (int, float)) or fi["powerCoeff"] <= 0:
        c.err("fight.powerCoeff must be positive")
    c.unit_map(fi["stamina"], "fight.stamina")
    c.unit_map(fi["styles"], "fight.styles", FIGHT_STYLES)
    c.unit_map(fi["mouth"], "fight.mouth")

    # body
    bo = d["body"]
    if bo["locomotion"] not in LOCOMOTION:
        c.err(f"body.locomotion '{bo['locomotion']}' is not a docs/04 section 9.2 mode")
    if bo.get("hoverMode") is not None and bo["hoverMode"] not in LOCOMOTION:
        c.err(f"body.hoverMode '{bo['hoverMode']}' is not a docs/04 section 9.2 mode")
    c.unit(bo["stiffness"], "body.stiffness")
    pat = bo["pattern"]
    for k in ("dorsal", "flank", "belly", "eye"):
        if not isinstance(pat[k], str) or not HEX.match(pat[k]):
            c.err(f"body.pattern.{k} = {pat[k]!r} is not a #rrggbb colour")
    for i, m in enumerate(pat["markings"]):
        if m.get("type") not in MARKING_TYPES:
            c.err(f"body.pattern.markings[{i}].type '{m.get('type')}' is unknown")
        if not HEX.match(str(m.get("color", ""))):
            c.err(f"body.pattern.markings[{i}].color is not a #rrggbb colour")
        if not m.get("where"):
            c.err(f"body.pattern.markings[{i}] needs 'where'")
    if not pat.get("source"):
        c.err("body.pattern needs a 'source' for the appearance")

    # regulations, economy, ledger
    r = d["regulations"]
    if not str(r["basedOn"]).startswith("https://"):
        c.err("regulations.basedOn must be a URL")
    for k in ("protected", "invasive"):
        if not isinstance(r[k], bool):
            c.err(f"regulations.{k} must be true/false")
    e = d["economy"]
    if e["marketDemand"] not in {"none", "low", "medium", "high"}:
        c.err("economy.marketDemand must be none/low/medium/high")
    if not e["edible"] and e["pricePerKg"] != 0:
        c.err("economy: inedible species should have pricePerKg 0")
    L = d["ledger"]
    if not 2 <= len(L["facts"]) <= 3:
        c.err("ledger.facts needs 2-3 facts")
    if len(L["research"]) != 3:
        c.err("ledger.research needs 3 tasks")
    for task in L["research"]:
        if not task.get("id") or not task.get("task"):
            c.err("ledger.research entries need id and task")
    return c, row


def main() -> int:
    files = sorted(SPECIES_DIR.glob("*.json"))
    if not files:
        print(f"no species files in {SPECIES_DIR}")
        return 1
    rows, errors, warnings = [], [], []
    ids = set()
    research_ids = set()
    for p in files:
        c, row = check_species(p)
        errors += c.errors
        warnings += c.warnings
        if row:
            rows.append(row)
        if p.stem in ids:
            errors.append(f"{p.stem}: duplicate id")
        ids.add(p.stem)
        try:
            for task in json.loads(p.read_text(encoding="utf-8"))["ledger"]["research"]:
                if task["id"] in research_ids:
                    errors.append(f"{p.stem}: duplicate research id '{task['id']}'")
                research_ids.add(task["id"])
        except Exception:  # noqa: BLE001
            pass

    print(f"{'id':<22} {'Lmax cm':>8} {'W(Lmax) kg':>11} {'record kg':>10} {'ratio':>6}")
    print("-" * 61)
    for sid, lmax, w_kg, rec, ratio in rows:
        flag = "" if abs(ratio - 1) <= RATIO_TOLERANCE else "  <-- out of range"
        print(f"{sid:<22} {lmax:>8.1f} {w_kg:>11.2f} {rec:>10.2f} {ratio:>6.2f}{flag}")
    print("-" * 61)
    print(f"{len(files)} species files, {len(errors)} errors, {len(warnings)} warnings")
    for w in warnings:
        print("WARN ", w)
    for e in errors:
        print("ERROR", e)
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())

"""Adds any [Tunable] SimConfig field missing from game/data/tuning/sim.json, with its code default.

Existing values in the JSON are kept (they're the owner's tuning). Run after adding tunables:
    python tools/sync_tuning.py
"""
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parent.parent
CONFIG = ROOT / "sim" / "Core" / "SimConfig.cs"
TUNING = ROOT / "game" / "data" / "tuning" / "sim.json"

FIELD = re.compile(
    r"\[Tunable\([^\]]*\)\]\s*public\s+(float|int|bool)\s+(\w+)\s*=\s*([^;]+);", re.MULTILINE
)


def parse_default(kind: str, text: str):
    text = text.strip()
    if kind == "bool":
        return text == "true"
    if kind == "int":
        return int(text)
    return float(text.rstrip("fF"))


def main() -> None:
    fields = [(m.group(2), parse_default(m.group(1), m.group(3))) for m in FIELD.finditer(CONFIG.read_text("utf-8"))]
    current = json.loads(TUNING.read_text("utf-8")) if TUNING.exists() else {}
    known = {name for name, _ in fields}
    out = {}
    added = []
    for name, default in fields:
        if name in current:
            out[name] = current[name]
        else:
            out[name] = default
            added.append(name)
    dropped = [k for k in current if k not in known]
    TUNING.write_text(json.dumps(out, indent=2) + "\n", "utf-8")
    print(f"{len(fields)} tunables; added {added or 'none'}; dropped unknown {dropped or 'none'}")


if __name__ == "__main__":
    main()

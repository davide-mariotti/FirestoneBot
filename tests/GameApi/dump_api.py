"""Turn REA's inspect-il2cpp-metadata Evidence into a greppable, diffable API listing.

After a game update, from a folder outside the repo (G = a box's Firestone dir, newest build):
  rea inspect-il2cpp-metadata "$G/Firestone_Data/il2cpp_data/Metadata/global-metadata.dat" \
      --binary "$G/GameAssembly.dll" --detail summary --evidence_output <abs>/il2cpp.json
  python dump_api.py <abs>/il2cpp.json tests/GameApi
then `git diff --no-index` the old and new .api.txt to see what the build changed.
The .txt outputs are gitignored: game names/strings stay local (public repo).

usage: python dump_api.py <evidence.json> <out_dir> [image=CoreASM.dll]
writes <out_dir>/<image>.api.txt and <out_dir>/strings.txt
"""
import json, sys, os

ev, out = sys.argv[1], sys.argv[2]
image = sys.argv[3] if len(sys.argv) > 3 else "CoreASM.dll"
r = json.load(open(ev, encoding="utf-8"))["records"][0]["normalized_result"]

types, fields, ftypes = r["types"], r["fields"], r["field_types"]
methods, rtypes = r["methods"], r["return_types"]
params, ptypes, props = r["parameters"], r["parameter_types"], r["properties"]

img = next(i for i in r["images"] if i["name"] == image)
lo, hi = img["type_start"], img["type_start"] + img["type_count"]

def tname(i):
    t = types[i]
    if t["declaring_type"] >= 0:
        return tname(t["declaring_type"]) + "+" + t["name"]
    return (t["namespace"] + "." if t["namespace"] else "") + t["name"]

def ty(x):
    return x["name"] or f"?{x['type']}"

by_owner = {}
for i, f in enumerate(fields):
    by_owner.setdefault(f["declaring_type"], {"f": [], "p": [], "m": []})["f"].append(i)
for i, p in enumerate(props):
    by_owner.setdefault(p["declaring_type"], {"f": [], "p": [], "m": []})["p"].append(i)
for i, m in enumerate(methods):
    by_owner.setdefault(m["declaring_type"], {"f": [], "p": [], "m": []})["m"].append(i)
mparams = {}
for i, p in enumerate(params):
    mparams.setdefault(p["method"], []).append(f"{ty(ptypes[i])} {p['name']}")

skip = lambda n: n.startswith("<")  # compiler-generated closures and backing fields
lines = []
for t in sorted(range(lo, hi), key=tname):
    n = tname(t)
    if skip(types[t]["name"]):
        continue
    lines.append(f"type {n}")
    o = by_owner.get(t, {"f": [], "p": [], "m": []})
    for i in o["f"]:
        if not skip(fields[i]["name"]):
            lines.append(f"  field {ty(ftypes[i])} {fields[i]['name']}")
    for i in o["p"]:
        lines.append(f"  prop  {props[i]['name']}")
    for i in o["m"]:
        nm = methods[i]["name"]
        if skip(nm) or nm.startswith(("get_", "set_")):
            continue
        lines.append(f"  meth  {ty(rtypes[i])} {nm}({', '.join(mparams.get(i, []))})")

os.makedirs(out, exist_ok=True)
with open(os.path.join(out, image + ".api.txt"), "w", encoding="utf-8", newline="\n") as fh:
    fh.write("\n".join(lines) + "\n")
with open(os.path.join(out, "strings.txt"), "w", encoding="utf-8", newline="\n") as fh:
    fh.write("\n".join(sorted({s["value"].replace("\n", "\\n") for s in r["string_literals"] if s["value"]})) + "\n")
print(len(lines), "lines")

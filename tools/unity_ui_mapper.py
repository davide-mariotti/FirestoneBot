"""
Maps Firestone's UI screens (GameObject/Transform hierarchy + real component types)
directly from resources.assets, without launching the game.

Every screen in this game (battle HUD panels, Town, Library, Guild, popups...) is its
own standalone prefab in resources.assets, discoverable by its root GameObject's name
(e.g. "topSideUI", "TownIrongard", "Library", "HallOfHeroes"). This walks that prefab's
Transform tree and resolves each MonoBehaviour component to its real class (Button,
TMPro.TextMeshProUGUI, or a custom script) by cross-referencing globalgamemanagers.assets.

Usage:
    python unity_ui_mapper.py <RootGameObjectName> [<AnotherRoot> ...] > out.json

Find candidate root names first by grepping resources.assets object names (e.g. via a
quick UnityPy scan), or by cross-referencing existing paths in src/Infrastructure/Paths.cs.
"""

import UnityPy, json, sys

BASE = "C:/Program Files (x86)/Steam/steamapps/common/Firestone/Firestone_Data"

env = UnityPy.load(BASE + "/resources.assets")
by_pathid = {obj.path_id: obj for obj in env.objects}

# externals table for cross-file PPtr resolution (m_FileID: 0=self, N=externals[N-1])
sample_obj = next(iter(env.objects))
externals = sample_obj.assets_file.externals

_external_envs = {}
_script_name_cache = {}  # (fileID, pathID) -> "Namespace.ClassName" or "ClassName" or None


def load_external(name):
    if name in _external_envs:
        return _external_envs[name]
    try:
        e = UnityPy.load(BASE + "/" + name)
        idx = {o.path_id: o for o in e.objects}
    except Exception as ex:
        print(f"Failed to load external {name}: {ex}", file=sys.stderr)
        idx = {}
    _external_envs[name] = idx
    return idx


def resolve_script_class(file_id, path_id):
    key = (file_id, path_id)
    if key in _script_name_cache:
        return _script_name_cache[key]

    if file_id == 0:
        target = by_pathid.get(path_id)
    else:
        idx = file_id - 1
        if idx < 0 or idx >= len(externals):
            _script_name_cache[key] = None
            return None
        target = load_external(externals[idx].name).get(path_id)

    if target is None or target.type.name != "MonoScript":
        _script_name_cache[key] = None
        return None

    try:
        d = target.read_typetree(check_read=False)
        cls, ns = d.get("m_ClassName"), d.get("m_Namespace")
        full = f"{ns}.{cls}" if ns else cls
    except Exception:
        full = None
    _script_name_cache[key] = full
    return full


def component_label(cobj):
    """Friendly type label for a component (resolves MonoBehaviour -> its real script class)."""
    if cobj.type.name != "MonoBehaviour":
        return cobj.type.name
    try:
        tree = cobj.read_typetree(check_read=False)
        script_ref = tree.get("m_Script")
        if not script_ref:
            return "MonoBehaviour"
        return resolve_script_class(script_ref["m_FileID"], script_ref["m_PathID"]) or "MonoBehaviour"
    except Exception:
        return "MonoBehaviour"


def go_data(go_pid):
    gobj = by_pathid.get(go_pid)
    if gobj is None:
        return None
    d = gobj.read()
    comp_labels = [
        component_label(cobj)
        for cp in d.m_Component
        if (cobj := by_pathid.get(cp.component.m_PathID)) is not None
        and cobj.type.name not in ("Transform", "RectTransform")
    ]
    return {"name": d.m_Name, "active": bool(d.m_IsActive), "components": comp_labels}


def dump_subtree(transform_pid, max_depth=40):
    tobj = by_pathid.get(transform_pid)
    if tobj is None:
        return None
    tdata = tobj.read()
    info = go_data(tdata.m_GameObject.m_PathID)
    if info is None:
        return None
    node = {"name": info["name"], "active": info["active"], "components": info["components"]}
    if max_depth <= 0:
        node["truncated"] = True
        return node
    children = [
        c for ptr in tdata.m_Children
        if (c := dump_subtree(ptr.m_PathID, max_depth - 1)) is not None
    ]
    if children:
        node["children"] = children
    return node


def find_root_transform_by_go_name(name):
    """Return (go_pid, transform_pid, n_children) for the GameObject named `name` whose Transform
    has no parent (heuristic to pick the real prefab root among any duplicately-named objects,
    preferring the one with the most children)."""
    best = None
    for obj in env.objects:
        if obj.type.name != "GameObject":
            continue
        d = obj.read()
        if d.m_Name != name:
            continue
        for cp in d.m_Component:
            cobj = by_pathid.get(cp.component.m_PathID)
            if cobj and cobj.type.name in ("Transform", "RectTransform"):
                tdata = cobj.read()
                if tdata.m_Father.m_PathID == 0:
                    n_children = len(tdata.m_Children)
                    if best is None or n_children > best[2]:
                        best = (obj.path_id, cp.component.m_PathID, n_children)
    return best


if __name__ == "__main__":
    names = sys.argv[1:] or ["topSideUI"]
    result = {}
    for name in names:
        found = find_root_transform_by_go_name(name)
        if not found:
            print(f"NOT FOUND as a root prefab: {name}", file=sys.stderr)
            continue
        go_pid, t_pid, n_children = found
        result[name] = dump_subtree(t_pid)
        print(f"Dumped {name}: {n_children} top-level children", file=sys.stderr)

    print(json.dumps(result, indent=1))

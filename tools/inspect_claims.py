"""
Dumps a GameObject's hierarchy with REAL script class names resolved for every MonoBehaviour
(via its m_Script -> MonoScript reference, following external assembly references when needed),
not just the generic "MonoBehaviour" label unity_ui_mapper.py and earlier ad-hoc scripts this
session settled for. This is what let this session confirm, for the first time, whether a given
node genuinely carries a UnityEngine.UI.Button (a real click target) versus just looking like one -
the exact distinction that mattered for the Free Pickaxes / Oracle's Gift claim-button bugs.

The three named lookups at the bottom (Quests/Meteorite Research/Oracle Store) were this session's
specific investigation - edit them or add your own `for obj in env.objects: ... dump_node(...)`
block for a different screen. Assumes the default Steam install path in BASE below.
"""
import UnityPy
import sys

BASE = "C:/Program Files (x86)/Steam/steamapps/common/Firestone/Firestone_Data"
env = UnityPy.load(BASE + "/resources.assets")
by_pathid = {obj.path_id: obj for obj in env.objects}

sample_obj = next(iter(env.objects))
externals = sample_obj.assets_file.externals
_external_envs = {}
_script_name_cache = {}

def load_external(name):
    if name in _external_envs:
        return _external_envs[name]
    try:
        e = UnityPy.load(BASE + "/" + name)
        idx = {o.path_id: o for o in e.objects}
    except Exception as ex:
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
            return None
        target = load_external(externals[idx].name).get(path_id)
    if target is None or target.type.name != "MonoScript":
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

def get_go_info(go_obj):
    d = go_obj.read()
    comps = []
    t_obj = None
    for cp in d.m_Component:
        cobj = by_pathid.get(cp.component.m_PathID)
        if not cobj: continue
        if cobj.type.name in ("Transform", "RectTransform"):
            t_obj = cobj
        else:
            comps.append(component_label(cobj))
    return d.m_Name, bool(d.m_IsActive), comps, t_obj

def dump_node(t_obj, depth=0, max_depth=6):
    if not t_obj or depth > max_depth: return
    tdata = t_obj.read()
    go_obj = by_pathid.get(tdata.m_GameObject.m_PathID)
    if not go_obj: return
    name, active, comps, _ = get_go_info(go_obj)
    comp_str = f" [{', '.join(comps)}]" if comps else " (no comps)"
    act_str = "" if active else " [INACTIVE]"
    print(f"{'  ' * depth}- {name}{act_str}{comp_str}")
    for child_ptr in tdata.m_Children:
        c_tobj = by_pathid.get(child_ptr.m_PathID)
        if c_tobj:
            dump_node(c_tobj, depth + 1, max_depth)

# 1. Investigate Quests
print("=== 1. QUESTS INSPECTION ===")
for obj in env.objects:
    if obj.type.name != 'GameObject': continue
    d = obj.read()
    if d.m_Name in ("dailyQuestsScroll", "Character", "quests"):
        name, active, comps, tobj = get_go_info(obj)
        print(f"Found candidate: {name} (path_id {obj.path_id})")
        dump_node(tobj, max_depth=4)
        break

# 2. Investigate Meteorite Research
print("\n=== 2. METEORITE RESEARCH INSPECTION ===")
for obj in env.objects:
    if obj.type.name != 'GameObject': continue
    d = obj.read()
    if d.m_Name == "meteoriteResearch":
        name, active, comps, tobj = get_go_info(obj)
        print(f"Found meteoriteResearch (path_id {obj.path_id})")
        dump_node(tobj, max_depth=4)
        break

# 3. Investigate OracleStore oraclesGift
print("\n=== 3. ORACLE STORE INSPECTION ===")
for obj in env.objects:
    if obj.type.name != 'GameObject': continue
    d = obj.read()
    if d.m_Name == "oraclesGift":
        name, active, comps, tobj = get_go_info(obj)
        print(f"Found oraclesGift (path_id {obj.path_id})")
        dump_node(tobj, max_depth=4)

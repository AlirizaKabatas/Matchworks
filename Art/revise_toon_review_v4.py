"""Targeted Blender revision; retains approved silhouettes and v3 on disk."""
import bpy, math, os
from math import sin, cos, pi
from mathutils import Vector
path='/Users/ali/MechanicalCrush/Art/WorkshopMatch_Toon_Review_v4.blend'
if os.path.exists(path): raise RuntimeError('v4 exists; refusing to overwrite')
ROOT=bpy.data.collections['WM_TOON_REVIEW_v3']
M={m.name[3:]:m for m in bpy.data.materials if m.name.startswith('WM_')}
source=open('/Users/ali/MechanicalCrush/Art/build_toon_review.py').read()
exec(source[source.index('def group('):source.index('cube=bpy.data.objects')])

oil=M['Oil']; color=(.105,.029,.006,1)
oil.diffuse_color=color
next(n for n in oil.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Base Color'].default_value=color

def replace_meshes(name):
    c=bpy.data.collections[name]; r=bpy.data.objects[name]
    for o in list(c.objects):
        if o.type=='MESH': bpy.data.objects.remove(o,do_unlink=True)
    return c,r

# Pronounced two-lobed jelly/peanut silhouette; Ruby remains unchanged.
g=replace_meshes('04_Candy_Mint')
outline=[]
for i in range(40):
    t=2*pi*i/40
    outline.append((.43*cos(t)*(.53+.72*sin(t)**2),.47*sin(t)))
rings('Green_double_lobe_gummy',outline,[(.58,-.20),(.92,-.11),(1,0),(.92,.12),(.58,.23)],'Mint',g,True)
g[1]['silhouette']='Strong narrow waist, two broad rounded lobes; distinct from oval Ruby'

# Exact one-cell bounding footprint. Tiny edge rounding, no irregular outer outline.
g=replace_meshes('08_Frozen_Grease')
base=box('Frozen_grease_full_cell',(0,0,0),(1,1,.24),'FrozenOil',g,.012)
for p in base.data.polygons: p.use_smooth=True
normal=base.modifiers.new('Flat face soft edge normals','WEIGHTED_NORMAL'); normal.keep_sharp=True
for i,(x,y,w,length) in enumerate([(-.32,.20,.067,.20),(-.10,.14,.092,.26),(.16,.25,.075,.17),(.34,.12,.052,.23)]):
    ball('Frozen_flow_'+str(i),(x,y,.13),(w,length,.040),'FrozenCream',g,12,6)
    ball('Frozen_drop_'+str(i),(x,y-length*.75,.14),(w*1.06,w*1.12,.055),'FrozenCream',g,12,6)
for i,(x,y,r) in enumerate([(-.30,-.29,.075),(.03,-.22,.052),(.28,-.31,.082)]):
    ball('Frozen_bubble_'+str(i),(x,y,.13),(r,r,.047),'FrozenOil',g,12,6)
g[1]['footprint']='1.0 x 1.0 cell; scale by board pitch, not candy normalization'
g[1]['surface']='Solid pale caramel square with arrested drips and rounded grease bubbles'
for o in ROOT.objects:
    if o.type=='FONT' and 'REVIEW 03' in o.data.body: o.data.body=o.data.body.replace('REVIEW 03','REVIEW 04')
ROOT.name='WM_TOON_REVIEW_v4'
bpy.ops.wm.save_as_mainfile(filepath=path)
print('V4 saved; changed only oil color, green silhouette and frozen grease geometry')

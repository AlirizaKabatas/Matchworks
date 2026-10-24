"""Run in the existing v2 Blender review scene. No Unity export."""
import bpy, math, os
from math import sin, cos, pi
from mathutils import Vector
path='/Users/ali/MechanicalCrush/Art/WorkshopMatch_Toon_Review_v3.blend'
if os.path.exists(path): raise RuntimeError('v3 already exists; do not overwrite a reviewed file')
ROOT=bpy.data.collections['WM_TOON_REVIEW_v2']
M={m.name[3:]:m for m in bpy.data.materials if m.name.startswith('WM_')}
source=open('/Users/ali/MechanicalCrush/Art/build_toon_review.py').read()
exec(source[source.index('def group('):source.index("cube=bpy.data.objects")])

def fresh(name):
    c=bpy.data.collections[name]; r=bpy.data.objects[name]
    # Only replace meshes in explicitly requested groups. v2 remains saved intact.
    for o in list(c.objects):
        if o.type=='MESH': bpy.data.objects.remove(o,do_unlink=True)
    return c,r
def mat(name,color,rough=.24,metal=0):
    m=bpy.data.materials.new('WM_'+name); m.use_nodes=True; M[name]=m
    m.diffuse_color=(*color,1)
    n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    n.inputs['Base Color'].default_value=(*color,1)
    n.inputs['Roughness'].default_value=rough
    n.inputs['Metallic'].default_value=metal
    n.inputs['Coat Weight'].default_value=.48
    return m
mat('Oil',(.22,.075,.018),.20)
mat('FrozenOil',(.56,.29,.12),.31)
mat('FrozenCream',(.75,.47,.24),.30)
mat('Turquoise',(.012,.54,.48),.17)
mat('Glint',(1,.94,.76),.2)

# Five non-fruit, non-flower candy silhouettes.
g=fresh('01_Candy_Cherry')
ball('Ruby_oval_dragee',(0,0,0),(.47,.35,.29),'Cherry',g,20,10)
g=fresh('02_Candy_Azure')
o=box('Blue_pillow',(0,0,0),(.83,.85,.52),'Azure',g,.20)
g=fresh('03_Candy_Honey')
o=box('Golden_diamond_bonbon',(0,0,0),(.66,.66,.48),'Honey',g,.12); o.rotation_euler.z=pi/4
g=fresh('04_Candy_Mint')
ball('Green_gummy_capsule',(0,0,0),(.31,.48,.28),'Mint',g,20,10)
g=fresh('05_Candy_Violet')
v=[]; f=[]; sections=13; around=12
for j in range(sections):
    t=j/(sections-1); y=(t-.5)*.94
    envelope=.06+.26*sin(pi*t)**.55
    for i in range(around):
        angle=i*2*pi/around+t*2.5*pi
        radius=envelope*(1+.14*cos(3*i*2*pi/around))
        v.append((radius*cos(angle),y,radius*sin(angle)))
f.append(tuple(reversed(range(around))))
for j in range(sections-1):
    for i in range(around): f.append((j*around+i,j*around+(i+1)%around,(j+1)*around+(i+1)%around,(j+1)*around+i))
f.append(tuple((sections-1)*around+i for i in range(around)))
o=mesh('Purple_twist',v,f,'Violet',g,True)
o.data.materials.append(M['Pink'])
for i,p in enumerate(o.data.polygons):
    if 0<i<1+(sections-1)*around and (i-1)%around in [0,1,6,7]: p.material_index=1

# Solvent is now an industrial squeeze cartridge, not a perfume bottle.
g=fresh('10_Power_Solvent')
box('Cleaner_cartridge',(0,-.08,0),(.55,.68,.38),'Turquoise',g,.14)
box('Cleaner_shoulder',(0,.25,0),(.38,.18,.32),'Brass',g,.06)
box('Cleaner_cap',(0,.40,0),(.30,.15,.28),'GoldLight',g,.04)
stroke('Cleaner_spout',(0,.45,0),(.21,.57,0),.11,'Steel',g)
disc('Cleaner_medallion',(0,-.08,.21),.17,.04,'Cream',g,16)
ball('Cleaner_droplet',(0,-.10,.255),(.058,.078,.018),'Turquoise',g,12,6)
for x in [-.16,0,.16]: box('Grip_rib',(x,-.28,.18),(.035,.15,.03),'Brass',g,.013)

# Frozen grease: a pale caramel waxy crust with arrested flowing drips.
g=fresh('08_Frozen_Grease')
outline=[]
for i in range(32):
    a=2*pi*i/32
    r=1+.055*sin(3*a)+.035*cos(5*a)
    outline.append((.45*r*math.copysign(abs(cos(a))**.45,cos(a)),.42*r*math.copysign(abs(sin(a))**.45,sin(a))))
rings('Frozen_caramel_crust',outline,[(.84,-.10),(1,-.04),(1,.04),(.90,.13),(.57,.18)],'FrozenOil',g,True)
for i,(x,y,s) in enumerate([(-.27,.12,.09),(-.09,.04,.13),(.14,.09,.11),(.28,.19,.065)]):
    ball('Arrested_drip_'+str(i),(x,y,.145),(s*.70,s*1.9,.045),'FrozenCream',g,12,6)
    ball('Rounded_drop_'+str(i),(x,y-s*1.4,.125),(s*.72,s*.78,.052),'FrozenCream',g,12,6)

# Concept swatch only: one continuous surface demonstrates two joined oily cells.
g=group('15_Oil_Joined_Surface_Preview',(4,-3.3,0))
outline=[]
for i in range(40):
    a=2*pi*i/40
    outline.append((.79*math.copysign(abs(cos(a))**.28,cos(a)),.39*math.copysign(abs(sin(a))**.28,sin(a))))
rings('Two_cells_no_internal_seam',outline,[(.93,-.02),(1,0),(.97,.035)],'Oil',g,True)
g[1]['implementation']='Preview only; Unity should merge adjacent oil cells into a single rounded outer contour. No per-cell internal border.'
label('OIL / JOINED SURFACE',(3.1,-4.05,.05),.13)

# Shared polished-toon treatment: saturated colors, broad specular, soft bevels.
palette={'Cherry':(.82,.018,.035),'Azure':(.008,.22,.95),'Honey':(1,.49,.012),
 'Mint':(.018,.66,.12),'Violet':(.38,.018,.74),'GoldLight':(1,.57,.095),
 'Brass':(.70,.29,.035),'Steel':(.24,.40,.54),'Wood':(.24,.075,.021)}
for name,m in M.items():
    node=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
    if not node: continue
    if name in palette:
        m.diffuse_color=(*palette[name],1)
        node.inputs['Base Color'].default_value=(*palette[name],1)
    candy=name in ['Cherry','Azure','Honey','Mint','Violet','Pink']
    if name not in ['Oil','FrozenOil','FrozenCream']:
        node.inputs['Roughness'].default_value=.18 if candy else .25 if name!='Wood' else .38
        node.inputs['Coat Weight'].default_value=.65 if candy else .32
        node.inputs['Coat Roughness'].default_value=.13
        node.inputs['Metallic'].default_value=.30 if name in ['Brass','GoldLight','Steel','DarkSteel'] else 0
for o in ROOT.all_objects:
    if o.type!='MESH': continue
    if o.name.startswith('Gear_') and not o.modifiers:
        b=o.modifiers.new('Toon bevel','BEVEL'); b.width=.025; b.segments=2
    if any(m.type=='BEVEL' for m in o.modifiers):
        for p in o.data.polygons: p.use_smooth=True
        if not any(m.type=='WEIGHTED_NORMAL' for m in o.modifiers):
            n=o.modifiers.new('Broad face highlights','WEIGHTED_NORMAL'); n.keep_sharp=True

labels={'Label_01 CHERRY':'01 RUBY DRAGEE','Label_02 AZURE':'02 BLUE PILLOW',
 'Label_03 HONEY':'03 GOLD BONBON','Label_04 MINT':'04 GREEN GUMMY','Label_05 VIOLET':'05 PURPLE TWIST',
 'Label_08 FROZEN':'08 FROZEN GREASE','Label_10 SOLVENT':'10 CLEANER'}
for name,text in labels.items():
    o=bpy.data.objects.get(name)
    if o: o.data.body=text; o.data.size=.135
for o in ROOT.objects:
    if o.type=='FONT' and 'REVIEW 02' in o.data.body: o.data.body=o.data.body.replace('REVIEW 02','REVIEW 03')
for a in bpy.context.screen.areas:
    if a.type=='VIEW_3D':
        s=a.spaces.active.shading; s.type='MATERIAL'; s.use_scene_world=False; s.use_scene_lights=False
        s.studiolight_rotate_z=.6; s.studiolight_intensity=1.15
        # Built-in environment only: no downloads or paid generation.
        s.studio_light='studio.exr'
ROOT.name='WM_TOON_REVIEW_v3'
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=path)
print('V3 saved, v2 retained; no Unity files touched')

"""Blender-only art review set. Never exports or writes into Assets/."""
import bpy, math
from mathutils import Vector, Quaternion
from math import sin, cos, pi

ROOT = bpy.data.collections.new('WM_TOON_REVIEW_v2')
bpy.context.scene.collection.children.link(ROOT)
M = {}
def material(name, color, metal=0, rough=.3):
    m = bpy.data.materials.new('WM_' + name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    node = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    node.inputs['Base Color'].default_value = (*color, 1)
    node.inputs['Metallic'].default_value = metal
    node.inputs['Roughness'].default_value = rough
    node.inputs['Coat Weight'].default_value = .24
    M[name] = m
    return m
for name, c, metal in [
    ('Cherry',(.72,.018,.035),0),('Azure',(.015,.24,.85),0),
    ('Honey',(1,.53,.035),.12),('Mint',(.018,.58,.11),0),
    ('Violet',(.46,.035,.82),0),('Brass',(.83,.43,.075),.55),
    ('GoldLight',(1,.72,.22),.38),('Steel',(.36,.51,.61),.65),
    ('DarkSteel',(.055,.10,.14),.45),('Wood',(.27,.105,.035),0),
    ('Ice',(.19,.74,.87),.1),('Cream',(.92,.85,.65),0),
    ('Rust2',(.30,.07,.022),.08),('Rust1',(.62,.23,.045),.22),
    ('Pink',(.88,.04,.30),0)]: material(name,c,metal)

def group(name, pos):
    c=bpy.data.collections.new(name); ROOT.children.link(c)
    r=bpy.data.objects.new(name,None); c.objects.link(r); r.location=pos
    r['approval']='DRAFT — Ali review; NOT exported to Unity'
    return c,r
def mesh(name, verts, faces, mat, g, smooth=False, bevel=0):
    me=bpy.data.meshes.new(name); me.from_pydata(verts,[],faces); me.update()
    o=bpy.data.objects.new(name,me); g[0].objects.link(o); o.parent=g[1]
    me.materials.append(M[mat])
    for f in me.polygons: f.use_smooth=smooth
    if bevel:
        mod=o.modifiers.new('Small toon edge','BEVEL'); mod.width=bevel; mod.segments=2
    return o
def box(name,p,s,mat,g,bevel=.06):
    v=[(x*s[0]/2,y*s[1]/2,z*s[2]/2) for x,y,z in
       [(-1,-1,-1),(-1,-1,1),(-1,1,-1),(-1,1,1),(1,-1,-1),(1,-1,1),(1,1,-1),(1,1,1)]]
    o=mesh(name,v,[(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)],mat,g,False,bevel)
    o.location=p; return o
def rings(name, outline, profile, mat,g,smooth=False):
    n=len(outline); v=[(x*r,y*r,z) for r,z in profile for x,y in outline]
    f=[tuple(reversed(range(n))),tuple((len(profile)-1)*n+i for i in range(n))]
    for j in range(len(profile)-1):
        for i in range(n): f.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    return mesh(name,v,f,mat,g,smooth)
def disc(name,p,r,h,mat,g,n=16):
    o=rings(name,[(cos(i*2*pi/n),sin(i*2*pi/n)) for i in range(n)],
      [(r*.87,-h/2),(r,-h/2+.03),(r,h/2-.03),(r*.87,h/2)],mat,g)
    o.location=p; return o
def ball(name,p,s,mat,g,n=16,k=8):
    v=[(0,0,-s[2])]
    for j in range(1,k):
        a=-pi/2+j*pi/k
        for i in range(n):
            t=2*pi*i/n; v.append((s[0]*cos(a)*cos(t),s[1]*cos(a)*sin(t),s[2]*sin(a)))
    v.append((0,0,s[2])); top=len(v)-1
    f=[(0,1+(i+1)%n,1+i) for i in range(n)]
    for j in range(k-2):
        for i in range(n): f.append((1+j*n+i,1+j*n+(i+1)%n,1+(j+1)*n+(i+1)%n,1+(j+1)*n+i))
    f.extend((top,1+(k-2)*n+i,1+(k-2)*n+(i+1)%n) for i in range(n))
    o=mesh(name,v,f,mat,g,True); o.location=p; return o
def tube(name,p,outer,inner,h,mat,g,n=24):
    v=[]
    for r,z in [(outer,-h/2),(outer,h/2),(inner,h/2),(inner,-h/2)]:
        for i in range(n): v.append((r*cos(i*2*pi/n),r*sin(i*2*pi/n),z))
    f=[]
    for ring in range(4):
        for i in range(n): f.append((ring*n+i,ring*n+(i+1)%n,((ring+1)%4)*n+(i+1)%n,((ring+1)%4)*n+i))
    o=mesh(name,v,f,mat,g,False,.015); o.location=p; return o
def stroke(name,a,b,width,mat,g):
    a,b=Vector(a),Vector(b); o=box(name,(a+b)/2,(width,width,(b-a).length),mat,g,width*.25)
    o.rotation_quaternion=(b-a).to_track_quat('Z','Y')
    # Assign matrix instead of relying on the object's Euler rotation mode.
    o.rotation_euler=o.rotation_quaternion.to_euler(); return o
def label(text,p,size=.16):
    bpy.ops.object.text_add(location=p)
    o=bpy.context.object; o.name='Label_'+text; o.data.body=text; o.data.size=size
    o.data.materials.append(M['Cream'])
    for c in list(o.users_collection): c.objects.unlink(o)
    ROOT.objects.link(o)
    return o

cube=bpy.data.objects.get('Cube')
if cube is not None and cube.type=='MESH': bpy.data.objects.remove(cube,do_unlink=True)

# All silhouettes face +Z, with shallow depth. Roots remain movable review placements.
g=group('01_Candy_Cherry',(-4,3,0))
outline=[]
for i in range(24):
    t=i*2*pi/24
    outline.append((.43*cos(t)*(1+.13*sin(t)),.43*sin(t)-.075*max(0,sin(t))**12))
rings('Cherry_body',outline,[(.63,-.20),(.96,-.11),(1,0),(.91,.17),(.55,.25)],'Cherry',g,True)
stroke('Cherry_stem',(.015,.34,.04),(.06,.54,.12),.08,'Wood',g)
o=ball('Cherry_leaf',(.20,.43,.09),(.18,.07,.035),'Mint',g,12,6); o.rotation_euler.z=.35

g=group('02_Candy_Azure',(-2,3,0))
box('Azure_soft_square',(0,0,0),(.84,.90,.43),'Azure',g,.15)
g=group('03_Candy_Honey',(0,3,0))
rings('Honey_hex_crystal',[(.48*cos(i*pi/3),.48*sin(i*pi/3)) for i in range(6)],
      [(.68,-.24),(1,-.12),(1,.12),(.70,.32)],'Honey',g)
g=group('04_Candy_Mint',(2,3,0))
# A squat rounded green jar silhouette seen from the gameplay face.
outline=[(-.22,.43),(.22,.43),(.28,.35),(.20,.20),(.30,.10),(.40,-.12),(.36,-.34),(.20,-.43),(-.20,-.43),(-.36,-.34),(-.40,-.12),(-.30,.10),(-.20,.20),(-.28,.35)]
outline.reverse()
rings('Mint_jar',outline,[(.70,-.20),(1,-.06),(1,.08),(.78,.23)],'Mint',g,True)
g=group('05_Candy_Violet',(4,3,0))
outline=[((.34+.09*cos(5*t))*cos(t),(.34+.09*cos(5*t))*sin(t)) for t in [i*2*pi/40 for i in range(40)]]
rings('Violet_five_petals',outline,[(.75,-.19),(1,-.06),(1,.08),(.72,.22)],'Violet',g,True)
ball('Flower_center',(0,0,.22),(.12,.12,.075),'Pink',g,12,6)
for x,t in zip([-4,-2,0,2,4],['01 CHERRY','02 AZURE','03 HONEY','04 MINT','05 VIOLET']): label(t,(x-.62,2.25,.05))

# Alternatives: actual open hex bore vs a short screw with modeled thread and slotted head.
g=group('06A_Nut_Alternative',(-4,1,0))
tube('Nut_hex_bore',(0,0,0),.50,.23,.34,'Steel',g,6)
tube('Nut_inner_lip',(0,0,.17),.27,.215,.055,'GoldLight',g,24)
g[1]['variants']='Use WM_Rust2 / WM_Rust1 / WM_Steel materials on the same mesh'
label('A / SOMUN',(-4.62,.25,.05))
g=group('06B_Screw_Alternative',(-2,1,0))
disc('Screw_shaft',(0,0,-.15),.19,.68,'Steel',g,12)
# A helical ridge, 3 turns, 72 radial steps, 3 vertices per ridge section.
v=[]; f=[]
for i in range(73):
    t=i*2*pi*3/72; z=-.47+i*.55/72
    for r,dz in [(.185,-.035),(.25,0),(.185,.035)]: v.append((r*cos(t),r*sin(t),z+dz))
for i in range(72):
    for j in range(3): f.append((i*3+j,i*3+(j+1)%3,(i+1)*3+(j+1)%3,(i+1)*3+j))
mesh('Screw_helical_thread',v,f,'Steel',g)
disc('Screw_head',(0,0,.24),.45,.22,'Steel',g,12)
box('Screw_slot',(0,0,.354),(.49,.095,.018),'DarkSteel',g,.015)
g[1].rotation_euler=(math.radians(58),math.radians(-12),math.radians(-20))
g[1]['variants']='WM_Rust2 / WM_Rust1 / WM_Steel'; label('B / VIDA',(-2.62,.25,.05))

g=group('07_Motor_3x2',(1,1,0))
box('Motor_housing',(0,0,0),(2.35,1.15,.58),'Brass',g,.13)
for x in [-.86,.86]:
    box('Steel_band',(x,0,.03),(.24,1.20,.66),'DarkSteel',g,.045)
    for y in [-.40,.40]: disc('Band_rivet',(x,y,.39),.085,.06,'GoldLight',g,8)
disc('Motor_rotor',(0,0,.40),.39,.17,'Steel',g,16)
disc('Motor_core',(0,0,.51),.20,.08,'Azure',g,12)
for i in range(5): disc('Charge_'+str(i),( -.40+i*.20,.40,.35),.052,.035,'Mint',g,8)
label('07 MOTOR / 3x2',(-.1,.15,.05))
g=group('08_Frozen_Grease',(4,1,0))
box('Ice_block',(0,0,0),(.87,.87,.45),'Ice',g,.10)
for j,(a,b) in enumerate([((-.24,.4,.235),(-.06,.1,.24)),((-.06,.1,.24),(.27,-.03,.24)),((-.06,.1,.24),(-.15,-.37,.24))]): stroke('Ice_crack_'+str(j),a,b,.024,'Cream',g)
label('08 FROZEN',(3.4,.25,.05))

g=group('09_Power_Rail',(-4,-1.2,0))
box('Rail_body',(0,0,0),(.34,.84,.33),'Azure',g,.12)
rings('Rail_nose',[(-.24,.34),(.24,.34),(0,.67)],[(1,0),(1,.24)],'GoldLight',g)
for x in [-.28,.28]: box('Rail_fin',(x,-.26,0),(.22,.32,.16),'Brass',g,.035)
box('Rail_band',(0,-.20,.04),(.40,.16,.36),'Cream',g,.035)
label('09 RAIL',(-4.6,-1.95,.05))
g=group('10_Power_Solvent',(-2,-1.2,0))
ball('Solvent_flask',(0,-.10,0),(.40,.40,.27),'Pink',g)
box('Solvent_neck',(0,.27,0),(.26,.27,.25),'Pink',g,.06)
box('Solvent_cap',(0,.43,0),(.34,.16,.30),'GoldLight',g,.045)
disc('Solvent_badge',(0,-.08,.25),.17,.035,'Cream',g,12)
label('10 SOLVENT',(-2.65,-1.95,.05))
g=group('11_Power_Magnet',(0,-1.2,0))
outline=[(-.43,.42),(-.18,.42),(-.18,-.10),(-.10,-.23),(.10,-.23),(.18,-.10),(.18,.42),(.43,.42),(.43,-.14),(.25,-.43),(-.25,-.43),(-.43,-.14)]
outline.reverse(); rings('Magnet_U',outline,[(.96,-.15),(1,-.10),(1,.10),(.96,.15)],'Cherry',g)
for x in [-.305,.305]: box('Magnet_tip',(x,.37,0),(.255,.21,.32),'Steel',g,.03)
label('11 MAGNET',(-.63,-1.95,.05))
g=group('12_Board_Cell',(2,-1.2,0))
box('Cell_wood',(0,0,-.08),(.96,.96,.15),'Wood',g,.06)
box('Cell_inlay',(0,0,.015),(.80,.80,.04),'Brass',g,.045)
box('Cell_surface',(0,0,.05),(.72,.72,.045),'Wood',g,.025)
label('12 CELL',(1.4,-1.95,.05))
g=group('13_Hammer_Optional',(4,-1.2,0))
box('Hammer_grip',(0,-.15,0),(.20,.76,.22),'Wood',g,.07)
box('Hammer_head',(0,.27,0),(.85,.34,.34),'Azure',g,.08)
for x in [-.37,.37]: box('Hammer_cap',(x,.27,0),(.14,.38,.38),'GoldLight',g,.035)
label('13 HAMMER',(3.35,-1.95,.05))

# Mechanical hero group; individual gear roots retain useful axial pivots.
g=group('14_Factory_Clockwork',(0,-4,0))
box('Machine_wood_backplate',(0,0,-.36),(5.0,1.78,.24),'Wood',g,.12)
box('Machine_brass_outline',(0,0,-.49),(5.18,1.94,.17),'Brass',g,.12)
gears=[]
def gear(name,p,r,teeth):
    outline=[]
    for i in range(teeth*4):
        a=2*pi*i/(teeth*4); rad=r*(1 if i%4 in [1,2] else .80)
        outline.append((rad*cos(a),rad*sin(a)))
    o=rings(name,outline,[(.94,-.15),(1,-.10),(1,.10),(.94,.16)],'GoldLight',g)
    o.location=p; gears.append(o)
    disc(name+'_blue_hub',(p[0],p[1],p[2]+.18),r*.32,.10,'Azure',g,16)
    disc(name+'_axle',(p[0],p[1],p[2]+.25),r*.17,.09,'Brass',g,12)
    return o
gear('Gear_12T',(-1.32,.06,0),.81,12)
gear('Gear_8T',(-.13,-.18,0),.54,8)
gear('Gear_10T',(.98,.04,0),.67,10)
gear('Gear_6T',(1.80,.55,-.04),.405,6)
for x in [-2.30,2.30]:
    for y in [-.70,.70]: disc('Plate_fastener',(x,y,-.16),.09,.10,'GoldLight',g,8)
for x in [-2.55,2.55]:
    box('Steam_pipe',(x,0,-.08),(.16,1.05,.18),'Steel',g,.07)
    disc('Steam_port',(x,.57,-.04),.14,.12,'DarkSteel',g,12)
tube('Bolt_docking_socket',(0,.66,-.12),.20,.10,.12,'Brass',g,16)
scene=bpy.context.scene; scene.frame_start=1; scene.frame_end=240; scene.render.fps=30
for i,o in enumerate(gears):
    o.rotation_euler.z=0; o.keyframe_insert(data_path='rotation_euler',index=2,frame=1)
    o.rotation_euler.z=(1 if i%2==0 else -1)*2*pi*(2 if i==0 else 3 if i==1 else 2.4 if i==2 else 4)
    o.keyframe_insert(data_path='rotation_euler',index=2,frame=241)
    action=o.animation_data.action
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    for k in fc.keyframe_points: k.interpolation='LINEAR'
scene.frame_set(1)
label('14 CLOCKWORK / animated 1-240',(-2.55,-5.40,.05),.20)
label('WORKSHOP MATCH / TOON LOW-POLY / REVIEW 02',(-4.65,4.20,.05),.27)
label('BLENDER ONLY  -  NOT APPROVED / NOT EXPORTED',(-4.65,3.80,.05),.13)

# Presentation only: no mesh exports, no Unity assets touched.
camera=bpy.data.objects.get('Camera'); camera.data.type='ORTHO'; camera.data.ortho_scale=12.9
camera.location=(0,-7,18); target=Vector((0,-.40,0)); camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
scene.camera=camera
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        sp=area.spaces.active; sp.shading.type='MATERIAL'; sp.overlay.show_overlays=False
        sp.region_3d.view_rotation=camera.rotation_euler.to_quaternion()
        sp.region_3d.view_distance=11.5; sp.region_3d.view_location=(0,-.5,0)
for o in bpy.context.selected_objects: o.select_set(False)
ROOT['scope']='Review-only meshes. Material colors, not baked image textures. No Unity export.'
bpy.ops.wm.save_as_mainfile(filepath='/Users/ali/MechanicalCrush/Art/WorkshopMatch_Toon_Review_v2.blend')
print('TOON_REVIEW_CREATED',len(ROOT.all_objects),'objects; saved outside Assets')

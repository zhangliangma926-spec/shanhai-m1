import bpy, math
from mathutils import Vector
from math import radians
# Clean factory scene and create a compact, readable low-poly spirit fox.
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
for m in bpy.data.materials: bpy.data.materials.remove(m)
def mat(name,color,metal=0.0,rough=.58,emit=0.0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 nt=m.node_tree;nt.nodes.clear();bs=nt.nodes.new('ShaderNodeBsdfPrincipled');out=nt.nodes.new('ShaderNodeOutputMaterial');nt.links.new(bs.outputs['BSDF'],out.inputs['Surface']);bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
 if emit: bs.inputs['Emission Color'].default_value=(*color,1);bs.inputs['Emission Strength'].default_value=emit
 return m
orange=mat('赤尾 · 绯红绒毛',(.62,.18,.085)); bright=mat('赤尾 · 金橘毛尖',(.95,.43,.12)); cream=mat('赤尾 · 奶金腹毛',(.94,.76,.48)); dark=mat('赤尾 · 深色爪鼻',(.12,.07,.055)); teal=mat('赤尾 · 灵玉青光',(.12,.72,.69),.35,.28,1.3); eye=mat('赤尾 · 暖金瞳',(.98,.76,.25),.1,.22,.35)
parts=[]
def sphere(name,loc,scale,material,seg=16,rings=10):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;o.data.materials.append(material);o.modifiers.clear();
 for p in o.data.polygons:p.use_smooth=False
 parts.append(o);return o
def cone(name,loc,radius,depth,material,verts=9,rotation=(0,0,0)):
 bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=radius,radius2=0,depth=depth,location=loc,rotation=rotation);o=bpy.context.object;o.name=name;o.data.materials.append(material);parts.append(o);return o
def torus(name,loc,major,minor,material,rotation=(0,0,0)):
 bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=20,minor_segments=6,location=loc,rotation=rotation);o=bpy.context.object;o.name=name;o.data.materials.append(material);parts.append(o);return o
# Body points forward along +Y. Rounded torso, shoulder ruff, belly and four legs.
sphere('身体',(0,0,.92),(.48,.72,.43),orange,20,12)
sphere('胸前奶金毛',(0,.37,.84),(.38,.36,.32),cream,16,10)
sphere('肩部鬃毛',(0,.42,1.15),(.50,.39,.38),bright,16,10)
for x in (-.30,.30):
 for y in (-.40,.33):
  sphere('灵狐四肢',(x,y,.42),(.16,.20,.40),orange,12,8)
  sphere('深色爪',(x,y+.025,.12),(.18,.22,.14),dark,12,8)
# Head, cheeks and muzzle.
sphere('头部',(0,.60,1.42),(.45,.42,.43),orange,20,12)
sphere('面颊',(0,.91,1.30),(.34,.25,.25),cream,16,10)
sphere('鼻尖',(0,1.12,1.36),(.11,.12,.095),dark,12,8)
for x in (-.205,.205):
 sphere('眼眶',(x,.88,1.51),(.115,.095,.14),cream,12,8)
 sphere('瞳',(x,.963,1.52),(.055,.045,.082),dark,12,8)
 sphere('眼神高光',(x-.018,.998,1.555),(.018,.016,.022),eye,8,6)
 # pointed upright ears, inner-color inset on forward surface
 cone('灵狐耳',(x,.53,1.82),.19,.46,orange,10,rotation=(radians(-7),0,radians(10 if x>0 else -10)))
 sphere('耳内绒',(x,.624,1.80),(.085,.035,.18),cream,10,6)
# Twin plume tail flowing behind the body with pale tips.
tail=sphere('蓬松灵尾',(0,-.84,1.13),(.46,.74,.48),bright,16,10);tail.rotation_euler[0]=radians(-22)
tip=sphere('灵尾浅色尖',(0,-1.36,1.32),(.32,.35,.32),cream,14,8);tip.rotation_euler[0]=radians(-22)
# Jade forehead crest, collar and three back spines.
sphere('额心灵玉',(0,.835,1.78),(.14,.09,.17),teal,12,8)
torus('青玉项圈',(0,.28,1.12),.36,.045,teal,rotation=(radians(90),0,0))
for i in range(3):
 cone('背脊灵晶',(0,-.26-i*.22,1.48-i*.035),.11,.27,teal,7,rotation=(radians(18),0,0))
# Join only visible creature meshes, preserving their material slots.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join(); beast=bpy.context.object;beast.name='RedTail_SpiritFox_v064';beast.data.name='RedTail_SpiritFox_Mesh_v064'
# Flat low-poly facets, apply transforms for predictable Unity dimensions.
for p in beast.data.polygons:p.use_smooth=False
bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
# Lighting and camera for a clear review image.
world=bpy.data.worlds.new('RedTail Preview World');world.use_nodes=True;wn=world.node_tree.nodes;wn.clear();bg=wn.new('ShaderNodeBackground');wo=wn.new('ShaderNodeOutputWorld');world.node_tree.links.new(bg.outputs[0],wo.inputs['Surface']);bg.inputs['Color'].default_value=(.065,.105,.15,1);bg.inputs['Strength'].default_value=.55;bpy.context.scene.world=world
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,0));floor=bpy.context.object;floor.name='Preview Ground';floor.data.materials.append(mat('Preview Ground',(.025,.09,.105)))
camd=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',camd);bpy.context.scene.collection.objects.link(cam);cam.location=(4,6,3.2);target=Vector((0,0,1.0));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();camd.type='ORTHO';camd.ortho_scale=4.6;bpy.context.scene.camera=cam
for name,loc,power,size in [('Key',(3,4,6),1200,4),('Fill',(-4,1,3),750,3),('Rim',(0,-4,4),1100,3)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);bpy.context.scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=1000;scene.render.resolution_y=760;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX';scene.render.filepath=r'D:\游戏开发\项目\山海驭灵录_M1\05_成品交付\预览\赤尾灵狐_Blender模型_v0.6.4.png'
bpy.ops.render.render(write_still=True)
# Save editable Blender source with preview stage retained. Export only the creature, excluding camera/lights/ground.
blend_path=r'D:\游戏开发\项目\山海驭灵录_M1\UnityProject_M1_v0.6.4\Assets\Art\Models\Blender_Custom\RedTailFox_v064\RedTail_SpiritFox_v064.blend'
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
bpy.ops.object.select_all(action='DESELECT');beast.select_set(True);bpy.context.view_layer.objects.active=beast
fbx_path=blend_path.replace('.blend','.fbx')
bpy.ops.export_scene.fbx(filepath=fbx_path,use_selection=True,apply_scale_options='FBX_SCALE_ALL',object_types={'MESH'},mesh_smooth_type='OFF',use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=False)
print('FOX_READY',blend_path,fbx_path,'DIMENSIONS',list(beast.dimensions),'VERTS',len(beast.data.vertices),'MATERIALS',[m.name for m in beast.data.materials])



import bpy
from mathutils import Vector
from math import radians
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
print('BOUNDS',obj.dimensions[:], 'MATERIALS',[(m.name if m else None) for m in obj.data.materials])
# Preview only in memory: never save back to source blend.
bpy.ops.object.mode_set(mode='OBJECT') if obj.mode!='OBJECT' else None
scene=bpy.context.scene
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1000; scene.render.resolution_y=760; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=False
scene.view_settings.view_transform='AgX'
world=bpy.data.worlds.new('PreviewWorld') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world=world; world.color=(0.08,0.12,0.17)
bounds=[obj.matrix_world @ Vector(c) for c in obj.bound_box]
center=sum(bounds, Vector((0,0,0)))/8.0
# camera from front-right elevated
camd=bpy.data.cameras.new('PreviewCamera'); cam=bpy.data.objects.new('PreviewCamera',camd); scene.collection.objects.link(cam)
cam.location=center+Vector((4,7,3.3)); target=center+Vector((0,0,0.2)); cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler(); camd.type='ORTHO'; camd.ortho_scale=max(obj.dimensions)*2.3; scene.camera=cam
for i,(loc,power,size) in enumerate([((4,-5,7),1800,5),((-5,-1,4),900,4),((0,5,5),1400,3)]):
 d=bpy.data.lights.new('PreviewLight'+str(i),'AREA'); d.energy=power; d.shape='DISK'; d.size=size; o=bpy.data.objects.new(d.name,d); scene.collection.objects.link(o); o.location=loc; o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
# ground
bpy.ops.mesh.primitive_plane_add(size=200,location=(center.x,center.y,0))
g=bpy.context.object; g.name='PreviewGround'; mat=bpy.data.materials.new('Preview Ground'); mat.diffuse_color=(.035,.095,.10,1); g.data.materials.append(mat)
scene.render.filepath=r'D:\游戏开发\项目\山海驭灵录_M1\05_成品交付\预览\岚角灵兽_Blender模型检查_v0.6.4.png'
bpy.ops.render.render(write_still=True)





import bpy
from pathlib import Path
import re
from bpy.app.handlers import persistent
from bpy_extras.io_utils import ExportHelper
from bpy.props import StringProperty
from bpy.types import Operator

bl_info = {
    "name": "Clone Dash - Nucleus Model 3 Extensions",
    "blender": (4, 30, 0),
    "category": "Object"
}

glTF_extension_name = "EXT_CloneDash_Model3"
WORKING_PLEASE_DONT_MESS_WITH_ME = False
def setBoneSlotStates(override = None):
    global WORKING_PLEASE_DONT_MESS_WITH_ME
    if WORKING_PLEASE_DONT_MESS_WITH_ME:
        return
    
    for object in bpy.data.objects:
        if object.type == "ARMATURE":
            for bone in object.pose.bones:
                if "M3B.ActiveSlot" in bone:
                    activeSlot = bone["M3B.ActiveSlot"]
                    # This means the bone supports slot animation.
                    # We iterate until we don't find a slot to determine how many slots exist.
                    howmanyslots = 0
                    while True:
                        index = howmanyslots + 1
                        name = "M3B.Slot" + str(index)
                        if name not in bone: break
                        
                        object = bone[name]
                        if override != None:
                            object.hide_viewport = override
                        else:
                            object.hide_viewport = index != activeSlot
                    
                        howmanyslots += 1

def writeModel3GLB(context, filepath, usefilepath):
    global WORKING_PLEASE_DONT_MESS_WITH_ME
    armature = context.object

    if usefilepath:
        if "CDModel3_FilePath" in armature:
            filepath = armature["CDModel3_FilePath"]
        else:
            raise Exception("No filepath available in the object.")
    else:
        armature["CDModel3_FilePath"] = filepath

    bpy.ops.object.select_all(action='DESELECT')
    setBoneSlotStates(False)
    WORKING_PLEASE_DONT_MESS_WITH_ME = True
    bpy.context.view_layer.update()

    armature.select_set(True)
    def select_children_recursive(parent):
        for child in parent.children:
            child.select_set(True)
            select_children_recursive(child)

    select_children_recursive(armature)

    bpy.ops.export_scene.gltf(
        export_extra_animations        = True,
        use_selection                  = True,
        filepath                       = filepath, 
        export_format                  = "GLB",
        export_hierarchy_flatten_bones = False,
        export_optimize_animation_size = False
    )

    WORKING_PLEASE_DONT_MESS_WITH_ME = False
    setBoneSlotStates()
    return {'FINISHED'}

class CDModel3_ExportModel3GLB(Operator, ExportHelper):
    """This appears in the tooltip of the operator and in the generated docs"""
    bl_idname = "cdmodel3.export" 
    bl_label = "Export Model3 .GLB"

    filename_ext = ".glb"

    filter_glob: StringProperty(
        default = "*.glb",
        options = {'HIDDEN'},
        maxlen = 255
    )

    def execute(self, context):
        return writeModel3GLB(context, self.filepath, False)

class CDModel3_ExportAutomatically(Operator):
    """This appears in the tooltip of the operator and in the generated docs"""
    bl_idname = "cdmodel3.export_to_saved" 
    bl_label = "Export to Armature Path"

    def execute(self, context):
        return writeModel3GLB(context, None, True)

class CDModel3_SetupBoneSlot(Operator):
    bl_idname = "cdmodel3.boneslotsetup" 
    bl_label = "Add Bone Slot"

    def execute(self, context):
        bone = context.selected_pose_bones_from_active_object[0]
        
        if "activeSlot" not in bone:
            bone["M3B.ActiveSlot"] = 1
            ui = bone.id_properties_ui("M3B.ActiveSlot")
            print(ui.as_dict())
            ui.update(description = "The current slot.")
        
        # figure out which slot we can take
        slotsAlready = 0
        
        for i in range(0, 10000):
            name = "M3B.Slot" + str(i + 1)
            if name not in bone:
                bone[name] = None
                ui = bone.id_properties_ui(name)
                ui.update(id_type = "OBJECT", description = "Slot Object #" + str(i + 1))
                break
        
        return {'FINISHED'}


class CDModel3_PT_Export(bpy.types.Panel):
    bl_idname = "CDModel3_PT_export"
    bl_label = "Export"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "CD Model3 Utils"

    @classmethod
    def poll(cls, context):
        return (context.object != None and context.object.type == "ARMATURE")

    def draw(self, context):
        layout = self.layout

        box = layout.box()
        box.operator("cdmodel3.export")
        
        if "CDModel3_FilePath" in context.object:
            box.operator("cdmodel3.export_to_saved")

class CDModel3_PT_BoneParams(bpy.types.Panel):
    bl_idname = "CDModel3_PT_BoneParams"
    bl_label = "CDModel3 - Bone Params"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Item"

    @classmethod
    def poll(cls, context):
        return (context.object != None and context.object.type == "ARMATURE" and context.object.mode == "POSE")

    def draw(self, context):
        layout = self.layout
        if len(context.selected_pose_bones_from_active_object) > 0:
            layout.operator("cdmodel3.boneslotsetup")


class CD_Model3GLTFProps(bpy.types.PropertyGroup):
    pass

classes = [
    # operators
    CDModel3_ExportModel3GLB,
    CDModel3_ExportAutomatically,
    CDModel3_SetupBoneSlot,

    # panels
    CDModel3_PT_Export,
    CDModel3_PT_BoneParams,
]

def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    
    bpy.utils.register_class(CD_Model3GLTFProps)
    bpy.types.Scene.CD_Model3GLTFProps = bpy.props.PointerProperty(type = CD_Model3GLTFProps)

def unregister():
    for cls in classes:
        bpy.utils.unregister_class(cls)
        
    bpy.utils.unregister_class(CD_Model3GLTFProps)
    del bpy.types.Scene.CD_Model3GLTFProps

if __name__ == "__main__":
    register()

class glTF2ExportUserExtension:
    def __init__(self):
        from io_scene_gltf2.io.com.gltf2_io_extensions import Extension
        self.Extension = Extension
        self.properties = bpy.context.scene.CD_Model3GLTFProps

    def extra_animation_manage(self, extra_samplers, obj_uuid, blender_object, blender_action, gltf_animation, export_settings):
        from io_scene_gltf2.io.com.gltf2_io import AnimationChannel, AnimationSampler, AnimationChannelTarget

        nodes = export_settings['vtree'].nodes
        for sampler in extra_samplers:
            match = re.match(r"pose\.bones\[\"(.*?(?=\"))\"\]\[\"(.*?(?=\"))\"\]", sampler[0])
            node = next(filter(lambda x: x[1].blender_type == 3 and x[1].blender_bone.name == match.group(1), nodes.items()))[1]

            blender_bone = node.blender_bone
            if "M3B.ActiveSlot" in blender_bone:
                if "NUCLEUS_model3_slots" not in node.node.extensions:
                    howmanyslots = 0
                    slotnodes = []
                    while True:
                        index = howmanyslots + 1
                        name = "M3B.Slot" + str(index)
                        if name not in blender_bone: break
                        
                        object = blender_bone[name]
                        object_node = next(filter(lambda x: x[1].blender_type == 1 and x[1].blender_object.name == object.name, nodes.items()))
                        slotnodes.append(object_node[1].node.mesh.name)
                        howmanyslots += 1

                    node.node.extensions["NUCLEUS_model3_slots"] = {
                        "slots": slotnodes
                    }

                channel = AnimationChannel(
                    extensions = {}, 
                    extras = None, 
                    sampler = sampler[1], 
                    target = AnimationChannelTarget(
                        extensions = {
                            "KHR_animation_pointer": {
                                "pointer": match.group(2)
                            }
                        }, 
                        extras = None, 
                        node = node.node, 
                        path = "pointer"
                    )
                )
                gltf_animation.channels.append(channel)

def append_function_unique(fn_list, fn):
    fn_name = fn.__name__
    fn_module = fn.__module__
    for i in range(len(fn_list) - 1, -1, -1):
        if fn_list[i].__name__ == fn_name and fn_list[i].__module__ == fn_module:
            del fn_list[i]
    fn_list.append(fn)

@persistent
def m3s_updateSlotPreview(dummy):
    setBoneSlotStates()
                        

append_function_unique(bpy.app.handlers.frame_change_post, m3s_updateSlotPreview)

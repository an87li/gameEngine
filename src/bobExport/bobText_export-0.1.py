#! BPY

#add-on information
bl_info = {
    "name": "BOB Exporter (text)",
    "description": "Export mesh data with UV's into Binary Object Format (Bob) text format",
    "author": "Bob Holcomb",
    "version": (2018, 3, 11),
    "blender": (2, 79, 0),
    "location": "File > Import-Export",
    "warning": "",
    "wiki_url": "",
    "tracker_url": "",
    "category": "Import-Export"}

import bpy
import math
import mathutils

import bpy_extras.io_utils
from bpy_extras.io_utils import ExportHelper,orientation_helper_factory, axis_conversion
from bpy.props import StringProperty, BoolProperty, EnumProperty
from bpy.types import Operator

BOBOrientationHelper = orientation_helper_factory(
    "BOBOrientationHelper", axis_forward='-Z', axis_up='Y'
)

###########################################################
#helper functions
###########################################################
def indent(num, str):
   padding = num * '   '
   return padding + str + '\n'
   
def r3d(v):
    return round(v[0],6), round(v[1],6), round(v[2],6)

def r2d(v):
    return round(v[0],6), round(v[1],6)
    
def grouper(seq, size):
    return (seq[pos:pos + size] for pos in range(0, len(seq), size))

def getBonesInOrder(skel):
   if(skel == None):
      return {}
      
   ret = []
   idx = 0
   rootBones = []
   for b in skel.bones:
      if(b.parent == None):
         rootBones.append(b)
         ret.append({'index': idx, 'bone': b})
         idx += 1
         #print("Adding root bone {}".format(b.name))
         
   for b in rootBones:
      print("Root bone {}".format(b.name))
      childBones = b.children_recursive
      for cb in childBones:
         ret.append({'index': idx, 'bone': cb})
         idx += 1
         #print("   Adding child bone {}".format(cb.name))
         
   #for b in ret:
      #print("index: {} bone: {}".format(b['index'], b['bone'].name))
      
   return ret;
      
def findBoneIndex(boneName, bonesInOrder):
   for b in bonesInOrder:
      if(b['bone'].name == boneName):
         return b['index']

   print("Failed to find bone {} in bone ordered list".format(boneName))
   raise

def matrixToString(mat):
   ret = ""
   for i in range(0, 4):
      for j in range(0, 4):
         ret += str(mat[i][j])
         if(i != 4 and j != 4):
            ret += ", "

   return ret


class Settings:
    __slots__ = (
        "global_matrix",
        "filepath",
        "context"
    )
    
    def __init__(self):
        filepath = ""
        context = bpy.context
        global_matrix = mathutils.Matrix.Identity(4)

###########################################################
#BOB chunk definitions
###########################################################
class Vertex:
   __slots__ = (
      "layout",
      "pos",
      "nor",
      "uv",
      "boneIndexes",
      "boneWeights"
   )
   
   def __init__(self, pos, nor, uv, bidx = None, bweights = None):
      if(bidx == None): 
         self.layout = "V3N3T2"
      else: 
         self.layout = "V3N3T2B4W4"
      self.pos = pos
      self.nor = nor
      self.uv = uv
      self.boneIndexes = bidx
      self.boneWeights = bweights

   def __repr__(self):
      if(self.boneIndexes != None):
         return "{:6f}, {:6f}, {:6f},      {:6f}, {:6f}, {:6f},      {:6f}, {:6f},      {}, {}, {}, {},      {:6f}, {:6f}, {:6f}, {:6f},".format(
         self.pos[0], self.pos[1], self.pos[2], 
         self.nor[0], self.nor[1], self.nor[2], 
         self.uv[0], self.uv[1],
         self.boneIndexes[0], self.boneIndexes[1], self.boneIndexes[2], self.boneIndexes[3],
         self.boneWeights[0], self.boneWeights[1], self.boneWeights[2], self.boneWeights[3])
      else:
         return "{:6f}, {:6f}, {:6f},      {:6f}, {:6f}, {:6f},      {:6f}, {:6f},".format(
         self.pos[0], self.pos[1], self.pos[2], 
         self.nor[0], self.nor[1], self.nor[2], 
         self.uv[0], self.uv[1])
         
   def __eq__(self, other):
      if(self.pos != other.pos): return False
      if(self.nor != other.nor): return False
      if(self.uv != other.uv): return False
      if(self.boneIndexes != other.boneIndexes): return false;
      if(self.boneWeights != other.boneWeights): return false;
      
      return True
      
   def __ne__(self, other):
      return not self.__eq__(other)

class BOBChunk:
   __slots__= (
      "type",
      "name",
      "version",
      "flags"
   )

   def __init__(self):
      self.type=""
      self.name =""
      self.version = 1
      self.flags = 1

   def write(self, file):
      file.write(indent(3, "type = \"{}\";".format(self.type)))
      file.write(indent(3, "name = \"{}\";".format(self.name)))
      file.write(indent(3, "version = {};".format(self.version)))
      file.write(indent(3, "flags = {};".format(self.flags)))

class BOBMaterial:
   __slots__ = (
      "name",
      "ambient",
      "diffuse",
      "spec", 
      "emission",
      "shininess",
      "alpha",
      "diffuseTexture",
      "specularTexture",
      "normalTexture",
      "emissionTexture",
      "alphaTexture"
   )
   
   def __init__(self):
      self.name = ""
      self.ambient = []
      self.diffuse = []
      self.spec = []
      self.emission = []
      self.shininess = 0
      self.alpha = 0
      self.diffuseTexture = ""
      self.specularTexture = ""
      self.normalTexture = ""
      self.emissionTexture = ""
      self.alphaTexture = ""
   
   def parse(self, m):
      self.name = m.name
      self.ambient = [m.ambient, m.ambient, m.ambient]
      self.diffuse= m.diffuse_intensity * m.diffuse_color
      self.spec = m.specular_intensity * m.specular_color
      self.emission = m.emit * m.diffuse_color
      self.shininess = m.specular_hardness
      self.alpha = m.alpha
      
      for tex in m.texture_slots:
         if tex and tex.use and tex.texture.type  == "IMAGE":
            texName = tex.texture.image.filepath.strip('/')
            if(tex.use_map_color_diffuse or tex.use_map_diffuse):
               self.diffuseTexture = texName
            elif(tex.use_map_color_spec or tex.use_map_specular):
               self.specularTexture = texName
            elif(tex.use_map_normal):
               self.normalTexture = texName
            elif(tex.use_map_emit or tex.use_map_emission):
               self.emissionTexture = texName
            elif(tex.use_map_translucency or tex.use_map_alpha):
               self.alphaTexture = texName

   def write(self, file):
      file.write(indent(4, "{"))
      file.write(indent(5, "name = \"{}\";".format(self.name)))
      file.write(indent(5, "ambient = {{ r={:.4f}, g={:.4f}, b={:.4f} }};".format(*(self.ambient))))
      file.write(indent(5, "diffuse = {{ r={:.4f}, g={:.4f}, b={:.4f} }};".format(*(self.diffuse))))
      file.write(indent(5, "spec = {{ r={:.4f}, g={:.4f}, b={:.4f} }};".format(*(self.spec))))
      file.write(indent(5, "emission = {{ r={:.4f}, g={:.4f}, b={:.4f} }};".format(*(self.emission))))
      file.write(indent(5, "shininess = {:.4f};".format(self.shininess)))
      file.write(indent(5, "alpha = {:.4f};".format(self.alpha)))
      if(self.diffuseTexture != ""):
         file.write(indent(5, "diffuseTexture = \"{}\";".format(self.diffuseTexture)))
      if(self.specularTexture != ""):
         file.write(indent(5, "specularTexture = \"{}\";".format(self.specularTexture)))
      if(self.normalTexture != ""):
         file.write(indent(5, "normalTexture = \"{}\";".format(self.normalTexture)))
      if(self.emissionTexture != ""):
         file.write(indent(5, "emissionTexture = \"{}\";".format(self.emissionTexture)))
      if(self.alphaTexture != ""):
         file.write(indent(5, "alphaTexture = \"{}\";".format(self.alphaTexture)))
      file.write(indent(4, "};"))

class BOBMesh:
   __slots__ = (
      "material",
      "indexStart",
      "indexCount", 
   )
   
   def __init__(self):
      self.material = ""
      self.indexStart = 0
      self.indexCount = 0
      
   def write(self, file):
      file.write(indent(4, "{"))
      file.write(indent(5, "material = \"{}\";".format(self.material)))
      file.write(indent(5, "indexStart = {};".format(self.indexStart)))
      file.write(indent(5, "indexCount = {};".format(self.indexCount)))
      file.write(indent(4, "};"))
  
class BOBModel(BOBChunk):
   __slots__= (
      "primativeType",
      "vertexFormat",
      "indexFormat",
      "vertexCount",
      "indexCount",
      "meshes",
      "materials",
      "verts",
      "indexes",
      "skeleton"
   )
   
   def __init__(self):
      BOBChunk.__init__(self)
      self.type = "model"
      self.primativeType = "TRI"
      self.vertexFormat = "V3N3T2"
      self.indexFormat = "UInt16"
      self.indexCount =0
      self.vertexCount=0
      self.meshes=[]
      self.materials = []
      self.verts = []
      self.indexes = []
      self.skeleton = ""
      
   def addVertex(self, vert):
      index = [n for n,x in enumerate(self.verts) if vert == x]
      #index = None
      if( not index):
         self.verts.append(vert)
         return len(self.verts) - 1
         
      return index[0]

   def parse(self, obj, settings):
      #does the model have an armature parent
      skeleton = obj.find_armature()
      
      #create a temp copy to work on
      # We don't apply modifiers for a skinned mesh because we need the vertex positions
      # before they are deformed by the armature modifier in order to export the proper
      # bind pose. This does mean that modifiers preceding the armature modifier are ignored,
      # but the Blender API does not provide a reasonable way to retrieve the mesh at an
      # arbitrary stage in the modifier stack.
      
      mesh = obj.to_mesh(settings.context.scene ,skeleton == None, "PREVIEW")
         
      #rotate to our selected space
      mesh.transform(settings.global_matrix)

      self.name = obj.name
      matIndexes=[]
      bones = None
      
      #get the bones for this model
      if(skeleton != None):
         self.skeleton = skeleton.name
         bones = getBonesInOrder(skeleton.data)

      #get the mesh data structs
      modUV = mesh.uv_layers.active.data
      modVert = mesh.vertices
      modFaces = mesh.polygons
      modLoops = mesh.loops
      modGroups = obj.vertex_groups
      
      #parse the materials
      for mat in mesh.materials:
         m = BOBMaterial()
         m.parse(mat)
         self.materials.append(m)
         matIndexes.append([]) #add an empty array for faces of this material
         #print("Added material: " + m.name)
      
      #break model into meshes by material
      for face in modFaces:
         #error if it's not a triangle face
         if(face.loop_total != 3):
            print("Face is not a triangle")
            raise
            
         #find index list by material
         indexList = matIndexes[face.material_index]
         
         #build the vertex and add the indexes to the material index list
         for loop_index in range(face.loop_start, face.loop_start + face.loop_total):
            vert_index = modLoops[loop_index].vertex_index
            pos = modVert[vert_index].co
            nor = modVert[vert_index].normal
            uv = modUV[loop_index].uv
            #uv[1] = 1.0 - uv[1] #flip the meaning of the Y axis since blender texture origin is top left and openGL is bottom left
            
            idx = 0
            if(skeleton != None):
               bids = []
               bweights = []
               for vgroup in modVert[vert_index].groups:
                  if(vgroup.weight > 0 and len(bids) < 4):
                     bids.append(findBoneIndex(modGroups[vgroup.group].name, bones))
                     bweights.append(vgroup.weight)
               
               #add zero weight bone ids to fill out data struct
               while(len(bids) < 4):
                  bids.append(0)
                  bweights.append(0)
                     
               idx = self.addVertex(Vertex(pos, nor, uv, bids, bweights))
            else:
               idx = self.addVertex(Vertex(pos, nor, uv)) #should return index of vert in the vert list even if its already added
            indexList.append(idx)
            
      #create the mesh data structures based on the material indexes
      for idx, val in enumerate(matIndexes):
         mesh = BOBMesh()
         mesh.material = self.materials[idx].name
         mesh.indexStart = len(self.indexes)
         mesh.indexCount = len(val)
         for i in val:
            self.indexes.append(i)
         self.meshes.append(mesh)
      
      #get some numbers about verts/index
      self.vertexCount = len(self.verts)
      self.indexCount = len(self.indexes)
      
      if(self.vertexCount >= 65535 ): #do we need to use 32 bit ints to describe indexes
         self.indexFormat = "UInt32"
      
      #cleanup our copy
      del mesh

   def write(self, file):
      file.write(indent(2, "{"))
      BOBChunk.write(self, file) #call base class
      file.write(indent(3, "primativeType = \"TRI\";"))
      file.write(indent(3, "vertexFormat = \"{}\";".format(self.verts[0].layout))) #all verts should use the same layout
      file.write(indent(3, "indexFormat = \"UInt16\";"))
      file.write(indent(3, "vertexCount = {};".format(self.vertexCount)))
      file.write(indent(3, "indexCount = {};".format(self.indexCount)))
      if(self.verts[0].layout == "V3N3T2B4B4"):
         file.write(indent(3, "skeleton = \"{}\";".format(self.skeleton)))
      
      file.write(indent(3, "meshes={"))
      for m in self.meshes:
         m.write(file)
      file.write(indent(3, "};"))
      
      file.write(indent(3, "materials = {"))
      for m in self.materials:
         m.write(file)
      file.write(indent(3, "};"))
      
      file.write(indent(3, "verts={"))
      for v in self.verts:
         file.write(indent(4, str(v)))
      file.write(indent(3, "};"))
      
      file.write(indent(3, "indexes={"))
      for idxGroup in grouper(self.indexes, 3):
         file.write(indent(4, "{},  {},  {}, ".format(idxGroup[0] ,idxGroup[1], idxGroup[2]))) 
      file.write(indent(3, "};"))
      
      file.write(indent(2, "};"))

class Bone:
   __slots__ = (
      "name",
      "parent",
      "matrix"
   )

   def __init__(self):
      self.name = ""
      self.parent = ""
      self.matrix = []
      
   def parse(self, bone):
      self.name = bone.name
      if(bone.parent != None):
         self.parent = bone.parent.name
      else:
         self.parent = ""
   
   def write(self, file):
      file.write(indent(4, "{"))
      file.write(indent(5, "name = \"{}\";".format(self.name)))
      file.write(indent(5, "parent = \"{}\";".format(self.parent)))
      file.write(indent(5, "matrix = {{ {} }};".format(matrixToString(self.matrix))))
      file.write(indent(4, "};"))
  
class BOBSkeleton(BOBChunk):
   __slots__ = (
      "id",
      "bones"
   )

   def __init__(self):
      BOBChunk.__init__(self)
      self.type="skeleton"
      self.id = ""
      self.bones = []

   def parse(self, obj, settings):
      arm = obj.data.copy()
      arm.transform(settings.global_matrix)

      self.name = obj.name
      self.id = obj.name
      bonesInOrder = getBonesInOrder(arm)
      for b in bonesInOrder:
         bone = Bone()
         bone.parse(b['bone'])
         bone.matrix = b['bone'].matrix_local
         self.bones.append(bone)
         
      #cleanup our copy
      del arm

   def write(self, file):
      file.write(indent(2, "{"))
      BOBChunk.write(self, file) #call base class
      file.write(indent(3, "id = \"{}\";".format(self.id)))
      file.write(indent(3, "bones = {"))
      for b in self.bones:
         b.write(file)

      file.write(indent(3, "};"))
      file.write(indent(2, "};"))

class BOBFile:
   __slots__ = (
      "version",
      "registry",
      "chunks"
   )

   def __init__(self):
      self.version = 1
      self.registry = {}
      self.chunks = []
      
   def addChunk(self, chunk):
      self.chunks.append(chunk)
      self.registry[chunk.name] = len(self.chunks) - 1 #use a 0 based array index
      
   def write(self, file):
      file.write(indent(0, "--Binary OBject file export from Blender"))
      file.write(indent(0, "BOB = {"))
      file.write(indent(1, "version = 1;"))
      file.write(indent(1, "registry = {"))
      for k,v in self.registry.items():
         file.write(indent(2, "--{} = \"{}\";".format(v, k)))
      file.write(indent(1, "};"))
      file.write(indent(1, "chunks = {"))
      
      for c in self.chunks:
         c.write(file)
      
      file.write(indent(1, "};"))
      file.write(indent(0, "}"))

###########################################################
#Export MESH object. By default export whole scene
###########################################################

def exportBOB(settings):
   scene = settings.context.scene
   bob = BOBFile()
   
   for o in scene.objects:
      if(o.type == "MESH"):
         model = BOBModel()
         model.parse(o, settings)
         bob.addChunk(model)

      if(o.type == "ARMATURE"):
         skeleton = BOBSkeleton()
         skeleton.parse(o, settings)
         bob.addChunk(skeleton)

   #open the output file
   file = open(settings.filepath, "w", newline="\n")
   
   #write the file
   bob.write(file)
   
   #close the file
   file.close()

###########################################################
#export operator for drawing the export menu
###########################################################
class ExportBOB(bpy.types.Operator, ExportHelper, BOBOrientationHelper):
    '''Export a whole scene or single object as a BOB file with normals and texture coordinates'''
    bl_idname = "export.bob"
    bl_label = "Export BOB"

    filename_ext = ".bob"
    filter_glob = StringProperty(default="*.bob", options={'HIDDEN'})
    entire_scene = BoolProperty(name="Entire Scene", description="Export all MESH object (Entire scene)", default=True)

    @classmethod
    def poll(cls, context):
        return context.active_object != None

    def execute(self, context):
        settings = Settings()
        settings.context = context
        
        filepath = self.filepath
        filepath = bpy.path.ensure_ext(filepath, self.filename_ext)
        settings.filepath = filepath
        
        #rotate everything so that it is -Z forward and Y up (my game engine format)
        settings.global_matrix = bpy_extras.io_utils.axis_conversion(
            to_forward=self.axis_forward,
            to_up=self.axis_up).to_4x4()
        
        exportBOB(settings)
        return {'FINISHED'}

    def draw(self, context):
        layout = self.layout

        row = layout.row()
        row.prop(self, "entire_scene")
        
        col = layout.box().column(align=True)
        col.label('Axis Conversion:', icon='MANIPUL')
        col.prop(self, 'axis_up')
        col.prop(self, 'axis_forward')

###########################################################
#add-on registration/unregistration functions
###########################################################
def menu_func_export(self, context):
    self.layout.operator(ExportBOB.bl_idname, text="BOB file (.bob)")


def register():
    bpy.utils.register_module(__name__)

    bpy.types.INFO_MT_file_export.append(menu_func_export)


def unregister():
    bpy.utils.unregister_module(__name__)

    bpy.types.INFO_MT_file_export.remove(menu_func_export)

if __name__ == "__main__":
    register()
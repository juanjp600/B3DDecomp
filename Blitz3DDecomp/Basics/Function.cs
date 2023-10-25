using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

namespace Blitz3DDecomp;

sealed class Function
{
    public sealed class AssemblySection
    {
        public readonly Function Owner;
        public readonly string Name;
        public readonly List<Instruction> Instructions = new List<Instruction>();

        public AssemblySection(Function owner, string name)
        {
            Owner = owner;
            Name = name;
        }

        public IEnumerable<GlobalVariable> ReferencedGlobals
            => ReferencedVariables
                .OfType<GlobalVariable>();

        public IEnumerable<Variable> ReferencedVariables
            => Instructions
                .SelectMany(i => new[] { i.LeftArg, i.RightArg })
                .Select(s => s.StripDeref())
                .Select(Owner.InstructionArgumentToVariable)
                .OfType<Variable>()
                .Distinct(); // Removes null entries

        public void CleanupNop()
        {
            var nopIndices = Instructions
                .Select((instr, index) => instr.Name == "nop" ? index : -1)
                .Where(index => index >= 0)
                .Reverse()
                .ToArray();

            foreach (var instruction in Instructions)
            {
                if (instruction.CallParameterAssignmentIndices is not { } callParameterAssignmentIndices) { continue; }

                for (int i = 0; i < callParameterAssignmentIndices.Length; i++)
                {
                    callParameterAssignmentIndices[i] -=
                        nopIndices.Count(index => index <= callParameterAssignmentIndices[i]);
                }
            }

            Instructions.RemoveAll(instr => instr.Name == "nop");
        }
    }

    public sealed class Parameter : Variable
    {
        public readonly int Index;

        public Parameter(string name, int index) : base(name)
        {
            Index = index;
        }

        public override string ToInstructionArg()
            => $"[ebp+0x{((Index << 2) + 0x14):x1}]";
    }

    public sealed class LocalVariable : Variable
    {
        public readonly int Index;

        public LocalVariable(string name, int index) : base(name)
        {
            Index = index;
        }

        public override string ToInstructionArg()
            => $"[ebp-0x{((Index << 2) + 0x4):x1}]";
    }

    public readonly string Name;
    public readonly Dictionary<string, AssemblySection> AssemblySections;

    public int TotalInstructionCount => AssemblySections.Values.Select(s => s.Instructions.Count).Sum();

    public static readonly List<Function> AllFunctions = new List<Function>();

    private static Dictionary<string, Function> lookupDictionary = new Dictionary<string, Function>();

    public static void InitBuiltIn()
    {
        rtSym("RuntimeStats");
        rtSym("%LoadSound$filename");
        rtSym("FreeSound%sound");
        rtSym("LoopSound%sound");
        rtSym("SoundPitch%sound%pitch");
        rtSym("SoundVolume%sound#volume");
        rtSym("SoundPan%sound#pan");
        rtSym("%PlaySound%sound");
        rtSym("%PlayMusic$midifile%mode=0");
        rtSym("%PlayCDTrack%track%mode=1");
        rtSym("StopChannel%channel");
        rtSym("PauseChannel%channel");
        rtSym("ResumeChannel%channel");
        rtSym("ChannelPitch%channel%pitch");
        rtSym("ChannelVolume%channel#volume");
        rtSym("ChannelPan%channel#pan");
        rtSym("%ChannelPlaying%channel");
        rtSym("%Load3DSound$filename");
        rtSym("%CreateBank%size=0");
        rtSym("FreeBank%bank");
        rtSym("%BankSize%bank");
        rtSym("ResizeBank%bank%size");
        rtSym("CopyBank%src_bank%src_offset%dest_bank%dest_offset%count");
        rtSym("%PeekByte%bank%offset");
        rtSym("%PeekShort%bank%offset");
        rtSym("%PeekInt%bank%offset");
        rtSym("#PeekFloat%bank%offset");
        rtSym("PokeByte%bank%offset%value");
        rtSym("PokeShort%bank%offset%value");
        rtSym("PokeInt%bank%offset%value");
        rtSym("PokeFloat%bank%offset#value");
        rtSym("%ReadBytes%bank%file%offset%count");
        rtSym("%WriteBytes%bank%file%offset%count");
        rtSym("%CallDLL$dll_name$func_name%in_bank=0%out_bank=0");
        rtSym("LoaderMatrix$file_ext#xx#xy#xz#yx#yy#yz#zx#zy#zz");
        rtSym("HWMultiTex%enable");
        rtSym("%HWTexUnits");
        rtSym("%GfxDriverCaps3D");
        rtSym("WBuffer%enable");
        rtSym("Dither%enable");
        rtSym("AntiAlias%enable");
        rtSym("WireFrame%enable");
        rtSym("AmbientLight#red#green#blue");
        rtSym("ClearCollisions");
        rtSym("Collisions%source_type%destination_type%method%response");
        rtSym("UpdateWorld#elapsed_time=1");
        rtSym("CaptureWorld");
        rtSym("RenderWorld#tween=1");
        rtSym("ClearWorld%entities=1%brushes=1%textures=1");
        rtSym("%ActiveTextures");
        rtSym("%TrisRendered");
        rtSym("#Stats3D%type");
        rtSym("%CreateTexture%width%height%flags=0%frames=1");
        rtSym("%LoadTexture$file%flags=1");
        rtSym("%LoadAnimTexture$file%flags%width%height%first%count");
        rtSym("FreeTexture%texture");
        rtSym("TextureBlend%texture%blend");
        rtSym("TextureCoords%texture%coords");
        rtSym("TextureBumpEnvMat%texture%x%y#envmat");
        rtSym("TextureBumpEnvScale%texture#envmat");
        rtSym("TextureBumpEnvOffset%texture#envoffset");
        rtSym("ScaleTexture%texture#u_scale#v_scale");
        rtSym("RotateTexture%texture#angle");
        rtSym("PositionTexture%texture#u_offset#v_offset");
        rtSym("TextureLodBias#bias");
        rtSym("TextureAnisotropic%level");
        rtSym("%TextureWidth%texture");
        rtSym("%TextureHeight%texture");
        rtSym("$TextureName%texture");
        rtSym("SetCubeFace%texture%face");
        rtSym("SetCubeMode%texture%mode");
        rtSym("%TextureBuffer%texture%frame=0");
        rtSym("ClearTextureFilters");
        rtSym("TextureFilter$match_text%texture_flags=0");
        rtSym("%CreateBrush#red=255#green=255#blue=255");
        rtSym("%LoadBrush$file%texture_flags=1#u_scale=1#v_scale=1");
        rtSym("FreeBrush%brush");
        rtSym("BrushColor%brush#red#green#blue");
        rtSym("BrushAlpha%brush#alpha");
        rtSym("BrushShininess%brush#shininess");
        rtSym("BrushTexture%brush%texture%frame=0%index=0");
        rtSym("%GetBrushTexture%brush%index=0");
        rtSym("BrushBlend%brush%blend");
        rtSym("BrushFX%brush%fx");
        rtSym("%LoadMesh$file%parent=0");
        rtSym("%LoadAnimMesh$file%parent=0");
        rtSym("%LoadAnimSeq%entity$file");
        rtSym("%CreateMesh%parent=0");
        rtSym("%CreateCube%parent=0");
        rtSym("%CreateSphere%segments=8%parent=0");
        rtSym("%CreateCylinder%segments=8%solid=1%parent=0");
        rtSym("%CreateCone%segments=8%solid=1%parent=0");
        rtSym("%CopyMesh%mesh%parent=0");
        rtSym("ScaleMesh%mesh#x_scale#y_scale#z_scale");
        rtSym("RotateMesh%mesh#pitch#yaw#roll");
        rtSym("PositionMesh%mesh#x#y#z");
        rtSym("FitMesh%mesh#x#y#z#width#height#depth%uniform=0");
        rtSym("FlipMesh%mesh");
        rtSym("PaintMesh%mesh%brush");
        rtSym("AddMesh%source_mesh%dest_mesh");
        rtSym("UpdateNormals%mesh");
        rtSym("LightMesh%mesh#red#green#blue#range=0#x=0#y=0#z=0");
        rtSym("#MeshWidth%mesh");
        rtSym("#MeshHeight%mesh");
        rtSym("#MeshDepth%mesh");
        rtSym("%MeshesIntersect%mesh_a%mesh_b");
        rtSym("%CountSurfaces%mesh");
        rtSym("%GetSurface%mesh%surface_index");
        rtSym("MeshCullBox%mesh#x#y#z#width#height#depth");
        rtSym("%CreateSurface%mesh%brush=0");
        rtSym("%GetSurfaceBrush%surface");
        rtSym("%GetEntityBrush%entity");
        rtSym("%FindSurface%mesh%brush");
        rtSym("ClearSurface%surface%clear_vertices=1%clear_triangles=1");
        rtSym("PaintSurface%surface%brush");
        rtSym("%AddVertex%surface#x#y#z#u=0#v=0#w=1");
        rtSym("%AddTriangle%surface%v0%v1%v2");
        rtSym("VertexCoords%surface%index#x#y#z");
        rtSym("VertexNormal%surface%index#nx#ny#nz");
        rtSym("VertexColor%surface%index#red#green#blue#alpha=1");
        rtSym("VertexTexCoords%surface%index#u#v#w=1%coord_set=0");
        rtSym("%CountVertices%surface");
        rtSym("%CountTriangles%surface");
        rtSym("#VertexX%surface%index");
        rtSym("#VertexY%surface%index");
        rtSym("#VertexZ%surface%index");
        rtSym("#VertexNX%surface%index");
        rtSym("#VertexNY%surface%index");
        rtSym("#VertexNZ%surface%index");
        rtSym("#VertexRed%surface%index");
        rtSym("#VertexGreen%surface%index");
        rtSym("#VertexBlue%surface%index");
        rtSym("#VertexAlpha%surface%index");
        rtSym("#VertexU%surface%index%coord_set=0");
        rtSym("#VertexV%surface%index%coord_set=0");
        rtSym("#VertexW%surface%index%coord_set=0");
        rtSym("%TriangleVertex%surface%index%vertex");
        rtSym("%CreateCamera%parent=0");
        rtSym("CameraZoom%camera#zoom");
        rtSym("CameraRange%camera#near#far");
        rtSym("#GetCameraRangeNear%camera");
        rtSym("#GetCameraRangeFar%camera");
        rtSym("CameraClsColor%camera#red#green#blue");
        rtSym("CameraClsMode%camera%cls_color%cls_zbuffer");
        rtSym("CameraProjMode%camera%mode");
        rtSym("CameraViewport%camera%x%y%width%height");
        rtSym("CameraFogColor%camera#red#green#blue");
        rtSym("CameraFogRange%camera#near#far");
        rtSym("#GetCameraFogRangeNear%camera");
        rtSym("#GetCameraFogRangeFar%camera");
        rtSym("CameraFogDensity%camera#density");
        rtSym("CameraFogMode%camera%mode");
        rtSym("CameraProject%camera#x#y#z");
        rtSym("#ProjectedX");
        rtSym("#ProjectedY");
        rtSym("#ProjectedZ");
        rtSym("%EntityInView%entity%camera");
        rtSym("%EntityVisible%src_entity%dest_entity");
        rtSym("%EntityPick%entity#range");
        rtSym("%LinePick#x#y#z#dx#dy#dz#radius=0");
        rtSym("%CameraPick%camera#viewport_x#viewport_y");
        rtSym("#PickedX");
        rtSym("#PickedY");
        rtSym("#PickedZ");
        rtSym("#PickedNX");
        rtSym("#PickedNY");
        rtSym("#PickedNZ");
        rtSym("#PickedTime");
        rtSym("%PickedEntity");
        rtSym("%PickedSurface");
        rtSym("%PickedTriangle");
        rtSym("%CreateLight%type=1%parent=0");
        rtSym("LightColor%light#red#green#blue");
        rtSym("LightRange%light#range");
        rtSym("LightConeAngles%light#inner_angle#outer_angle");
        rtSym("%CreatePivot%parent=0");
        rtSym("%CreateSprite%parent=0");
        rtSym("%LoadSprite$file%texture_flags=1%parent=0");
        rtSym("RotateSprite%sprite#angle");
        rtSym("ScaleSprite%sprite#x_scale#y_scale");
        rtSym("HandleSprite%sprite#x_handle#y_handle");
        rtSym("SpriteViewMode%sprite%view_mode");
        rtSym("%LoadMD2$file%parent=0");
        rtSym("AnimateMD2%md2%mode=1#speed=1%first_frame=0%last_frame=9999#transition=0");
        rtSym("#MD2AnimTime%md2");
        rtSym("%MD2AnimLength%md2");
        rtSym("%MD2Animating%md2");
        rtSym("%LoadBSP$file#gamma_adj=0%parent=0");
        rtSym("BSPLighting%bsp%use_lightmaps");
        rtSym("BSPAmbientLight%bsp#red#green#blue");
        rtSym("%CreateMirror%parent=0");
        rtSym("%CreatePlane%segments=1%parent=0");
        rtSym("%CreateTerrain%grid_size%parent=0");
        rtSym("%LoadTerrain$heightmap_file%parent=0");
        rtSym("TerrainDetail%terrain%detail_level%morph=0");
        rtSym("TerrainShading%terrain%enable");
        rtSym("#TerrainX%terrain#world_x#world_y#world_z");
        rtSym("#TerrainY%terrain#world_x#world_y#world_z");
        rtSym("#TerrainZ%terrain#world_x#world_y#world_z");
        rtSym("%TerrainSize%terrain");
        rtSym("#TerrainHeight%terrain%terrain_x%terrain_z");
        rtSym("ModifyTerrain%terrain%terrain_x%terrain_z#height%realtime=0");
        rtSym("%CreateListener%parent#rolloff_factor=1#doppler_scale=1#distance_scale=1");
        rtSym("%EmitSound%sound%entity");
        rtSym("%CopyEntity%entity%parent=0");
        rtSym("#EntityX%entity%global=0");
        rtSym("#EntityY%entity%global=0");
        rtSym("#EntityZ%entity%global=0");
        rtSym("#EntityPitch%entity%global=0");
        rtSym("#EntityYaw%entity%global=0");
        rtSym("#EntityRoll%entity%global=0");
        rtSym("#EntityScaleX%entity%global=0");
        rtSym("#EntityScaleY%entity%global=0");
        rtSym("#EntityScaleZ%entity%global=0");
        rtSym("#GetMatElement%entity%row%column");
        rtSym("TFormPoint#x#y#z%source_entity%dest_entity");
        rtSym("TFormVector#x#y#z%source_entity%dest_entity");
        rtSym("TFormNormal#x#y#z%source_entity%dest_entity");
        rtSym("#TFormedX");
        rtSym("#TFormedY");
        rtSym("#TFormedZ");
        rtSym("#VectorYaw#x#y#z");
        rtSym("#VectorPitch#x#y#z");
        rtSym("#DeltaPitch%src_entity%dest_entity");
        rtSym("#DeltaYaw%src_entity%dest_entity");
        rtSym("ResetEntity%entity");
        rtSym("EntityType%entity%collision_type%recursive=0");
        rtSym("EntityPickMode%entity%pick_geometry%obscurer=1");
        rtSym("%GetParent%entity");
        rtSym("%GetEntityType%entity");
        rtSym("EntityRadius%entity#x_radius#y_radius=0");
        rtSym("EntityBox%entity#x#y#z#width#height#depth");
        rtSym("#EntityDistance%source_entity%destination_entity");
        rtSym("#EntityDistanceSquared%source_entity%destination_entity");
        rtSym("%EntityCollided%entity%type");
        rtSym("%CountCollisions%entity");
        rtSym("#CollisionX%entity%collision_index");
        rtSym("#CollisionY%entity%collision_index");
        rtSym("#CollisionZ%entity%collision_index");
        rtSym("#CollisionNX%entity%collision_index");
        rtSym("#CollisionNY%entity%collision_index");
        rtSym("#CollisionNZ%entity%collision_index");
        rtSym("#CollisionTime%entity%collision_index");
        rtSym("%CollisionEntity%entity%collision_index");
        rtSym("%CollisionSurface%entity%collision_index");
        rtSym("%CollisionTriangle%entity%collision_index");
        rtSym("#Distance#x1#x2#y1#y2#z1=0#z2=0");
        rtSym("#DistanceSquared#x1#x2#y1#y2#z1=0#z2=0");
        rtSym("MoveEntity%entity#x#y#z");
        rtSym("TurnEntity%entity#pitch#yaw#roll%global=0");
        rtSym("TranslateEntity%entity#x#y#z%global=0");
        rtSym("PositionEntity%entity#x#y#z%global=0");
        rtSym("ScaleEntity%entity#x_scale#y_scale#z_scale%global=0");
        rtSym("RotateEntity%entity#pitch#yaw#roll%global=0");
        rtSym("PointEntity%entity%target#roll=0");
        rtSym("AlignToVector%entity#vector_x#vector_y#vector_z%axis#rate=1");
        rtSym("SetAnimTime%entity#time%anim_seq=0");
        rtSym("Animate%entity%mode=1#speed=1%sequence=0#transition=0");
        rtSym("SetAnimKey%entity%frame%pos_key=1%rot_key=1%scale_key=1");
        rtSym("%AddAnimSeq%entity%length");
        rtSym("%ExtractAnimSeq%entity%first_frame%last_frame%anim_seq=0");
        rtSym("%AnimSeq%entity");
        rtSym("#AnimTime%entity");
        rtSym("%AnimLength%entity");
        rtSym("%Animating%entity");
        rtSym("EntityParent%entity%parent%global=1");
        rtSym("%CountChildren%entity");
        rtSym("%GetChild%entity%index");
        rtSym("%FindChild%entity$name");
        rtSym("PaintEntity%entity%brush");
        rtSym("EntityColor%entity#red#green#blue");
        rtSym("EntityAlpha%entity#alpha");
        rtSym("EntityShininess%entity#shininess");
        rtSym("EntityTexture%entity%texture%frame=0%index=0");
        rtSym("EntityBlend%entity%blend");
        rtSym("EntityFX%entity%fx");
        rtSym("EntityAutoFade%entity#near#far");
        rtSym("EntityOrder%entity%order");
        rtSym("HideEntity%entity");
        rtSym("ShowEntity%entity");
        rtSym("FreeEntity%entity");
        rtSym("NameEntity%entity$name");
        rtSym("$EntityName%entity");
        rtSym("$EntityClass%entity");
        rtSym("%MemoryLoad");
        rtSym("%TotalPhys");
        rtSym("%AvailPhys");
        rtSym("%TotalVirtual");
        rtSym("%AvailVirtual");
        rtSym("%OpenFile$filename");
        rtSym("%ReadFile$filename");
        rtSym("%WriteFile$filename");
        rtSym("CloseFile%file_stream");
        rtSym("%FilePos%file_stream");
        rtSym("%SeekFile%file_stream%pos");
        rtSym("%ReadDir$dirname");
        rtSym("CloseDir%dir");
        rtSym("$NextFile%dir");
        rtSym("$CurrentDir");
        rtSym("ChangeDir$dir");
        rtSym("CreateDir$dir");
        rtSym("DeleteDir$dir");
        rtSym("%FileSize$file");
        rtSym("%FileType$file");
        rtSym("$FileExtension$file");
        rtSym("CopyFile$file$to");
        rtSym("DeleteFile$file");
        rtSym("%CountGfxDrivers");
        rtSym("$GfxDriverName%driver");
        rtSym("SetGfxDriver%driver");
        rtSym("%CountGfxModes");
        rtSym("%GfxModeExists%width%height%depth");
        rtSym("%GfxModeWidth%mode");
        rtSym("%GfxModeHeight%mode");
        rtSym("%GfxModeDepth%mode");
        rtSym("%AvailVidMem");
        rtSym("%TotalVidMem");
        rtSym("%GfxDriver3D%driver");
        rtSym("%CountGfxModes3D");
        rtSym("%GfxMode3DExists%width%height%depth");
        rtSym("%GfxMode3D%mode");
        rtSym("%Windowed3D");
        rtSym("Graphics%width%height%depth=0%mode=0");
        rtSym("Graphics3D%width%height%depth=0%mode=0");
        rtSym("EndGraphics");
        rtSym("%GraphicsLost");
        rtSym("%InFocus");
        rtSym("SetGamma%src_red%src_green%src_blue#dest_red#dest_green#dest_blue");
        rtSym("UpdateGamma%calibrate=0");
        rtSym("#GammaRed%red");
        rtSym("#GammaGreen%green");
        rtSym("#GammaBlue%blue");
        rtSym("%FrontBuffer");
        rtSym("%BackBuffer");
        rtSym("%ScanLine");
        rtSym("VWait%frames=1");
        rtSym("Flip%vwait=1");
        rtSym("%GraphicsWidth");
        rtSym("%GraphicsHeight");
        rtSym("%GraphicsDepth");
        rtSym("SetBuffer%buffer");
        rtSym("%GraphicsBuffer");
        rtSym("%LoadBuffer%buffer$bmpfile");
        rtSym("%SaveBuffer%buffer$bmpfile");
        rtSym("BufferDirty%buffer");
        rtSym("LockBuffer%buffer=0");
        rtSym("UnlockBuffer%buffer=0");
        rtSym("%ReadPixel%x%y%buffer=0");
        rtSym("WritePixel%x%y%argb%buffer=0");
        rtSym("%ReadPixelFast%x%y%buffer=0");
        rtSym("WritePixelFast%x%y%argb%buffer=0");
        rtSym("CopyPixel%src_x%src_y%src_buffer%dest_x%dest_y%dest_buffer=0");
        rtSym("CopyPixelFast%src_x%src_y%src_buffer%dest_x%dest_y%dest_buffer=0");
        rtSym("Origin%x%y");
        rtSym("Viewport%x%y%width%height");
        rtSym("Color%red%green%blue");
        rtSym("GetColor%x%y");
        rtSym("%ColorRed");
        rtSym("%ColorGreen");
        rtSym("%ColorBlue");
        rtSym("ClsColor%red%green%blue");
        rtSym("SetFont%font");
        rtSym("Cls");
        rtSym("Plot%x%y");
        rtSym("Rect%x%y%width%height%solid=1");
        rtSym("Oval%x%y%width%height%solid=1");
        rtSym("Line%x1%y1%x2%y2");
        rtSym("Text%x%y$text%centre_x=0%centre_y=0");
        rtSym("CopyRect%source_x%source_y%width%height%dest_x%dest_y%src_buffer=0%dest_buffer=0");
        rtSym("CopyRectStretch%source_x%source_y%width%height%dest_x%dest_y%dest_w%dest_h%src_buffer=0%dest_buffer=0");
        rtSym("%LoadFont$fontname%height=12");
        rtSym("FreeFont%font");
        rtSym("%FontWidth");
        rtSym("%FontHeight");
        rtSym("%StringWidth$string");
        rtSym("%StringHeight$string");
        rtSym("%OpenMovie$file");
        rtSym("%DrawMovie%movie%x=0%y=0%w=-1%h=-1");
        rtSym("%MovieWidth%movie");
        rtSym("%MovieHeight%movie");
        rtSym("%MoviePlaying%movie");
        rtSym("CloseMovie%movie");
        rtSym("%LoadImage$bmpfile");
        rtSym("%LoadAnimImage$bmpfile%cellwidth%cellheight%first%count");
        rtSym("%CopyImage%image");
        rtSym("%CreateImage%width%height%frames=1");
        rtSym("FreeImage%image");
        rtSym("%SaveImage%image$bmpfile%frame=0");
        rtSym("GrabImage%image%x%y%frame=0");
        rtSym("%ImageBuffer%image%frame=0");
        rtSym("DrawImage%image%x%y%frame=0");
        rtSym("DrawBlock%image%x%y%frame=0");
        rtSym("TileImage%image%x=0%y=0%frame=0");
        rtSym("TileBlock%image%x=0%y=0%frame=0");
        rtSym("DrawImageRect%image%x%y%rect_x%rect_y%rect_width%rect_height%frame=0");
        rtSym("DrawBlockRect%image%x%y%rect_x%rect_y%rect_width%rect_height%frame=0");
        rtSym("MaskImage%image%red%green%blue");
        rtSym("HandleImage%image%x%y");
        rtSym("MidHandle%image");
        rtSym("AutoMidHandle%enable");
        rtSym("%ImageWidth%image");
        rtSym("%ImageHeight%image");
        rtSym("%ImageXHandle%image");
        rtSym("%ImageYHandle%image");
        rtSym("ScaleImage%image#xscale#yscale");
        rtSym("ResizeImage%image#width#height");
        rtSym("RotateImage%image#angle");
        rtSym("TFormImage%image#a#b#c#d");
        rtSym("TFormFilter%enable");
        rtSym("%ImagesOverlap%image1%x1%y1%image2%x2%y2");
        rtSym("%ImagesCollide%image1%x1%y1%frame1%image2%x2%y2%frame2");
        rtSym("%RectsOverlap%x1%y1%width1%height1%x2%y2%width2%height2");
        rtSym("%ImageRectOverlap%image%x%y%rect_x%rect_y%rect_width%rect_height");
        rtSym("%ImageRectCollide%image%x%y%frame%rect_x%rect_y%rect_width%rect_height");
        rtSym("Write$string");
        rtSym("Print$string=\"\"");
        rtSym("$Input$prompt=\"\"");
        rtSym("Locate%x%y");
        rtSym("ShowPointer");
        rtSym("HidePointer");
        rtSym("%DesktopWidth");
        rtSym("%DesktopHeight");
        rtSym("%KeyDown%key");
        rtSym("%KeyHit%key");
        rtSym("%GetKey");
        rtSym("%WaitKey");
        rtSym("$TextInput$txt");
        rtSym("FlushKeys");
        rtSym("%MouseDown%button");
        rtSym("%MouseHit%button");
        rtSym("%GetMouse");
        rtSym("%WaitMouse");
        rtSym("%MouseWait");
        rtSym("%MouseX");
        rtSym("%MouseY");
        rtSym("%MouseZ");
        rtSym("%MouseXSpeed");
        rtSym("%MouseYSpeed");
        rtSym("%MouseZSpeed");
        rtSym("FlushMouse");
        rtSym("MoveMouse%x%y");
        rtSym("%JoyType%port=0");
        rtSym("%JoyDown%button%port=0");
        rtSym("%JoyHit%button%port=0");
        rtSym("%GetJoy%port=0");
        rtSym("%WaitJoy%port=0");
        rtSym("%JoyWait%port=0");
        rtSym("#JoyX%port=0");
        rtSym("#JoyY%port=0");
        rtSym("#JoyZ%port=0");
        rtSym("#JoyU%port=0");
        rtSym("#JoyV%port=0");
        rtSym("#JoyPitch%port=0");
        rtSym("#JoyYaw%port=0");
        rtSym("#JoyRoll%port=0");
        rtSym("%JoyHat%port=0");
        rtSym("%JoyXDir%port=0");
        rtSym("%JoyYDir%port=0");
        rtSym("%JoyZDir%port=0");
        rtSym("%JoyUDir%port=0");
        rtSym("%JoyVDir%port=0");
        rtSym("FlushJoy");
        rtSym("EnableDirectInput%enable");
        rtSym("%DirectInputEnabled");
        rtSym("#Sin#degrees");
        rtSym("#Cos#degrees");
        rtSym("#Tan#degrees");
        rtSym("#ASin#float");
        rtSym("#ACos#float");
        rtSym("#ATan#float");
        rtSym("#ATan2#floata#floatb");
        rtSym("#Sqr#float");
        rtSym("#Floor#float");
        rtSym("#Ceil#float");
        rtSym("#Exp#float");
        rtSym("#Log#float");
        rtSym("#Log10#float");
        rtSym("#Min#n#m");
        rtSym("#Max#n#m");
        rtSym("#Clamp#v#lo#hi");
        rtSym("%IsNaN#n");
        rtSym("#Rnd#from#to=0");
        rtSym("%Rand%from%to=1");
        rtSym("SeedRnd%seed");
        rtSym("%RndSeed");
        rtSym("End");
        rtSym("Stop");
        rtSym("AppTitle$title$close_prompt=\"\"");
        rtSym("RuntimeError$message");
        rtSym("InitErrorMsgs%number");
        rtSym("SetErrorMsg%pos$message");
        rtSym("ExecFile$command");
        rtSym("Delay%millisecs");
        rtSym("%MilliSecs");
        rtSym("$CommandLine");
        rtSym("$SystemProperty$property");
        rtSym("$GetEnv$env_var");
        rtSym("SetEnv$env_var$value");
        rtSym("%CreateTimer%hertz");
        rtSym("%WaitTimer%timer");
        rtSym("FreeTimer%timer");
        rtSym("$GetClipboardContents");
        rtSym("SetClipboardContents$contents");
        rtSym("DebugLog$text");
        rtSym("_bbDebugStmt");
        rtSym("_bbDebugEnter");
        rtSym("_bbDebugLeave");
        rtSym("$DottedIP%IP");
        rtSym("%CountHostIPs$host_name");
        rtSym("%HostIP%host_index");
        rtSym("%CreateUDPStream%port=0");
        rtSym("CloseUDPStream%udp_stream");
        rtSym("SendUDPMsg%udp_stream%dest_ip%dest_port=0");
        rtSym("%RecvUDPMsg%udp_stream");
        rtSym("%UDPStreamIP%udp_stream");
        rtSym("%UDPStreamPort%udp_stream");
        rtSym("%UDPMsgIP%udp_stream");
        rtSym("%UDPMsgPort%udp_stream");
        rtSym("UDPTimeouts%recv_timeout");
        rtSym("%OpenTCPStream$server%server_port%local_port=0");
        rtSym("CloseTCPStream%tcp_stream");
        rtSym("%CreateTCPServer%port");
        rtSym("CloseTCPServer%tcp_server");
        rtSym("%AcceptTCPStream%tcp_server");
        rtSym("%TCPStreamIP%tcp_stream");
        rtSym("%TCPStreamPort%tcp_stream");
        rtSym("TCPTimeouts%read_millis%accept_millis");
        rtSym("%Eof%stream");
        rtSym("%ReadAvail%stream");
        rtSym("%ReadByte%stream");
        rtSym("%ReadShort%stream");
        rtSym("%ReadInt%stream");
        rtSym("#ReadFloat%stream");
        rtSym("$ReadString%stream");
        rtSym("$ReadLine%stream");
        rtSym("WriteByte%stream%byte");
        rtSym("WriteShort%stream%short");
        rtSym("WriteInt%stream%int");
        rtSym("WriteFloat%stream#float");
        rtSym("WriteString%stream$string");
        rtSym("WriteLine%stream$string");
        rtSym("CopyStream%src_stream%dest_stream%buffer_size=16384");
        rtSym("$String$string%repeat");
        rtSym("$Left$string%count");
        rtSym("$Right$string%count");
        rtSym("$Replace$string$from$to");
        rtSym("%Instr$string$find%from=1");
        rtSym("$Mid$string%start%count=-1");
        rtSym("$Upper$string");
        rtSym("$Lower$string");
        rtSym("$Trim$string");
        rtSym("$LSet$string%size");
        rtSym("$RSet$string%size");
        rtSym("$Chr%ascii");
        rtSym("%Asc$string");
        rtSym("%Len$string");
        rtSym("$Hex%value");
        rtSym("$Bin%value");
        rtSym("$CurrentDate");
        rtSym("$CurrentTime");

        _ = new Function("_builtIn__bbAbs", DeclType.Int) { ReturnType = DeclType.Int };
        _ = new Function("_builtIn__bbFAbs", DeclType.Float) { ReturnType = DeclType.Float };
        _ = new Function("_builtIn__bbFPow", DeclType.Float, DeclType.Float) { ReturnType = DeclType.Float };
        _ = new Function("_builtIn__bbMod", DeclType.Int, DeclType.Int) { ReturnType = DeclType.Int };
        _ = new Function("_builtIn__bbFMod", DeclType.Float, DeclType.Float) { ReturnType = DeclType.Float };
        
        _ = new Function("_builtIn__bbStrConst", 1) { ReturnType = DeclType.String };
        _ = new Function("_builtIn__bbStrFromInt", DeclType.Int) { ReturnType = DeclType.String };
        _ = new Function("_builtIn__bbStrFromFloat", DeclType.Float) { ReturnType = DeclType.String };
        _ = new Function("_builtIn__bbStrToInt", DeclType.String) { ReturnType = DeclType.Int };
        _ = new Function("_builtIn__bbStrToFloat", DeclType.String) { ReturnType = DeclType.Float };
        _ = new Function("_builtIn__bbStrLoad", DeclType.String) { ReturnType = DeclType.String };
        _ = new Function("_builtIn__bbStrRelease", DeclType.String);
        _ = new Function("_builtIn__bbStrStore", 2);
        _ = new Function("_builtIn__bbStrConcat", DeclType.String, DeclType.String) { ReturnType = DeclType.String };
        _ = new Function("_builtIn__bbStrCompare", DeclType.String, DeclType.String) { ReturnType = DeclType.Int };
        
        _ = new Function("_builtIn__bbReadStr", 0);
        _ = new Function("_builtIn_ferrorlog", 0) { ReturnType = DeclType.String };

        _ = new Function("_builtIn__bbObjEachFirst", 2);
        _ = new Function("_builtIn__bbObjEachNext", 1);
        _ = new Function("_builtIn__bbObjEachFirst2", 2);
        _ = new Function("_builtIn__bbObjEachNext2", 1);
        _ = new Function("_builtIn__bbObjFromHandle", 2);
        _ = new Function("_builtIn__bbObjToHandle", 1) { ReturnType = DeclType.Int };
        _ = new Function("_builtIn__bbObjNew", 1);
        _ = new Function("_builtIn__bbObjNext", 1);
        _ = new Function("_builtIn__bbObjPrev", 1);
        _ = new Function("_builtIn__bbObjCompare", 2);
        _ = new Function("_builtIn__bbObjRelease", 1);
        _ = new Function("_builtIn__bbObjStore", 2);
        _ = new Function("_builtIn__bbObjLoad", 1);
        _ = new Function("_builtIn__bbFieldPtrAdd", 2);
        _ = new Function("_builtIn__bbObjDelete", 1);
    }
    
    public static Function? GetFunctionWithName(string name)
    {
        name = name.ToLowerInvariant();
        if (name[0] == '@') { name = name[1..]; }

        if (lookupDictionary.TryGetValue(name, out var f))
        {
            return f;
        }

        if (name[0] == '_' && name[1] == 'f')
        {
            name = name[2..];
            if (lookupDictionary.TryGetValue(name, out f))
            {
                return f;
            }
        }

        return null;
    }

    public bool IsBuiltIn
        => Name.StartsWith("_builtIn");

    public string CoreSymbolName
        => Name == "EntryPoint"
            ? "__MAIN"
            : Name.StartsWith("_builtIn")
                ? Name
                : $"_f{Name}";

    public DeclType ReturnType = DeclType.Unknown;
    public readonly List<Parameter> Parameters = new List<Parameter>();
    public readonly List<LocalVariable> LocalVariables = new List<LocalVariable>();
    public readonly List<LocalVariable> CompilerGeneratedTempVars = new List<LocalVariable>();

    public Variable? InstructionArgumentToVariable(string arg)
    {
        arg = arg.StripDeref();
        if (arg.StartsWith("ebp-0x", StringComparison.Ordinal))
        {
            var varIndex = (int.Parse(arg[6..], NumberStyles.HexNumber) - 0x4) >> 2;
            if (varIndex >= 0 && varIndex < LocalVariables.Count)
            {
                return LocalVariables[varIndex];
            }
        }
        else if (arg.StartsWith("ebp+0x", StringComparison.Ordinal))
        {
            var paramIndex = (int.Parse(arg[6..], NumberStyles.HexNumber) - 0x14) >> 2;
            if (paramIndex >= 0 && paramIndex < Parameters.Count)
            {
                return Parameters[paramIndex];
            }
        }
        else if (arg.StartsWith("@_v", StringComparison.Ordinal))
        {
            return GlobalVariable.AllGlobals.FirstOrDefault(
                v => v.Name.Equals(arg[3..], StringComparison.OrdinalIgnoreCase));
        }
        else if (arg.Contains('\\', StringComparison.Ordinal))
        {
            var split = arg.Split('\\');

            var matchingRootVar = LocalVariables.Cast<Variable>().Concat(Parameters).Concat(GlobalVariable.AllGlobals)
                .FirstOrDefault(v => v.Name.Equals(split[0], StringComparison.OrdinalIgnoreCase));
            if (matchingRootVar is null) { return null; }

            var currVar = matchingRootVar;
            for (int i = 1; i < split.Length; i++)
            {
                var part = split[i];
                var arrayIndex = "";
                if (part.IndexOf('[', StringComparison.Ordinal) is var indexerStart and >= 0
                    && part.IndexOf(']', StringComparison.Ordinal) is var indexerEnd and >= 0)
                {
                    arrayIndex = part[(indexerStart + 1)..indexerEnd];
                    part = part[..indexerStart];
                }
                var index = int.Parse(string.Join("", part.Where(char.IsDigit)));
                currVar = currVar.Fields[index];
                if (arrayIndex != "" && currVar.GetArrayElement(arrayIndex) is { } arrayElement)
                {
                    currVar = arrayElement;
                }
            }
            return currVar;
        }

        return null;
    }

    public sealed class Instruction
    {
        public string Name;
        public string LeftArg;
        public string RightArg;

        public int[]? CallParameterAssignmentIndices;
        public string? BbObjType;

        public Instruction(string name, string leftArg = "", string rightArg = "")
        {
            Name = name;
            LeftArg = leftArg;
            RightArg = rightArg;
        }

        public bool IsJumpOrCall
            => Name is
                "call" or "jmp" or "je" or "jz"
                or "jne" or "jnz" or "jg" or "jge"
                or "jl" or "jle";

        public override string ToString()
            => string.IsNullOrWhiteSpace(LeftArg)
                ? Name
                : string.IsNullOrWhiteSpace(RightArg)
                    ? $"{Name} {LeftArg}"
                    : $"{Name} {LeftArg}, {RightArg}";
    }

    private static Function rtSym(string str)
    {
        static DeclType ripTypeFromStr(ref string str, DeclType defaultType)
        {
            if (str[0] == '#')
            {
                str = str[1..];
                return DeclType.Float;
            }
            if (str[0] == '%')
            {
                str = str[1..];
                return DeclType.Int;
            }
            if (str[0] == '$')
            {
                str = str[1..];
                return DeclType.String;
            }
            return defaultType;
        }

        DeclType returnType = ripTypeFromStr(ref str, DeclType.Unknown);

        str = str
            .Replace("%", " %")
            .Replace("#", " #")
            .Replace("$", " $");
        var split = str.Split(" ");
        var parameters = new List<Parameter>();
        var funcName = split[0].ToLowerInvariant();
        split = split.Skip(1).ToArray();
        for (var argIndex = 0; argIndex < split.Length; argIndex++)
        {
            var argName = split[argIndex];
            var argType = ripTypeFromStr(ref argName, DeclType.Int);
            parameters.Add(new Parameter(argName, argIndex) { DeclType = argType });
        }

        var newFunction = new Function($"_builtIn_f{funcName}", 0) { ReturnType = returnType };
        newFunction.Parameters.Clear(); newFunction.Parameters.AddRange(parameters);
        return newFunction;
    }

    public Function(string name, int argCount) : this(name, new Dictionary<string, AssemblySection>())
    {
        Parameters = Enumerable.Range(0, argCount)
            .Select(i => new Parameter($"arg{i}", i) { DeclType = DeclType.Unknown })
            .ToList();
    }

    public Function(string name, params DeclType[] types) : this(name, types.Length)
    {
        for (int i = 0; i < types.Length; i++)
        {
            Parameters[i].DeclType = types[i];
        }
    }

    public Function(string name, Dictionary<string, AssemblySection> assemblySections)
    {
        Name = name;
        AssemblySections = assemblySections;
        AllFunctions.Add(this);
        lookupDictionary.Add(name.ToLowerInvariant(), this);
    }

    public override string ToString()
    {
        return Name + ReturnType.Suffix + "("
               + string.Join(", ", Parameters) + ")";
    }
}

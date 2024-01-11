﻿namespace Blitz3DDecomp;

static class Blitz3dBuiltIns
{
    public static void Init()
    {
        Function.FromBlitzSymbol("RuntimeStats");
        Function.FromBlitzSymbol("%LoadSound$filename");
        Function.FromBlitzSymbol("FreeSound%sound");
        Function.FromBlitzSymbol("LoopSound%sound");
        Function.FromBlitzSymbol("SoundPitch%sound%pitch");
        Function.FromBlitzSymbol("SoundVolume%sound#volume");
        Function.FromBlitzSymbol("SoundPan%sound#pan");
        Function.FromBlitzSymbol("%PlaySound%sound");
        Function.FromBlitzSymbol("%PlayMusic$midifile%mode=0");
        Function.FromBlitzSymbol("%PlayCDTrack%track%mode=1");
        Function.FromBlitzSymbol("StopChannel%channel");
        Function.FromBlitzSymbol("PauseChannel%channel");
        Function.FromBlitzSymbol("ResumeChannel%channel");
        Function.FromBlitzSymbol("ChannelPitch%channel%pitch");
        Function.FromBlitzSymbol("ChannelVolume%channel#volume");
        Function.FromBlitzSymbol("ChannelPan%channel#pan");
        Function.FromBlitzSymbol("%ChannelPlaying%channel");
        Function.FromBlitzSymbol("%Load3DSound$filename");
        Function.FromBlitzSymbol("%CreateBank%size=0");
        Function.FromBlitzSymbol("FreeBank%bank");
        Function.FromBlitzSymbol("%BankSize%bank");
        Function.FromBlitzSymbol("ResizeBank%bank%size");
        Function.FromBlitzSymbol("CopyBank%src_bank%src_offset%dest_bank%dest_offset%count");
        Function.FromBlitzSymbol("%PeekByte%bank%offset");
        Function.FromBlitzSymbol("%PeekShort%bank%offset");
        Function.FromBlitzSymbol("%PeekInt%bank%offset");
        Function.FromBlitzSymbol("#PeekFloat%bank%offset");
        Function.FromBlitzSymbol("PokeByte%bank%offset%value");
        Function.FromBlitzSymbol("PokeShort%bank%offset%value");
        Function.FromBlitzSymbol("PokeInt%bank%offset%value");
        Function.FromBlitzSymbol("PokeFloat%bank%offset#value");
        Function.FromBlitzSymbol("%ReadBytes%bank%file%offset%count");
        Function.FromBlitzSymbol("%WriteBytes%bank%file%offset%count");
        Function.FromBlitzSymbol("%CallDLL$dll_name$func_name%in_bank=0%out_bank=0");
        Function.FromBlitzSymbol("LoaderMatrix$file_ext#xx#xy#xz#yx#yy#yz#zx#zy#zz");
        Function.FromBlitzSymbol("HWMultiTex%enable");
        Function.FromBlitzSymbol("%HWTexUnits");
        Function.FromBlitzSymbol("%GfxDriverCaps3D");
        Function.FromBlitzSymbol("WBuffer%enable");
        Function.FromBlitzSymbol("Dither%enable");
        Function.FromBlitzSymbol("AntiAlias%enable");
        Function.FromBlitzSymbol("WireFrame%enable");
        Function.FromBlitzSymbol("AmbientLight#red#green#blue");
        Function.FromBlitzSymbol("ClearCollisions");
        Function.FromBlitzSymbol("Collisions%source_type%destination_type%method%response");
        Function.FromBlitzSymbol("UpdateWorld#elapsed_time=1");
        Function.FromBlitzSymbol("CaptureWorld");
        Function.FromBlitzSymbol("RenderWorld#tween=1");
        Function.FromBlitzSymbol("ClearWorld%entities=1%brushes=1%textures=1");
        Function.FromBlitzSymbol("%ActiveTextures");
        Function.FromBlitzSymbol("%TrisRendered");
        Function.FromBlitzSymbol("#Stats3D%type");
        Function.FromBlitzSymbol("%CreateTexture%width%height%flags=0%frames=1");
        Function.FromBlitzSymbol("%LoadTexture$file%flags=1");
        Function.FromBlitzSymbol("%LoadAnimTexture$file%flags%width%height%first%count");
        Function.FromBlitzSymbol("FreeTexture%texture");
        Function.FromBlitzSymbol("TextureBlend%texture%blend");
        Function.FromBlitzSymbol("TextureCoords%texture%coords");
        Function.FromBlitzSymbol("TextureBumpEnvMat%texture%x%y#envmat");
        Function.FromBlitzSymbol("TextureBumpEnvScale%texture#envmat");
        Function.FromBlitzSymbol("TextureBumpEnvOffset%texture#envoffset");
        Function.FromBlitzSymbol("ScaleTexture%texture#u_scale#v_scale");
        Function.FromBlitzSymbol("RotateTexture%texture#angle");
        Function.FromBlitzSymbol("PositionTexture%texture#u_offset#v_offset");
        Function.FromBlitzSymbol("TextureLodBias#bias");
        Function.FromBlitzSymbol("TextureAnisotropic%level");
        Function.FromBlitzSymbol("%TextureWidth%texture");
        Function.FromBlitzSymbol("%TextureHeight%texture");
        Function.FromBlitzSymbol("$TextureName%texture");
        Function.FromBlitzSymbol("SetCubeFace%texture%face");
        Function.FromBlitzSymbol("SetCubeMode%texture%mode");
        Function.FromBlitzSymbol("%TextureBuffer%texture%frame=0");
        Function.FromBlitzSymbol("ClearTextureFilters");
        Function.FromBlitzSymbol("TextureFilter$match_text%texture_flags=0");
        Function.FromBlitzSymbol("%CreateBrush#red=255#green=255#blue=255");
        Function.FromBlitzSymbol("%LoadBrush$file%texture_flags=1#u_scale=1#v_scale=1");
        Function.FromBlitzSymbol("FreeBrush%brush");
        Function.FromBlitzSymbol("BrushColor%brush#red#green#blue");
        Function.FromBlitzSymbol("BrushAlpha%brush#alpha");
        Function.FromBlitzSymbol("BrushShininess%brush#shininess");
        Function.FromBlitzSymbol("BrushTexture%brush%texture%frame=0%index=0");
        Function.FromBlitzSymbol("%GetBrushTexture%brush%index=0");
        Function.FromBlitzSymbol("BrushBlend%brush%blend");
        Function.FromBlitzSymbol("BrushFX%brush%fx");
        Function.FromBlitzSymbol("%LoadMesh$file%parent=0");
        Function.FromBlitzSymbol("%LoadAnimMesh$file%parent=0");
        Function.FromBlitzSymbol("%LoadAnimSeq%entity$file");
        Function.FromBlitzSymbol("%CreateMesh%parent=0");
        Function.FromBlitzSymbol("%CreateCube%parent=0");
        Function.FromBlitzSymbol("%CreateSphere%segments=8%parent=0");
        Function.FromBlitzSymbol("%CreateCylinder%segments=8%solid=1%parent=0");
        Function.FromBlitzSymbol("%CreateCone%segments=8%solid=1%parent=0");
        Function.FromBlitzSymbol("%CopyMesh%mesh%parent=0");
        Function.FromBlitzSymbol("ScaleMesh%mesh#x_scale#y_scale#z_scale");
        Function.FromBlitzSymbol("RotateMesh%mesh#pitch#yaw#roll");
        Function.FromBlitzSymbol("PositionMesh%mesh#x#y#z");
        Function.FromBlitzSymbol("FitMesh%mesh#x#y#z#width#height#depth%uniform=0");
        Function.FromBlitzSymbol("FlipMesh%mesh");
        Function.FromBlitzSymbol("PaintMesh%mesh%brush");
        Function.FromBlitzSymbol("AddMesh%source_mesh%dest_mesh");
        Function.FromBlitzSymbol("UpdateNormals%mesh");
        Function.FromBlitzSymbol("LightMesh%mesh#red#green#blue#range=0#x=0#y=0#z=0");
        Function.FromBlitzSymbol("#MeshWidth%mesh");
        Function.FromBlitzSymbol("#MeshHeight%mesh");
        Function.FromBlitzSymbol("#MeshDepth%mesh");
        Function.FromBlitzSymbol("%MeshesIntersect%mesh_a%mesh_b");
        Function.FromBlitzSymbol("%CountSurfaces%mesh");
        Function.FromBlitzSymbol("%GetSurface%mesh%surface_index");
        Function.FromBlitzSymbol("MeshCullBox%mesh#x#y#z#width#height#depth");
        Function.FromBlitzSymbol("%CreateSurface%mesh%brush=0");
        Function.FromBlitzSymbol("%GetSurfaceBrush%surface");
        Function.FromBlitzSymbol("%GetEntityBrush%entity");
        Function.FromBlitzSymbol("%FindSurface%mesh%brush");
        Function.FromBlitzSymbol("ClearSurface%surface%clear_vertices=1%clear_triangles=1");
        Function.FromBlitzSymbol("PaintSurface%surface%brush");
        Function.FromBlitzSymbol("%AddVertex%surface#x#y#z#u=0#v=0#w=1");
        Function.FromBlitzSymbol("%AddTriangle%surface%v0%v1%v2");
        Function.FromBlitzSymbol("VertexCoords%surface%index#x#y#z");
        Function.FromBlitzSymbol("VertexNormal%surface%index#nx#ny#nz");
        Function.FromBlitzSymbol("VertexColor%surface%index#red#green#blue#alpha=1");
        Function.FromBlitzSymbol("VertexTexCoords%surface%index#u#v#w=1%coord_set=0");
        Function.FromBlitzSymbol("%CountVertices%surface");
        Function.FromBlitzSymbol("%CountTriangles%surface");
        Function.FromBlitzSymbol("#VertexX%surface%index");
        Function.FromBlitzSymbol("#VertexY%surface%index");
        Function.FromBlitzSymbol("#VertexZ%surface%index");
        Function.FromBlitzSymbol("#VertexNX%surface%index");
        Function.FromBlitzSymbol("#VertexNY%surface%index");
        Function.FromBlitzSymbol("#VertexNZ%surface%index");
        Function.FromBlitzSymbol("#VertexRed%surface%index");
        Function.FromBlitzSymbol("#VertexGreen%surface%index");
        Function.FromBlitzSymbol("#VertexBlue%surface%index");
        Function.FromBlitzSymbol("#VertexAlpha%surface%index");
        Function.FromBlitzSymbol("#VertexU%surface%index%coord_set=0");
        Function.FromBlitzSymbol("#VertexV%surface%index%coord_set=0");
        Function.FromBlitzSymbol("#VertexW%surface%index%coord_set=0");
        Function.FromBlitzSymbol("%TriangleVertex%surface%index%vertex");
        Function.FromBlitzSymbol("%CreateCamera%parent=0");
        Function.FromBlitzSymbol("CameraZoom%camera#zoom");
        Function.FromBlitzSymbol("CameraRange%camera#near#far");
        Function.FromBlitzSymbol("#GetCameraRangeNear%camera");
        Function.FromBlitzSymbol("#GetCameraRangeFar%camera");
        Function.FromBlitzSymbol("CameraClsColor%camera#red#green#blue");
        Function.FromBlitzSymbol("CameraClsMode%camera%cls_color%cls_zbuffer");
        Function.FromBlitzSymbol("CameraProjMode%camera%mode");
        Function.FromBlitzSymbol("CameraViewport%camera%x%y%width%height");
        Function.FromBlitzSymbol("CameraFogColor%camera#red#green#blue");
        Function.FromBlitzSymbol("CameraFogRange%camera#near#far");
        Function.FromBlitzSymbol("#GetCameraFogRangeNear%camera");
        Function.FromBlitzSymbol("#GetCameraFogRangeFar%camera");
        Function.FromBlitzSymbol("CameraFogDensity%camera#density");
        Function.FromBlitzSymbol("CameraFogMode%camera%mode");
        Function.FromBlitzSymbol("CameraProject%camera#x#y#z");
        Function.FromBlitzSymbol("#ProjectedX");
        Function.FromBlitzSymbol("#ProjectedY");
        Function.FromBlitzSymbol("#ProjectedZ");
        Function.FromBlitzSymbol("%EntityInView%entity%camera");
        Function.FromBlitzSymbol("%EntityVisible%src_entity%dest_entity");
        Function.FromBlitzSymbol("%EntityPick%entity#range");
        Function.FromBlitzSymbol("%LinePick#x#y#z#dx#dy#dz#radius=0");
        Function.FromBlitzSymbol("%CameraPick%camera#viewport_x#viewport_y");
        Function.FromBlitzSymbol("#PickedX");
        Function.FromBlitzSymbol("#PickedY");
        Function.FromBlitzSymbol("#PickedZ");
        Function.FromBlitzSymbol("#PickedNX");
        Function.FromBlitzSymbol("#PickedNY");
        Function.FromBlitzSymbol("#PickedNZ");
        Function.FromBlitzSymbol("#PickedTime");
        Function.FromBlitzSymbol("%PickedEntity");
        Function.FromBlitzSymbol("%PickedSurface");
        Function.FromBlitzSymbol("%PickedTriangle");
        Function.FromBlitzSymbol("%CreateLight%type=1%parent=0");
        Function.FromBlitzSymbol("LightColor%light#red#green#blue");
        Function.FromBlitzSymbol("LightRange%light#range");
        Function.FromBlitzSymbol("LightConeAngles%light#inner_angle#outer_angle");
        Function.FromBlitzSymbol("%CreatePivot%parent=0");
        Function.FromBlitzSymbol("%CreateSprite%parent=0");
        Function.FromBlitzSymbol("%LoadSprite$file%texture_flags=1%parent=0");
        Function.FromBlitzSymbol("RotateSprite%sprite#angle");
        Function.FromBlitzSymbol("ScaleSprite%sprite#x_scale#y_scale");
        Function.FromBlitzSymbol("HandleSprite%sprite#x_handle#y_handle");
        Function.FromBlitzSymbol("SpriteViewMode%sprite%view_mode");
        Function.FromBlitzSymbol("%LoadMD2$file%parent=0");
        Function.FromBlitzSymbol("AnimateMD2%md2%mode=1#speed=1%first_frame=0%last_frame=9999#transition=0");
        Function.FromBlitzSymbol("#MD2AnimTime%md2");
        Function.FromBlitzSymbol("%MD2AnimLength%md2");
        Function.FromBlitzSymbol("%MD2Animating%md2");
        Function.FromBlitzSymbol("%LoadBSP$file#gamma_adj=0%parent=0");
        Function.FromBlitzSymbol("BSPLighting%bsp%use_lightmaps");
        Function.FromBlitzSymbol("BSPAmbientLight%bsp#red#green#blue");
        Function.FromBlitzSymbol("%CreateMirror%parent=0");
        Function.FromBlitzSymbol("%CreatePlane%segments=1%parent=0");
        Function.FromBlitzSymbol("%CreateTerrain%grid_size%parent=0");
        Function.FromBlitzSymbol("%LoadTerrain$heightmap_file%parent=0");
        Function.FromBlitzSymbol("TerrainDetail%terrain%detail_level%morph=0");
        Function.FromBlitzSymbol("TerrainShading%terrain%enable");
        Function.FromBlitzSymbol("#TerrainX%terrain#world_x#world_y#world_z");
        Function.FromBlitzSymbol("#TerrainY%terrain#world_x#world_y#world_z");
        Function.FromBlitzSymbol("#TerrainZ%terrain#world_x#world_y#world_z");
        Function.FromBlitzSymbol("%TerrainSize%terrain");
        Function.FromBlitzSymbol("#TerrainHeight%terrain%terrain_x%terrain_z");
        Function.FromBlitzSymbol("ModifyTerrain%terrain%terrain_x%terrain_z#height%realtime=0");
        Function.FromBlitzSymbol("%CreateListener%parent#rolloff_factor=1#doppler_scale=1#distance_scale=1");
        Function.FromBlitzSymbol("%EmitSound%sound%entity");
        Function.FromBlitzSymbol("%CopyEntity%entity%parent=0");
        Function.FromBlitzSymbol("#EntityX%entity%global=0");
        Function.FromBlitzSymbol("#EntityY%entity%global=0");
        Function.FromBlitzSymbol("#EntityZ%entity%global=0");
        Function.FromBlitzSymbol("#EntityPitch%entity%global=0");
        Function.FromBlitzSymbol("#EntityYaw%entity%global=0");
        Function.FromBlitzSymbol("#EntityRoll%entity%global=0");
        Function.FromBlitzSymbol("#EntityScaleX%entity%global=0");
        Function.FromBlitzSymbol("#EntityScaleY%entity%global=0");
        Function.FromBlitzSymbol("#EntityScaleZ%entity%global=0");
        Function.FromBlitzSymbol("#GetMatElement%entity%row%column");
        Function.FromBlitzSymbol("TFormPoint#x#y#z%source_entity%dest_entity");
        Function.FromBlitzSymbol("TFormVector#x#y#z%source_entity%dest_entity");
        Function.FromBlitzSymbol("TFormNormal#x#y#z%source_entity%dest_entity");
        Function.FromBlitzSymbol("#TFormedX");
        Function.FromBlitzSymbol("#TFormedY");
        Function.FromBlitzSymbol("#TFormedZ");
        Function.FromBlitzSymbol("#VectorYaw#x#y#z");
        Function.FromBlitzSymbol("#VectorPitch#x#y#z");
        Function.FromBlitzSymbol("#DeltaPitch%src_entity%dest_entity");
        Function.FromBlitzSymbol("#DeltaYaw%src_entity%dest_entity");
        Function.FromBlitzSymbol("ResetEntity%entity");
        Function.FromBlitzSymbol("EntityType%entity%collision_type%recursive=0");
        Function.FromBlitzSymbol("EntityPickMode%entity%pick_geometry%obscurer=1");
        Function.FromBlitzSymbol("%GetParent%entity");
        Function.FromBlitzSymbol("%GetEntityType%entity");
        Function.FromBlitzSymbol("EntityRadius%entity#x_radius#y_radius=0");
        Function.FromBlitzSymbol("EntityBox%entity#x#y#z#width#height#depth");
        Function.FromBlitzSymbol("#EntityDistance%source_entity%destination_entity");
        Function.FromBlitzSymbol("#EntityDistanceSquared%source_entity%destination_entity");
        Function.FromBlitzSymbol("%EntityCollided%entity%type");
        Function.FromBlitzSymbol("%CountCollisions%entity");
        Function.FromBlitzSymbol("#CollisionX%entity%collision_index");
        Function.FromBlitzSymbol("#CollisionY%entity%collision_index");
        Function.FromBlitzSymbol("#CollisionZ%entity%collision_index");
        Function.FromBlitzSymbol("#CollisionNX%entity%collision_index");
        Function.FromBlitzSymbol("#CollisionNY%entity%collision_index");
        Function.FromBlitzSymbol("#CollisionNZ%entity%collision_index");
        Function.FromBlitzSymbol("#CollisionTime%entity%collision_index");
        Function.FromBlitzSymbol("%CollisionEntity%entity%collision_index");
        Function.FromBlitzSymbol("%CollisionSurface%entity%collision_index");
        Function.FromBlitzSymbol("%CollisionTriangle%entity%collision_index");
        Function.FromBlitzSymbol("#Distance#x1#x2#y1#y2#z1=0#z2=0");
        Function.FromBlitzSymbol("#DistanceSquared#x1#x2#y1#y2#z1=0#z2=0");
        Function.FromBlitzSymbol("MoveEntity%entity#x#y#z");
        Function.FromBlitzSymbol("TurnEntity%entity#pitch#yaw#roll%global=0");
        Function.FromBlitzSymbol("TranslateEntity%entity#x#y#z%global=0");
        Function.FromBlitzSymbol("PositionEntity%entity#x#y#z%global=0");
        Function.FromBlitzSymbol("ScaleEntity%entity#x_scale#y_scale#z_scale%global=0");
        Function.FromBlitzSymbol("RotateEntity%entity#pitch#yaw#roll%global=0");
        Function.FromBlitzSymbol("PointEntity%entity%target#roll=0");
        Function.FromBlitzSymbol("AlignToVector%entity#vector_x#vector_y#vector_z%axis#rate=1");
        Function.FromBlitzSymbol("SetAnimTime%entity#time%anim_seq=0");
        Function.FromBlitzSymbol("Animate%entity%mode=1#speed=1%sequence=0#transition=0");
        Function.FromBlitzSymbol("SetAnimKey%entity%frame%pos_key=1%rot_key=1%scale_key=1");
        Function.FromBlitzSymbol("%AddAnimSeq%entity%length");
        Function.FromBlitzSymbol("%ExtractAnimSeq%entity%first_frame%last_frame%anim_seq=0");
        Function.FromBlitzSymbol("%AnimSeq%entity");
        Function.FromBlitzSymbol("#AnimTime%entity");
        Function.FromBlitzSymbol("%AnimLength%entity");
        Function.FromBlitzSymbol("%Animating%entity");
        Function.FromBlitzSymbol("EntityParent%entity%parent%global=1");
        Function.FromBlitzSymbol("%CountChildren%entity");
        Function.FromBlitzSymbol("%GetChild%entity%index");
        Function.FromBlitzSymbol("%FindChild%entity$name");
        Function.FromBlitzSymbol("PaintEntity%entity%brush");
        Function.FromBlitzSymbol("EntityColor%entity#red#green#blue");
        Function.FromBlitzSymbol("EntityAlpha%entity#alpha");
        Function.FromBlitzSymbol("EntityShininess%entity#shininess");
        Function.FromBlitzSymbol("EntityTexture%entity%texture%frame=0%index=0");
        Function.FromBlitzSymbol("EntityBlend%entity%blend");
        Function.FromBlitzSymbol("EntityFX%entity%fx");
        Function.FromBlitzSymbol("EntityAutoFade%entity#near#far");
        Function.FromBlitzSymbol("EntityOrder%entity%order");
        Function.FromBlitzSymbol("HideEntity%entity");
        Function.FromBlitzSymbol("ShowEntity%entity");
        Function.FromBlitzSymbol("%EntityHidden%entity");
        Function.FromBlitzSymbol("FreeEntity%entity");
        Function.FromBlitzSymbol("NameEntity%entity$name");
        Function.FromBlitzSymbol("$EntityName%entity");
        Function.FromBlitzSymbol("$EntityClass%entity");
        Function.FromBlitzSymbol("%MemoryLoad");
        Function.FromBlitzSymbol("%TotalPhys");
        Function.FromBlitzSymbol("%AvailPhys");
        Function.FromBlitzSymbol("%TotalVirtual");
        Function.FromBlitzSymbol("%AvailVirtual");
        Function.FromBlitzSymbol("%OpenFile$filename");
        Function.FromBlitzSymbol("%ReadFile$filename");
        Function.FromBlitzSymbol("%WriteFile$filename");
        Function.FromBlitzSymbol("CloseFile%file_stream");
        Function.FromBlitzSymbol("%FilePos%file_stream");
        Function.FromBlitzSymbol("%SeekFile%file_stream%pos");
        Function.FromBlitzSymbol("%ReadDir$dirname");
        Function.FromBlitzSymbol("CloseDir%dir");
        Function.FromBlitzSymbol("$NextFile%dir");
        Function.FromBlitzSymbol("$CurrentDir");
        Function.FromBlitzSymbol("ChangeDir$dir");
        Function.FromBlitzSymbol("CreateDir$dir");
        Function.FromBlitzSymbol("DeleteDir$dir");
        Function.FromBlitzSymbol("%FileSize$file");
        Function.FromBlitzSymbol("%FileType$file");
        Function.FromBlitzSymbol("$FileExtension$file");
        Function.FromBlitzSymbol("CopyFile$file$to");
        Function.FromBlitzSymbol("DeleteFile$file");
        Function.FromBlitzSymbol("%CountGfxDrivers");
        Function.FromBlitzSymbol("$GfxDriverName%driver");
        Function.FromBlitzSymbol("SetGfxDriver%driver");
        Function.FromBlitzSymbol("%CountGfxModes");
        Function.FromBlitzSymbol("%GfxModeExists%width%height%depth");
        Function.FromBlitzSymbol("%GfxModeWidth%mode");
        Function.FromBlitzSymbol("%GfxModeHeight%mode");
        Function.FromBlitzSymbol("%GfxModeDepth%mode");
        Function.FromBlitzSymbol("%AvailVidMem");
        Function.FromBlitzSymbol("%TotalVidMem");
        Function.FromBlitzSymbol("%GfxDriver3D%driver");
        Function.FromBlitzSymbol("%CountGfxModes3D");
        Function.FromBlitzSymbol("%GfxMode3DExists%width%height%depth");
        Function.FromBlitzSymbol("%GfxMode3D%mode");
        Function.FromBlitzSymbol("%Windowed3D");
        Function.FromBlitzSymbol("Graphics%width%height%depth=0%mode=0");
        Function.FromBlitzSymbol("Graphics3D%width%height%depth=0%mode=0");
        Function.FromBlitzSymbol("EndGraphics");
        Function.FromBlitzSymbol("%GraphicsLost");
        Function.FromBlitzSymbol("%InFocus");
        Function.FromBlitzSymbol("SetGamma%src_red%src_green%src_blue#dest_red#dest_green#dest_blue");
        Function.FromBlitzSymbol("UpdateGamma%calibrate=0");
        Function.FromBlitzSymbol("#GammaRed%red");
        Function.FromBlitzSymbol("#GammaGreen%green");
        Function.FromBlitzSymbol("#GammaBlue%blue");
        Function.FromBlitzSymbol("%FrontBuffer");
        Function.FromBlitzSymbol("%BackBuffer");
        Function.FromBlitzSymbol("%ScanLine");
        Function.FromBlitzSymbol("VWait%frames=1");
        Function.FromBlitzSymbol("Flip%vwait=1");
        Function.FromBlitzSymbol("%GraphicsWidth");
        Function.FromBlitzSymbol("%GraphicsHeight");
        Function.FromBlitzSymbol("%GraphicsDepth");
        Function.FromBlitzSymbol("SetBuffer%buffer");
        Function.FromBlitzSymbol("%GraphicsBuffer");
        Function.FromBlitzSymbol("%LoadBuffer%buffer$bmpfile");
        Function.FromBlitzSymbol("%SaveBuffer%buffer$bmpfile");
        Function.FromBlitzSymbol("BufferDirty%buffer");
        Function.FromBlitzSymbol("LockBuffer%buffer=0");
        Function.FromBlitzSymbol("UnlockBuffer%buffer=0");
        Function.FromBlitzSymbol("%ReadPixel%x%y%buffer=0");
        Function.FromBlitzSymbol("WritePixel%x%y%argb%buffer=0");
        Function.FromBlitzSymbol("%ReadPixelFast%x%y%buffer=0");
        Function.FromBlitzSymbol("WritePixelFast%x%y%argb%buffer=0");
        Function.FromBlitzSymbol("CopyPixel%src_x%src_y%src_buffer%dest_x%dest_y%dest_buffer=0");
        Function.FromBlitzSymbol("CopyPixelFast%src_x%src_y%src_buffer%dest_x%dest_y%dest_buffer=0");
        Function.FromBlitzSymbol("Origin%x%y");
        Function.FromBlitzSymbol("Viewport%x%y%width%height");
        Function.FromBlitzSymbol("Color%red%green%blue");
        Function.FromBlitzSymbol("GetColor%x%y");
        Function.FromBlitzSymbol("%ColorRed");
        Function.FromBlitzSymbol("%ColorGreen");
        Function.FromBlitzSymbol("%ColorBlue");
        Function.FromBlitzSymbol("ClsColor%red%green%blue");
        Function.FromBlitzSymbol("SetFont%font");
        Function.FromBlitzSymbol("Cls");
        Function.FromBlitzSymbol("Plot%x%y");
        Function.FromBlitzSymbol("Rect%x%y%width%height%solid=1");
        Function.FromBlitzSymbol("Oval%x%y%width%height%solid=1");
        Function.FromBlitzSymbol("Line%x1%y1%x2%y2");
        Function.FromBlitzSymbol("Text%x%y$text%centre_x=0%centre_y=0");
        Function.FromBlitzSymbol("CopyRect%source_x%source_y%width%height%dest_x%dest_y%src_buffer=0%dest_buffer=0");
        Function.FromBlitzSymbol("CopyRectStretch%source_x%source_y%width%height%dest_x%dest_y%dest_w%dest_h%src_buffer=0%dest_buffer=0");
        Function.FromBlitzSymbol("%LoadFont$fontname%height=12");
        Function.FromBlitzSymbol("FreeFont%font");
        Function.FromBlitzSymbol("%FontWidth");
        Function.FromBlitzSymbol("%FontHeight");
        Function.FromBlitzSymbol("%StringWidth$string");
        Function.FromBlitzSymbol("%StringHeight$string");
        Function.FromBlitzSymbol("%OpenMovie$file");
        Function.FromBlitzSymbol("%DrawMovie%movie%x=0%y=0%w=-1%h=-1");
        Function.FromBlitzSymbol("%MovieWidth%movie");
        Function.FromBlitzSymbol("%MovieHeight%movie");
        Function.FromBlitzSymbol("%MoviePlaying%movie");
        Function.FromBlitzSymbol("CloseMovie%movie");
        Function.FromBlitzSymbol("%LoadImage$bmpfile");
        Function.FromBlitzSymbol("%LoadAnimImage$bmpfile%cellwidth%cellheight%first%count");
        Function.FromBlitzSymbol("%CopyImage%image");
        Function.FromBlitzSymbol("%CreateImage%width%height%frames=1");
        Function.FromBlitzSymbol("FreeImage%image");
        Function.FromBlitzSymbol("%SaveImage%image$bmpfile%frame=0");
        Function.FromBlitzSymbol("GrabImage%image%x%y%frame=0");
        Function.FromBlitzSymbol("%ImageBuffer%image%frame=0");
        Function.FromBlitzSymbol("DrawImage%image%x%y%frame=0");
        Function.FromBlitzSymbol("DrawBlock%image%x%y%frame=0");
        Function.FromBlitzSymbol("TileImage%image%x=0%y=0%frame=0");
        Function.FromBlitzSymbol("TileBlock%image%x=0%y=0%frame=0");
        Function.FromBlitzSymbol("DrawImageRect%image%x%y%rect_x%rect_y%rect_width%rect_height%frame=0");
        Function.FromBlitzSymbol("DrawBlockRect%image%x%y%rect_x%rect_y%rect_width%rect_height%frame=0");
        Function.FromBlitzSymbol("MaskImage%image%red%green%blue");
        Function.FromBlitzSymbol("HandleImage%image%x%y");
        Function.FromBlitzSymbol("MidHandle%image");
        Function.FromBlitzSymbol("AutoMidHandle%enable");
        Function.FromBlitzSymbol("%ImageWidth%image");
        Function.FromBlitzSymbol("%ImageHeight%image");
        Function.FromBlitzSymbol("%ImageXHandle%image");
        Function.FromBlitzSymbol("%ImageYHandle%image");
        Function.FromBlitzSymbol("ScaleImage%image#xscale#yscale");
        Function.FromBlitzSymbol("ResizeImage%image#width#height");
        Function.FromBlitzSymbol("RotateImage%image#angle");
        Function.FromBlitzSymbol("TFormImage%image#a#b#c#d");
        Function.FromBlitzSymbol("TFormFilter%enable");
        Function.FromBlitzSymbol("%ImagesOverlap%image1%x1%y1%image2%x2%y2");
        Function.FromBlitzSymbol("%ImagesCollide%image1%x1%y1%frame1%image2%x2%y2%frame2");
        Function.FromBlitzSymbol("%RectsOverlap%x1%y1%width1%height1%x2%y2%width2%height2");
        Function.FromBlitzSymbol("%ImageRectOverlap%image%x%y%rect_x%rect_y%rect_width%rect_height");
        Function.FromBlitzSymbol("%ImageRectCollide%image%x%y%frame%rect_x%rect_y%rect_width%rect_height");
        Function.FromBlitzSymbol("Write$string");
        Function.FromBlitzSymbol("Print$string=\"\"");
        Function.FromBlitzSymbol("$Input$prompt=\"\"");
        Function.FromBlitzSymbol("Locate%x%y");
        Function.FromBlitzSymbol("ShowPointer");
        Function.FromBlitzSymbol("HidePointer");
        Function.FromBlitzSymbol("%DesktopWidth");
        Function.FromBlitzSymbol("%DesktopHeight");
        Function.FromBlitzSymbol("%KeyDown%key");
        Function.FromBlitzSymbol("%KeyHit%key");
        Function.FromBlitzSymbol("%GetKey");
        Function.FromBlitzSymbol("%WaitKey");
        Function.FromBlitzSymbol("$TextInput$txt");
        Function.FromBlitzSymbol("FlushKeys");
        Function.FromBlitzSymbol("%MouseDown%button");
        Function.FromBlitzSymbol("%MouseHit%button");
        Function.FromBlitzSymbol("%GetMouse");
        Function.FromBlitzSymbol("%WaitMouse");
        Function.FromBlitzSymbol("%MouseWait");
        Function.FromBlitzSymbol("%MouseX");
        Function.FromBlitzSymbol("%MouseY");
        Function.FromBlitzSymbol("%MouseZ");
        Function.FromBlitzSymbol("%MouseXSpeed");
        Function.FromBlitzSymbol("%MouseYSpeed");
        Function.FromBlitzSymbol("%MouseZSpeed");
        Function.FromBlitzSymbol("FlushMouse");
        Function.FromBlitzSymbol("MoveMouse%x%y");
        Function.FromBlitzSymbol("%JoyType%port=0");
        Function.FromBlitzSymbol("%JoyDown%button%port=0");
        Function.FromBlitzSymbol("%JoyHit%button%port=0");
        Function.FromBlitzSymbol("%GetJoy%port=0");
        Function.FromBlitzSymbol("%WaitJoy%port=0");
        Function.FromBlitzSymbol("%JoyWait%port=0");
        Function.FromBlitzSymbol("#JoyX%port=0");
        Function.FromBlitzSymbol("#JoyY%port=0");
        Function.FromBlitzSymbol("#JoyZ%port=0");
        Function.FromBlitzSymbol("#JoyU%port=0");
        Function.FromBlitzSymbol("#JoyV%port=0");
        Function.FromBlitzSymbol("#JoyPitch%port=0");
        Function.FromBlitzSymbol("#JoyYaw%port=0");
        Function.FromBlitzSymbol("#JoyRoll%port=0");
        Function.FromBlitzSymbol("%JoyHat%port=0");
        Function.FromBlitzSymbol("%JoyXDir%port=0");
        Function.FromBlitzSymbol("%JoyYDir%port=0");
        Function.FromBlitzSymbol("%JoyZDir%port=0");
        Function.FromBlitzSymbol("%JoyUDir%port=0");
        Function.FromBlitzSymbol("%JoyVDir%port=0");
        Function.FromBlitzSymbol("FlushJoy");
        Function.FromBlitzSymbol("EnableDirectInput%enable");
        Function.FromBlitzSymbol("%DirectInputEnabled");
        Function.FromBlitzSymbol("#Sin#degrees");
        Function.FromBlitzSymbol("#Cos#degrees");
        Function.FromBlitzSymbol("#Tan#degrees");
        Function.FromBlitzSymbol("#ASin#float");
        Function.FromBlitzSymbol("#ACos#float");
        Function.FromBlitzSymbol("#ATan#float");
        Function.FromBlitzSymbol("#ATan2#floata#floatb");
        Function.FromBlitzSymbol("#Sqr#float");
        Function.FromBlitzSymbol("#Floor#float");
        Function.FromBlitzSymbol("#Ceil#float");
        Function.FromBlitzSymbol("#Exp#float");
        Function.FromBlitzSymbol("#Log#float");
        Function.FromBlitzSymbol("#Log10#float");
        Function.FromBlitzSymbol("#Min#n#m");
        Function.FromBlitzSymbol("#Max#n#m");
        Function.FromBlitzSymbol("#Clamp#v#lo#hi");
        Function.FromBlitzSymbol("%IsNaN#n");
        Function.FromBlitzSymbol("#Rnd#from#to=0");
        Function.FromBlitzSymbol("%Rand%from%to=1");
        Function.FromBlitzSymbol("SeedRnd%seed");
        Function.FromBlitzSymbol("%RndSeed");
        Function.FromBlitzSymbol("End");
        Function.FromBlitzSymbol("Stop");
        Function.FromBlitzSymbol("AppTitle$title$close_prompt=\"\"");
        Function.FromBlitzSymbol("RuntimeError$message");
        Function.FromBlitzSymbol("InitErrorMsgs%number");
        Function.FromBlitzSymbol("SetErrorMsg%pos$message");
        Function.FromBlitzSymbol("ExecFile$command");
        Function.FromBlitzSymbol("Delay%millisecs");
        Function.FromBlitzSymbol("%MilliSecs");
        Function.FromBlitzSymbol("$CommandLine");
        Function.FromBlitzSymbol("$SystemProperty$property");
        Function.FromBlitzSymbol("$GetEnv$env_var");
        Function.FromBlitzSymbol("SetEnv$env_var$value");
        Function.FromBlitzSymbol("%CreateTimer%hertz");
        Function.FromBlitzSymbol("%WaitTimer%timer");
        Function.FromBlitzSymbol("FreeTimer%timer");
        Function.FromBlitzSymbol("$GetClipboardContents");
        Function.FromBlitzSymbol("SetClipboardContents$contents");
        Function.FromBlitzSymbol("DebugLog$text");
        Function.FromBlitzSymbol("_bbDebugStmt");
        Function.FromBlitzSymbol("_bbDebugEnter");
        Function.FromBlitzSymbol("_bbDebugLeave");
        Function.FromBlitzSymbol("$DottedIP%IP");
        Function.FromBlitzSymbol("%CountHostIPs$host_name");
        Function.FromBlitzSymbol("%HostIP%host_index");
        Function.FromBlitzSymbol("%CreateUDPStream%port=0");
        Function.FromBlitzSymbol("CloseUDPStream%udp_stream");
        Function.FromBlitzSymbol("SendUDPMsg%udp_stream%dest_ip%dest_port=0");
        Function.FromBlitzSymbol("%RecvUDPMsg%udp_stream");
        Function.FromBlitzSymbol("%UDPStreamIP%udp_stream");
        Function.FromBlitzSymbol("%UDPStreamPort%udp_stream");
        Function.FromBlitzSymbol("%UDPMsgIP%udp_stream");
        Function.FromBlitzSymbol("%UDPMsgPort%udp_stream");
        Function.FromBlitzSymbol("UDPTimeouts%recv_timeout");
        Function.FromBlitzSymbol("%OpenTCPStream$server%server_port%local_port=0");
        Function.FromBlitzSymbol("CloseTCPStream%tcp_stream");
        Function.FromBlitzSymbol("%CreateTCPServer%port");
        Function.FromBlitzSymbol("CloseTCPServer%tcp_server");
        Function.FromBlitzSymbol("%AcceptTCPStream%tcp_server");
        Function.FromBlitzSymbol("%TCPStreamIP%tcp_stream");
        Function.FromBlitzSymbol("%TCPStreamPort%tcp_stream");
        Function.FromBlitzSymbol("TCPTimeouts%read_millis%accept_millis");
        Function.FromBlitzSymbol("%Eof%stream");
        Function.FromBlitzSymbol("%ReadAvail%stream");
        Function.FromBlitzSymbol("%ReadByte%stream");
        Function.FromBlitzSymbol("%ReadShort%stream");
        Function.FromBlitzSymbol("%ReadInt%stream");
        Function.FromBlitzSymbol("#ReadFloat%stream");
        Function.FromBlitzSymbol("$ReadString%stream");
        Function.FromBlitzSymbol("$ReadLine%stream");
        Function.FromBlitzSymbol("WriteByte%stream%byte");
        Function.FromBlitzSymbol("WriteShort%stream%short");
        Function.FromBlitzSymbol("WriteInt%stream%int");
        Function.FromBlitzSymbol("WriteFloat%stream#float");
        Function.FromBlitzSymbol("WriteString%stream$string");
        Function.FromBlitzSymbol("WriteLine%stream$string");
        Function.FromBlitzSymbol("CopyStream%src_stream%dest_stream%buffer_size=16384");
        Function.FromBlitzSymbol("$String$string%repeat");
        Function.FromBlitzSymbol("$Left$string%count");
        Function.FromBlitzSymbol("$Right$string%count");
        Function.FromBlitzSymbol("$Replace$string$from$to");
        Function.FromBlitzSymbol("%Instr$string$find%from=1");
        Function.FromBlitzSymbol("$Mid$string%start%count=-1");
        Function.FromBlitzSymbol("$Upper$string");
        Function.FromBlitzSymbol("$Lower$string");
        Function.FromBlitzSymbol("$Trim$string");
        Function.FromBlitzSymbol("$LSet$string%size");
        Function.FromBlitzSymbol("$RSet$string%size");
        Function.FromBlitzSymbol("$Chr%ascii");
        Function.FromBlitzSymbol("%Asc$string");
        Function.FromBlitzSymbol("%Len$string");
        Function.FromBlitzSymbol("$Hex%value");
        Function.FromBlitzSymbol("$Bin%value");
        Function.FromBlitzSymbol("$CurrentDate");
        Function.FromBlitzSymbol("$CurrentTime");

        _ = new Function("_builtIn__bbAbs", DeclType.Int) { ReturnType = DeclType.Int };
        _ = new Function("_builtIn__bbFAbs", DeclType.Float) { ReturnType = DeclType.Float };
        _ = new Function("_builtIn__bbSgn", DeclType.Int) { ReturnType = DeclType.Int };
        _ = new Function("_builtIn__bbFSgn", DeclType.Float) { ReturnType = DeclType.Float };
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
        _ = new Function("_builtIn__bbStrToCStr", DeclType.String) { ReturnType = DeclType.String };
        _ = new Function("_builtIn__bbCStrToStr", DeclType.String) { ReturnType = DeclType.String };

        _ = new Function("_builtIn_ferrorlog", 0) { ReturnType = DeclType.String };

        _ = new Function("_builtIn__bbVecAlloc", 1);
        _ = new Function("_builtIn__bbVecFree", 2);

        _ = new Function("_builtIn__bbRestore", 1);
        _ = new Function("_builtIn__bbReadInt", 0);
        _ = new Function("_builtIn__bbReadFloat", 0);
        _ = new Function("_builtIn__bbReadStr", 0);

        _ = new Function("_builtIn__bbLoadLibs", 1);

        _ = new Function("_builtIn__bbObjEachFirst", 2);
        _ = new Function("_builtIn__bbObjEachNext", 1);
        _ = new Function("_builtIn__bbObjEachFirst2", 2);
        _ = new Function("_builtIn__bbObjEachNext2", 1);
        _ = new Function("_builtIn__bbObjFromHandle", DeclType.Int, DeclType.Unknown);
        _ = new Function("_builtIn__bbObjToHandle", 1) { ReturnType = DeclType.Int };
        _ = new Function("_builtIn__bbObjNew", 1);
        _ = new Function("_builtIn__bbObjFirst", 1);
        _ = new Function("_builtIn__bbObjLast", 1);
        _ = new Function("_builtIn__bbObjNext", 1);
        _ = new Function("_builtIn__bbObjPrev", 1);
        _ = new Function("_builtIn__bbObjCompare", 2);
        _ = new Function("_builtIn__bbObjRelease", 1);
        _ = new Function("_builtIn__bbObjStore", 2);
        _ = new Function("_builtIn__bbObjLoad", 1);
        _ = new Function("_builtIn__bbFieldPtrAdd", 2);
        _ = new Function("_builtIn__bbObjDelete", 1);
        _ = new Function("_builtIn__bbObjDeleteEach", 1);
        _ = new Function("_builtIn__bbObjInsBefore", 2);
        _ = new Function("_builtIn__bbObjInsAfter", 2);
    }
}
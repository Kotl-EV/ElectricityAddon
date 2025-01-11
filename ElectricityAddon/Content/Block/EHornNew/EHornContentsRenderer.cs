using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ElectricityAddon.Content.Block.EHornNew;

public class EHornContentsRenderer : IRenderer, ITexPositionSource
{
    private ICoreClientAPI capi;
    private BlockPos pos;

    MeshRef workItemMeshRef;

    MeshRef heQuadRef;


    ItemStack stack;
    bool burning;

    TextureAtlasPosition hetexpos;

    int textureId;


    string tmpMetal;
    ITexPositionSource tmpTextureSource;

    Matrixf ModelMat = new Matrixf();



    public double RenderOrder
    {
        get { return 0.5; }
    }

    public int RenderRange
    {
        get { return 24; }
    }

    public Size2i AtlasSize
    {
        get { return capi.BlockTextureAtlas.Size; }
    }

    public TextureAtlasPosition this[string textureCode]
    {
        get { return tmpTextureSource[tmpMetal]; }
    }
    
    public EHornContentsRenderer(BlockPos pos, ICoreClientAPI capi)
    {
        this.pos = pos;
        this.capi = capi;

        Vintagestory.API.Common.Block block = capi.World.GetBlock(new AssetLocation("temporalenergetics:tfforge"));

        hetexpos = capi.BlockTextureAtlas.GetPosition(block, "iron");

        MeshData heMesh;
        Shape ovshape = capi.Assets.TryGet(new AssetLocation("temporalenergetics:shapes/block/tfforge/heating_element.json")).ToObject<Shape>();
        capi.Tesselator.TesselateShape(block, ovshape, out heMesh);

        for (int i = 0; i < heMesh.Uv.Length; i += 2)
        {
            heMesh.Uv[i + 0] = hetexpos.x1 + heMesh.Uv[i + 0] * 32f / AtlasSize.Width;
            heMesh.Uv[i + 1] = hetexpos.y1 + heMesh.Uv[i + 1] * 32f / AtlasSize.Height;
        }

        heQuadRef = capi.Render.UploadMesh(heMesh);
    }

    public void SetContents(ItemStack stack, bool burning, bool regen)
    {
        this.stack = stack;
        this.burning = burning;

        if (regen) RegenMesh();
    }


    void RegenMesh()
    {
        if (workItemMeshRef != null) workItemMeshRef.Dispose();
        workItemMeshRef = null;
        if (stack == null) return;

        Shape shape;

        tmpMetal = stack.Collectible.LastCodePart();
        MeshData mesh = null;

        string firstCodePart = stack.Collectible.FirstCodePart();
        if (firstCodePart == "metalplate")
        {
            tmpTextureSource = capi.Tesselator.GetTexSource(capi.World.GetBlock(new AssetLocation("platepile")));
            shape = capi.Assets.TryGet("shapes/block/stone/forge/platepile.json").ToObject<Shape>();
            textureId = tmpTextureSource[tmpMetal].atlasTextureId;
            capi.Tesselator.TesselateShape("block-fcr", shape, out mesh, this, null, 0, 0, 0, stack.StackSize);

        }
        else if (firstCodePart == "workitem")
        {
            MeshData workItemMesh = ItemWorkItem.GenMesh(capi, stack, ItemWorkItem.GetVoxels(stack), out textureId);
            workItemMesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.75f, 0.75f, 0.75f);
            workItemMesh.Translate(0, -9f / 16f, 0);
            workItemMeshRef = capi.Render.UploadMesh(workItemMesh);
        }
        else if (firstCodePart == "ingot")
        {
            tmpTextureSource = capi.Tesselator.GetTexSource(capi.World.GetBlock(new AssetLocation("ingotpile")));
            shape = capi.Assets.TryGet("shapes/block/stone/forge/ingotpile.json").ToObject<Shape>();
            textureId = tmpTextureSource[tmpMetal].atlasTextureId;
            capi.Tesselator.TesselateShape("block-fcr", shape, out mesh, this, null, 0, 0, 0, stack.StackSize);
        }
        else if (stack.Collectible.Attributes != null && stack.Collectible.Attributes.IsTrue("forgable") == true)
        {
            if (stack.Class == EnumItemClass.Block)
            {
                mesh = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
                //textureId = capi.BlockTextureAtlas.AtlasTextureIds[0];
            }
            else
            {
                capi.Tesselator.TesselateItem(stack.Item, out mesh);
                //textureId = capi.ItemTextureAtlas.AtlasTextures[];
            }

            ModelTransform tf = stack.Collectible.Attributes["inForgeTransform"].AsObject<ModelTransform>();
            if (tf != null)
            {
                tf.EnsureDefaultValues();
                mesh.ModelTransform(tf);
            }
        }

        if (mesh != null)
        {
            workItemMeshRef = capi.Render.UploadMesh(mesh);
        }
    }



    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        IRenderAPI rpi = capi.Render;
        IClientWorldAccessor worldAccess = capi.World;
        Vec3d camPos = worldAccess.Player.Entity.CameraPos;

        rpi.GlDisableCullFace();
        IStandardShaderProgram prog = rpi.StandardShader;
        prog.Use();
        prog.RgbaAmbientIn = rpi.AmbientColor;
        prog.RgbaFogIn = rpi.FogColor;
        prog.FogMinIn = rpi.FogMin;
        prog.FogDensityIn = rpi.FogDensity;
        prog.RgbaTint = ColorUtil.WhiteArgbVec;
        prog.DontWarpVertices = 0;
        prog.AddRenderFlags = 0;
        prog.ExtraGodray = 0;
        prog.OverlayOpacity = 0;

        Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);

        if (stack != null && workItemMeshRef != null)
        {
            int temp = (int)stack.Collectible.GetTemperature(capi.World, stack);

            //Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
            float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(temp);
            int extraGlow = GameMath.Clamp((temp - 550) / 2, 0, 255);

            prog.NormalShaded = 1;
            prog.RgbaLightIn = lightrgbs;
            prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], extraGlow / 255f);

            prog.ExtraGlow = extraGlow;
            prog.Tex2D = textureId;
            prog.ModelMatrix = ModelMat.Identity().Translate(pos.X - camPos.X, pos.Y - camPos.Y + 10 / 16f, pos.Z - camPos.Z).Values;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

            rpi.RenderMesh(workItemMeshRef);
        }

        if (burning)
        {
            float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(1200);
            prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], 1);
        }
        else
        {
            prog.RgbaGlowIn = new Vec4f(0, 0, 0, 0);
        }

        prog.NormalShaded = 1;
        prog.RgbaLightIn = lightrgbs;

        prog.ExtraGlow = burning ? 255 : 0;


        rpi.BindTexture2d(hetexpos.atlasTextureId);

        prog.ModelMatrix = ModelMat.Identity().Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z).Values;
        prog.ViewMatrix = rpi.CameraMatrixOriginf;
        prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

        rpi.RenderMesh(heQuadRef);

        prog.Stop();
    }



    public void Dispose()
    {
        capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        if (heQuadRef != null) heQuadRef.Dispose();
        if (workItemMeshRef != null) workItemMeshRef.Dispose();
    }
}
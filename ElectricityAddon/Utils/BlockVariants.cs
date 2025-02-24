using ElectricityAddon.Content.Block.ECable;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Utils;

public class BlockVariants
{
    public readonly Cuboidf[] CollisionBoxes;
    public readonly MeshData? MeshData;
    public readonly Cuboidf[] SelectionBoxes;

    /// <summary>
    /// Извлекаем нужный вариант блока провода
    /// </summary>
    /// <param name="api"></param>
    /// <param name="baseBlock"></param>
    /// <param name="indexMaterial"></param>
    /// <param name="indexQuantity"></param>
    /// <param name="indexType"></param>
    public BlockVariants(ICoreAPI api, CollectibleObject baseBlock, int indexMaterial, int indexQuantity, int indexType)
    {
        string[] t = new string[3];
        string[] v = new string[3];

        t[0] = "material";
        t[1] = "quantity";
        t[2] = "type";

        v[0] = BlockECable.materials[indexMaterial];
        v[1] = BlockECable.quantitys[indexQuantity];
        v[2] = BlockECable.types[indexType];

        var assetLocation = baseBlock.CodeWithVariants(t, v);
        var block = api.World.GetBlock(assetLocation);

        this.CollisionBoxes = block.CollisionBoxes;
        this.SelectionBoxes = block.SelectionBoxes;

        if (api is ICoreClientAPI clientApi)
        {
            var cachedShape = clientApi.TesselatorManager.GetCachedShape(block.Shape.Base);

            clientApi.Tesselator.TesselateShape(baseBlock, cachedShape, out this.MeshData);
        }
    }



}

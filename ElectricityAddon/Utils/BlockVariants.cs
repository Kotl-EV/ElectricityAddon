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
    public BlockVariants(ICoreAPI api, CollectibleObject baseBlock, int indexVoltage, int indexMaterial, int indexQuantity, int indexType)
    {
        if (indexQuantity == 0)
            return;

        string[] t = new string[4];
        string[] v = new string[4];

        t[0] = "voltage";
        t[1] = "material";
        t[2] = "quantity";
        t[3] = "type";

        v[0] = BlockECable.voltages[indexVoltage];
        v[1] = BlockECable.materials[indexMaterial];
        v[2] = BlockECable.quantitys[indexQuantity];  
        v[3] = BlockECable.types[indexType];

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

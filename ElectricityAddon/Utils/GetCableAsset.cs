using ElectricityAddon.Content.Block.ECable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ElectricityAddon.Utils
{
    class GetCableAsset
    {
        /// <summary>
        /// Извлекаем нужный вариант блока провода
        /// </summary>
        /// <param name="api"></param>
        /// <param name="baseBlock"></param>
        /// <param name="indexMaterial"></param>
        /// <param name="indexQuantity"></param>
        /// <param name="indexType"></param>
        public Block CableAsset(ICoreAPI api, CollectibleObject baseBlock, int indexMaterial, int indexQuantity, int indexType)
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

            return api.World.GetBlock(assetLocation);

        }
    }
}

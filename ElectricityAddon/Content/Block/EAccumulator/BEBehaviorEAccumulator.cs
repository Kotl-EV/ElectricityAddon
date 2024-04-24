using System.Text;
using Electricity.Interface;
using Electricity.Utils;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace ElectricityAddon.Content.Block.EAccumulator;

public class BEBehaviorEAccumulator : BlockEntityBehavior, IElectricAccumulator {
    public int capacity;
    public BEBehaviorEAccumulator(BlockEntity blockEntity) : base(blockEntity) {
    }
    public int GetMaxCapacity()
    {
        return MyMiniLib.GetAttributeInt(this.Block, "maxcapacity",16000);
    }

    public int GetCapacity() {
        return capacity;
    }
    public void Store(int amount) {
        capacity += amount;
    }
    public void Release(int amount) {
        capacity -= amount;
    }
    public override void ToTreeAttributes(ITreeAttribute tree) {
        base.ToTreeAttributes(tree);
        tree.SetInt("electricity:energy", capacity);
    }
    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        capacity = tree.GetInt("electricity:energy");
    }
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder stringBuilder) {
        base.GetBlockInfo(forPlayer, stringBuilder);
        stringBuilder.AppendLine(StringHelper.Progressbar(GetCapacity() * 100.0f / GetMaxCapacity()));
        stringBuilder.AppendLine("└ " + Lang.Get("Storage") + GetCapacity() + "/" + GetMaxCapacity() + "Eu");
        stringBuilder.AppendLine();
    }
}
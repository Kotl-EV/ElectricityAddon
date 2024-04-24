using Vintagestory.API.Common;

namespace ElectricityAddon.Interface;

public interface IEnergyStorageItem
{
    int receiveEnergy(ItemStack itemstack, int maxReceive);
}
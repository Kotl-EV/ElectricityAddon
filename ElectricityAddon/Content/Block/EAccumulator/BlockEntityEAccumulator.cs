using ElectricityAddon.Content.Block;
using ElectricityAddon.Utils;
using Vintagestory.API.Common;

namespace ElectricityAddon.Content.Block.EAccumulator;

public class BlockEntityEAccumulator : BlockEntity
{
    private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();


    


    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);

        this.ElectricityAddon.Connection = Facing.DownAll;
        this.ElectricityAddon.Eparams= new float[7]
                        {
                            10,                                 //������������ ���
                            0,                                  //----
                            0,                                  //������ ������� � �������� ����
                            1,                                  //���������� ����� �������� ����/�������
                            0,                                  //���������� (�������� ����� ��� �����)
                            0,                                  //������ ��� ���
                            32                                  //����������
                        };

    }
}

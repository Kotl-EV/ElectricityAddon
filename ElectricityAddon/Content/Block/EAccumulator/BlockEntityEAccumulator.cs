using ElectricityAddon.Content.Block;
using ElectricityAddon.Utils;
using System.Linq;
using Vintagestory.API.Common;

namespace ElectricityAddon.Content.Block.EAccumulator;

public class BlockEntityEAccumulator : BlockEntity
{
    private BEBehaviorElectricityAddon? ElectricityAddon => GetBehavior<BEBehaviorElectricityAddon>();


    


    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);

        this.ElectricityAddon.Connection = Facing.DownAll;
        this.ElectricityAddon.Eparams= (new float[7]
                        {
                            10,                                 //������������ ���
                            0,                                  //������ ���������?!!!
                            0,                                  //������ ������� � �������� ����
                            1,                                  //���������� ����� �������� ����/�������
                            1,                                  //������� ������� ����� ���� (def=1)
                            0,                                  //������ ��� ���
                            32                                  //����������
                        },
                        FacingHelper.Faces(Facing.DownAll).First().Index);

    }
}

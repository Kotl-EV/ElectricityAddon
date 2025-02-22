using ElectricityAddon.Content.Block;
using ElectricityAddon.Content.Block.ECable;
using System.Linq;
using Vintagestory.API.MathTools;
using static ElectricityAddon.Content.Block.ECable.BlockECable;


namespace ElectricityAddon.Utils
{
    class SelectionFacingCable
    {

        /// <summary>
        /// Выводит грань выключателя
        /// </summary>
        /// <param name="key"></param>
        /// <param name="hitPosition"></param>
        /// <param name="Cable"></param>
        /// <returns></returns>
        public Facing SelectionFacingSwitch(CacheDataKey key, Vec3d hitPosition, BlockECable Cable)
        {
            var selectedFacing = (
                        from keyValuePair in BlockECable.CalculateBoxes(
                            key,
                            BlockECable.SelectionBoxesCache,
                            Cable.dotVariant!.SelectionBoxes,
                            Cable.partVariant!.SelectionBoxes,
                            Cable.enabledSwitchVariant!.SelectionBoxes,
                            Cable.disabledSwitchVariant!.SelectionBoxes
                        )
                        let selectionFacing = keyValuePair.Key
                        let selectionBoxes = keyValuePair.Value
                        from selectionBox in selectionBoxes
                        where selectionBox.Clone()
                            .OmniGrowBy(0.005f)
                            .Contains(hitPosition.X, hitPosition.Y, hitPosition.Z)
                        select selectionFacing
                    )
                    .Aggregate(Facing.None, (current, selectionFacing) => current | selectionFacing);


            foreach (var face in FacingHelper.Faces(selectedFacing))
            {
                selectedFacing |= FacingHelper.FromFace(face);
            }

            return selectedFacing;
        }




        /// <summary>
        /// Выводит направления, на которые наведен курсор
        /// </summary>
        /// <param name="key"></param>
        /// <param name="hitPosition"></param>
        /// <param name="Cable"></param>
        /// <returns></returns>
        public Facing SelectionFacing(CacheDataKey key, Vec3d hitPosition, BlockECable Cable)
        {

            var selectedFacing = (
                            from keyValuePair in BlockECable.CalculateBoxes(
                                key,
                                BlockECable.SelectionBoxesCache,
                                Cable.dotVariant!.SelectionBoxes,
                                Cable.partVariant!.SelectionBoxes,
                                Cable.enabledSwitchVariant!.SelectionBoxes,
                                Cable.disabledSwitchVariant!.SelectionBoxes
                            )
                            let selectionFacing = keyValuePair.Key
                            let selectionBoxes = keyValuePair.Value
                            from selectionBox in selectionBoxes
                            where selectionBox.Clone()
                                .OmniGrowBy(0.01f)
                                .Contains(hitPosition.X, hitPosition.Y, hitPosition.Z)
                            select selectionFacing
                        )
                        .Aggregate(
                            Facing.None,
                            (current, selectionFacing) =>
                                current | selectionFacing
                    );


            return selectedFacing;
        }
    }
}

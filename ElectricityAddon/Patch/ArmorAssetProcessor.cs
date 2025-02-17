using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

public class ArmorAssetProcessor
{
    private readonly ICoreAPI _api;

    public ArmorAssetProcessor(ICoreAPI api)
    {
        _api = api;
    }

    public void ProcessAssets(string modId, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            try
            {
                var assetLocation = new AssetLocation(modId, path);
                var asset = _api.Assets.Get(assetLocation);

                if (asset != null)
                {
                    // Преобразуем данные ассета в JObject
                    var jsonObject = asset.ToObject<JObject>();

                    if (ModifyAsset(jsonObject))
                    {
                        // Перезаписываем измененные данные обратно в ассет
                        string jsonString = jsonObject.ToString(Newtonsoft.Json.Formatting.None);
                        byte[] data = Encoding.UTF8.GetBytes(jsonString);

                        asset.Data = data;


                        _api.Assets.AllAssets[assetLocation] = asset;
                        
                        _api.Logger.Debug($"Successfully modified: {path}");
                    }
                }
            }
            catch (Exception ex)
            {
                //_api.Logger.Error($"Failed to process {path}: {ex}");
            }
        }
    }

    /// <summary>
    /// ковыряем ассет
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    private bool ModifyAsset(JObject root)
    {
        if (root["attributes"] is not JObject attributes) return false;

        bool modified = false;

        if (attributes.Remove("protectionModifiersByType")) modified = true;
        if (attributes.Remove("statModifiersByType")) modified = true;

        return modified;
    }
}
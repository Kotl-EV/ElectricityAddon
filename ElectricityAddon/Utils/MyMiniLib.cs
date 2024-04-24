using Vintagestory.API.Common;

namespace ElectricityAddon.Utils;

public static class MyMiniLib
{
    public static int GetAttributeInt(CollectibleObject block, string attrname, int def = 0)
    {
        if (block != null && block.Attributes != null && block.Attributes[attrname] != null)
        {
            return block.Attributes[attrname].AsInt(def);
        }
        return def;
    }
    
    public static string GetAttributeString(CollectibleObject block, string attrname, string def)
    {
        if (block != null && block.Attributes != null && block.Attributes[attrname] != null)
        {
            return block.Attributes[attrname].AsString(def);
        }
        return def;
    }
}
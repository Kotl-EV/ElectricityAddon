using System.Collections.Generic;
using ElectricityAddon.CustomRecipe.Recipe;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace ElectricityAddon.CustomRecipe;


public class CustomRecipeManager : ModSystem
{
    public static List<CentrifugeRecipe> CentrifugeRecipes;
    public static List<PressRecipe> PressRecipes;

    private ICoreServerAPI api;

    public override void StartServerSide(ICoreServerAPI api)
    {
        this.api = api;
        api.Event.SaveGameLoaded += CentrifugeRecipe;
        api.Event.SaveGameLoaded += PressRecipe;
    }

    public void CentrifugeRecipe()
    {
        CentrifugeRecipes = new List<CentrifugeRecipe>();
        RecipeLoader recipeLoader = api.ModLoader.GetModSystem<RecipeLoader>();
        recipeLoader.LoadRecipes<CentrifugeRecipe>("Centrifuge Recipe", "recipes/electric/centrifugerecipe", (r) => CentrifugeRecipes.Add(r));
        api.World.Logger.StoryEvent(Lang.Get("electricityaddon:recipeloading"));
    }
    
    public void PressRecipe()
    {
        PressRecipes = new List<PressRecipe>();
        RecipeLoader recipeLoader = api.ModLoader.GetModSystem<RecipeLoader>();
        recipeLoader.LoadRecipes<PressRecipe>("Press Recipe", "recipes/electric/pressrecipe", (r) => PressRecipes.Add(r));
        api.World.Logger.StoryEvent(Lang.Get("electricityaddon:recipeloading"));
    }
}

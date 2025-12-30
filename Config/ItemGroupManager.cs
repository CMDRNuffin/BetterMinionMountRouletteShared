namespace BetterRouletteBase.Config;

using System.Linq;

internal static class ItemGroupManager<TGroup> where TGroup : IItemGroup
{
    public static void Delete(ICharacterConfig<TGroup> config, string name)
    {
        for (int i = 0; i < config.Groups.Count; ++i)
        {
            if (name == config.Groups[i].Name)
            {
                config.Groups.RemoveAt(i);
                break;
            }
        }

        config.ResetSelection(name, config.Groups.FirstOrDefault()?.Name);
    }

    public static void Rename(ICharacterConfig<TGroup> config, string currentName, string newName)
    {
        TGroup? group = config.Groups.FirstOrDefault(x => x.Name == currentName);
        if (group is { } g)
        {
            g.Name = newName;
        }

        config.ResetSelection(currentName, newName);
    }
}

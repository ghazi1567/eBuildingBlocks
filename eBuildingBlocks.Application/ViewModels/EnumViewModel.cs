using Humanizer;

namespace eBuildingBlocks.Application.ViewModels;

public record EnumViewModel(
    int Key,
    string Value,
    string Description
    )
{
    public EnumViewModel(Enum enumValue) : this((int)(object)enumValue, enumValue.ToString(), enumValue.Humanize())
    {
    }
}

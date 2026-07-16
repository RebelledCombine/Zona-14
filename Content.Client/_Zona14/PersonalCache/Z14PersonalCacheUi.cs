// SPDX-License-Identifier: MIT

using Content.Client.UserInterface.Fragments;
using Content.Shared._Zona14.PersonalCache;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;

namespace Content.Client._Zona14.PersonalCache;

/// <summary>
/// PDA cartridge UI fragment that lists the owner's personal caches.
/// </summary>
public sealed partial class Z14PersonalCacheUi : UIFragment
{
    private Z14PersonalCacheUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new Z14PersonalCacheUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not Z14PersonalCacheUiState cacheState)
            return;

        _fragment?.UpdateState(cacheState);
    }
}

public sealed class Z14PersonalCacheUiFragment : BoxContainer
{
    private readonly Label _header;
    private readonly BoxContainer _entries;

    public Z14PersonalCacheUiFragment()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        VerticalExpand = true;

        _header = new Label
        {
            Text = Loc.GetString("z14-personal-cache-cartridge-header"),
            HorizontalExpand = true,
        };
        AddChild(_header);

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        _entries = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        scroll.AddChild(_entries);
        AddChild(scroll);
    }

    public void UpdateState(Z14PersonalCacheUiState state)
    {
        _entries.RemoveAllChildren();

        if (state.Caches.Count == 0)
        {
            _entries.AddChild(new Label
            {
                Text = Loc.GetString("z14-personal-cache-cartridge-empty"),
                HorizontalExpand = true,
            });
            return;
        }

        foreach (var cache in state.Caches)
        {
            var status = cache.Hidden
                ? Loc.GetString("z14-personal-cache-cartridge-hidden")
                : Loc.GetString("z14-personal-cache-cartridge-visible");

            var text = Loc.GetString("z14-personal-cache-cartridge-entry",
                ("map", cache.MapKey),
                ("x", (int) cache.X),
                ("y", (int) cache.Y),
                ("status", status),
                ("weight", cache.Weight.ToString("F1")));

            _entries.AddChild(new Label
            {
                Text = text,
                HorizontalExpand = true,
            });
        }
    }
}

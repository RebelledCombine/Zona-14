// SPDX-License-Identifier: MIT

namespace Content.Server._Zona14.MapRadiation;

[RegisterComponent]
public sealed partial class MapRadiationBlockerComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    public float Radius = 5f;
}

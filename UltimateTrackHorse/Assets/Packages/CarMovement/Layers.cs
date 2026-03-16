/// <summary>
/// Central registry of Unity layer indices used across the project.
/// Add a new constant here whenever you create a new layer in Edit > Project Settings > Tags and Layers.
/// Usage example:  LayerMask.GetMask(Layers.Names.Asphalt)
///                 gameObject.layer = Layers.Asphalt;
/// </summary>
public static class Layers
{
    // ?? Built-in Unity layers ????????????????????????????????????????????????
    public const int Default      = 0;
    public const int TransparentFX = 1;
    public const int IgnoreRaycast = 2;
    public const int Water        = 4;
    public const int UI           = 5;

    // ?? Project layers (Edit > Project Settings > Tags and Layers) ???????????
    // Add every custom layer here as a new constant.
    // The number must match the slot you assigned in the Unity editor.
    public const int Asphalt      = 6;   // drivable – normal road surface
    public const int Grass        = 7;   // drivable – grass / off-road surface
    public const int Gravel       = 8;   // drivable – gravel surface

    // ?? Helpers ??????????????????????????????????????????????????????????????
    /// <summary>String names of all drivable layers, ready for LayerMask.GetMask().</summary>
    public static class Names
    {
        public const string Asphalt = "Asphalt";
        public const string Grass   = "Grass";
        public const string Gravel  = "Gravel";
    }
}

using p4g64.debugStuff.Template.Configuration;
using System.ComponentModel;
using SharpDX.DirectInput;

namespace p4g64.debugStuff.Configuration;
public class Config : Configurable<Config>
{
    [DisplayName("Env Menu Keybind")]
    [Description("The button to press to enter the menu for editing the FBN (lighting, fog, etc)")]
    [DefaultValue(false)]
    public Key EnvMenu { get; set; } = Key.F4;
    
    [DisplayName("Fbn Menu Keybind")]
    [Description("The button to press to enter the menu for editing the FBN (NPCs, free camera)")]
    [DefaultValue(false)]
    public Key FbnMenu { get; set; } = Key.F5;
    
    [DisplayName("Debug Mode")]
    [Description("Logs additional information to the console that is useful for debugging.")]
    [DefaultValue(false)]
    public bool DebugEnabled { get; set; } = false;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}
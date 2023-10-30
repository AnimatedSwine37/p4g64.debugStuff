using p4g64.debugStuff.Configuration;
using p4g64.debugStuff.NuGet.templates.defaultPlus;
using p4g64.debugStuff.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace p4g64.debugStuff;
/// <summary>
/// Your mod logic goes here.
/// </summary>
public unsafe class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    private IHook<DebugLogDelegate> _debugLogHook;

    public Mod(ModContext context)
    {
        //Debugger.Launch();
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        Utils.Initialise(_logger, _configuration, _modLoader);

        Utils.SigScan("E8 ?? ?? ?? ?? EB ?? BB 3D 00 00 00", "DebugPrintPtr", address =>
        {
            var funcAddress = Utils.GetGlobalAddress(address + 1);
            Utils.Log($"Found DebugPrint at 0x{funcAddress:X}");
            _debugLogHook = _hooks.CreateHook<DebugLogDelegate>(DebugLog, (long)funcAddress).Activate();
        });
    }

    private void DebugLog(string format, nuint arg1, nuint arg2, nuint arg3, nuint arg4, nuint arg5)
    {
        int n = 0;
        nuint getArg()
        {
            switch (n)
            {
                case 0: return arg1;
                case 1: return arg2;
                case 2: return arg3;
                case 3: return arg4;
                case 4: return arg5;
                default: return arg5;
            }
        }
        List<object> args = new();
        string csFormat = Regex.Replace(format, @"%((0([0-9])lX)?[dsf]?)", m =>
        {
            if (m.Groups[2].Success)
            {
                // Hex strings
                args.Add(getArg());
                return $"{{{n++}:X{m.Groups[3].Value}}}";
            }
            switch (m.Groups[1].Value)
            {
                case "d":
                    args.Add((int)(getArg() & 0xFFFFFFFF));
                    break;
                case "f":
                    args.Add(BitConverter.UInt64BitsToDouble(getArg()));
                    break;
                case "s":
                    args.Add(Utils.GetCString((byte*)getArg()));
                    break;
            }
            return $"{{{n++}}}";
        });

        _logger.Write(string.Format(csFormat, args.ToArray()), System.Drawing.Color.LightGreen); ;
    }

    private delegate void DebugLogDelegate(string format, nuint arg1, nuint arg2, nuint arg3, nuint arg4, nuint arg5);

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}
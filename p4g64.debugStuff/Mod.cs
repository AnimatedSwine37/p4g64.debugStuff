using p4g64.debugStuff.Configuration;
using p4g64.debugStuff.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using SharpDX.DirectInput;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;
using Reloaded.Memory;
using p4g64.debugStuff.DebugMenus;
using p4g64.debugStuff.Native;
using static p4g64.debugStuff.Native.Tasks;

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
    private Action _runSprView;
    private Action _runBustupView;
    private TestDrawPolyDelegate _runTestDrawPoly;
    private Action _runScriptViewer;
    private Action _runCpuGraph;
    private Action _runFontInfo;
    private Action _runRgbEdit;
    private Action _runDebugMenu;
    private Action _runEvtEditLoad;
    private RunFbnEditorDelegate _runTestMayonakaTv;
    private Action _runCommunityEdit;
    private nuint _fieldViewer;

    private char* _fieldViewerStr;

    private ListBox _listBox;
    private Files _fileHooks;

    private EnvironmentEditor _environmentEditor;
    private FbnEditor _fbnEditor;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        Utils.Initialise(_logger, _configuration, _modLoader);
        Text.Initialise(_hooks);
        Tasks.Initialise(_hooks);

        _listBox = new(_hooks);
        _fileHooks = new(_hooks, _modLoader.GetDirectoryForModId(_modConfig.ModId));

        _environmentEditor = new(_hooks);
        _fbnEditor = new(_hooks);

        var memory = Memory.Instance;
        _fieldViewerStr = (char*)memory.Allocate(24).Address;
        memory.WriteRaw((nuint)_fieldViewerStr, Encoding.ASCII.GetBytes("field view\0"));

        Utils.SigScan("E8 ?? ?? ?? ?? EB ?? BB 3D 00 00 00", "DebugPrintPtr", address =>
        {
            var funcAddress = Utils.GetGlobalAddress(address + 1);
            Utils.Log($"Found DebugPrint at 0x{funcAddress:X}");
            _debugLogHook = _hooks.CreateHook<DebugLogDelegate>(DebugLog, (long)funcAddress).Activate();
        });

        Utils.SigScan("40 53 48 83 EC 40 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? BA 70 19 01 00", "RunSprView", address =>
        {
            _runSprView = _hooks.CreateWrapper<Action>(address, out _);
        });

        Utils.SigScan("48 83 EC 48 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? BA 10 00 00 00 8D 4A ?? E8 ?? ?? ?? ?? 48 85 C0 0F 84 ?? ?? ?? ??", "RunBustupView", address =>
        {
            _runBustupView = _hooks.CreateWrapper<Action>(address, out _);
        });

        Utils.SigScan("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 48 8B F1 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? BA 20 00 00 00 8D 4A ?? E8 ?? ?? ?? ?? 48 8B F8", "RunTestDrawPoly", address =>
        {
            _runTestDrawPoly = _hooks.CreateWrapper<TestDrawPolyDelegate>(address, out _);
        });

        Utils.SigScan("48 83 EC 48 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 75 ?? BA 40 02 00 00", "RunScriptViewer", address =>
        {
            _runScriptViewer = _hooks.CreateWrapper<Action>(address, out _);
        });

        Utils.SigScan("48 83 EC 48 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 ?? 48 8B C8 48 83 C4 48 E9 ?? ?? ?? ?? BA 54 31 00 00", "RunCpuGraph", address =>
        {
            _runCpuGraph = _hooks.CreateWrapper<Action>(address, out _);
        });

        Utils.SigScan("48 83 EC 48 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 85 C0 74 ?? 48 8B C8 48 83 C4 48 E9 ?? ?? ?? ?? 48 89 44 24 ??", "RunFontInfo", address =>
        {
            _runFontInfo = _hooks.CreateWrapper<Action>(address, out _);
        });

        Utils.SigScan("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? E8 ?? ?? ?? ??", "RunEvtEditLoad", address =>
        {
            _runEvtEditLoad = _hooks.CreateWrapper<Action>(address, out _);
        });

        Utils.SigScan("48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 55 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC 00 01 00 00", "FieldViewer", address =>
        {
            _fieldViewer = (nuint)address;
        });


        // Not unique and I can't be bothered scanning for the struct it's from (yet)
        //_runRgbEdit = _hooks.CreateWrapper<Action>(0x140440fa0, out _);
        _runDebugMenu = _hooks.CreateWrapper<Action>(0x1403db350, out _);
        //_runTestMayonakaTv = _hooks.CreateWrapper<RunFbnEditorDelegate>(0x1404759f0, out _);
        //_runCommunityEdit = _hooks.CreateWrapper<Action>(0x1401d62a0, out _);

        Task.Run(() => InputHook());
    }

    private void InputHook()
    {
        // TODO absolutely do not do this, use OnModLoaderInitialised or something like that. It was causing problems
        Thread.Sleep(5000);
        Utils.Log("Setting up input hook");
        var directInput = new DirectInput();
        var keyboard = new Keyboard(directInput);

        // Acquire the joystick
        keyboard.Properties.BufferSize = 128;
        keyboard.Acquire();

        // Poll events from keyboard
        while (true)
        {
            keyboard.Poll();
            var datas = keyboard.GetBufferedData();
            foreach (var state in datas)
            {
                if (state.Key == Key.F4 && state.IsPressed)
                {
                    Utils.Log("Running thing!");

                    _environmentEditor.Run();

                    //_runDebugMenu();
                    // _fbnEditor.Run();
                    //_runCommunityEdit ();
                    //_runTestMayonakaTv(0);
                    //_runEvtEditLoad();
                    //RunTask(_fieldViewerStr, 0x101, 0, 0, _fieldViewer, 0, (void*)0);

                    //_runRgbEdit();
                    //_runFontInfo();
                    //_runCpuGraph();
                    //_runScriptViewer();
                    //_runTestDrawPoly(0);
                    //_runBustupView();
                    //_runSprView();
                }
            }
        }
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

    private delegate nuint TestDrawPolyDelegate(nuint param_1);
    private delegate TaskInfo* RunFbnEditorDelegate(nuint task);
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
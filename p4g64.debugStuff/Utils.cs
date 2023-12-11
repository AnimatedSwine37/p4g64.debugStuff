using p4g64.debugStuff.Configuration;
using Reloaded.Memory;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory.Structs;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace p4g64.debugStuff.NuGet.templates.defaultPlus;
internal class Utils
{
    private static ILogger _logger;
    private static Config _config;
    private static IStartupScanner _startupScanner;
    private static Memory _memory;

    internal static nint BaseAddress { get; private set; }

    internal static bool Initialise(ILogger logger, Config config, IModLoader modLoader)
    {
        _logger = logger;
        _config = config;
        _memory = Memory.Instance;
        using var thisProcess = Process.GetCurrentProcess();
        BaseAddress = thisProcess.MainModule!.BaseAddress;

        var startupScannerController = modLoader.GetController<IStartupScanner>();
        if (startupScannerController == null || !startupScannerController.TryGetTarget(out _startupScanner))
        {
            LogError($"Unable to get controller for Reloaded SigScan Library, stuff won't work :(");
            return false;
        }

        return true;

    }

    internal static void LogDebug(string message)
    {
        if (_config.DebugEnabled)
            _logger.WriteLine($"[Debug Stuff] {message}");
    }

    internal static void Log(string message)
    {
        _logger.WriteLine($"[Debug Stuff] {message}");
    }

    internal static void LogError(string message, Exception e)
    {
        _logger.WriteLine($"[Debug Stuff] {message}: {e.Message}", System.Drawing.Color.Red);
    }

    internal static void LogError(string message)
    {
        _logger.WriteLine($"[Debug Stuff] {message}", System.Drawing.Color.Red);
    }

    internal static void SigScan(string pattern, string name, Action<nint> action)
    {
        _startupScanner.AddMainModuleScan(pattern, result =>
        {
            if (!result.Found)
            {
                LogError($"Unable to find {name}, stuff won't work :(");
                return;
            }
            LogDebug($"Found {name} at 0x{result.Offset + BaseAddress:X}");

            action(result.Offset + BaseAddress);
        });
    }

    public unsafe static string GetCString(byte* ptr)
    {
        return Encoding.ASCII.GetString(ptr, GetCStringLength(ptr));
    }

    public unsafe static int GetCStringLength(byte* ptr)
    {
        int count = 0;
        while (*(ptr + count) != 0)
            count++;
        return count;
    }

    /// <summary>
    /// Writes a string to memory, returning the address of it
    /// </summary>
    /// <param name="str">The string to write</param>
    /// <param name="encoding">The encoding to use</param>
    /// <returns>The address of the string in memory</returns>
    internal static unsafe char* WriteStr(string str, Encoding encoding, out MemoryAllocation allocation)
    {
        // null terminate if it isn't already
        if (!str.EndsWith('\0'))
            str = str + '\0';

        var strBytes = encoding.GetBytes(str);
        allocation = _memory.Allocate((nuint)strBytes.Length);
        var strPtr = allocation.Address;
        _memory.WriteRaw(strPtr, strBytes);
        LogDebug($"Wrote \"{str}\" to 0x{strPtr:X} with encoding {encoding.EncodingName}");
        return (char*)strPtr;
    }


    // Pushes the value of an xmm register to the stack, saving it so it can be restored with PopXmm
    public static string PushXmm(int xmmNum)
    {
        return // Save an xmm register 
            $"sub rsp, 16\n" + // allocate space on stack
            $"movdqu dqword [rsp], xmm{xmmNum}\n";
    }

    // Pushes all xmm registers (0-15) to the stack, saving them to be restored with PopXmm
    public static string PushXmm()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 16; i++)
        {
            sb.Append(PushXmm(i));
        }
        return sb.ToString();
    }

    // Pops the value of an xmm register to the stack, restoring it after being saved with PushXmm
    public static string PopXmm(int xmmNum)
    {
        return                 //Pop back the value from stack to xmm
            $"movdqu xmm{xmmNum}, dqword [rsp]\n" +
            $"add rsp, 16\n"; // re-align the stack
    }

    // Pops all xmm registers (0-7) from the stack, restoring them after being saved with PushXmm
    public static string PopXmm()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 7; i >= 0; i--)
        {
            sb.Append(PopXmm(i));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Gets the address of a global from something that references it
    /// </summary>
    /// <param name="ptrAddress">The address to the pointer to the global (like in a mov instruction or something)</param>
    /// <returns>The address of the global</returns>
    internal static unsafe nuint GetGlobalAddress(nint ptrAddress)
    {
        return (nuint)((*(int*)ptrAddress) + ptrAddress + 4);
    }
}

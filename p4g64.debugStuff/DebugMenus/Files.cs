using p4g64.debugStuff.NuGet.templates.defaultPlus;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace p4g64.debugStuff.DebugMenus;
internal unsafe class Files
{
    private IHook<OpenFileDelegate> _openFileHook;
    private string _filesPath;
    private Memory _memory;

    internal Files(IReloadedHooks hooks, string filesPath)
    {
        _memory = Memory.Instance;
        _filesPath = filesPath;
        Utils.SigScan("48 8B C4 56 57 41 54", "OpenFile", address =>
        {
            //_openFileHook = hooks.CreateHook<OpenFileDelegate>(OpenFile, address).Activate();
        });
    }

    private nint OpenFile(char* path, nuint param_2)
    {
        var handle = _openFileHook.OriginalFunction(path, param_2);
        if (handle > 0) return handle; // The original function worked, leave it at that

        // The original didn't, let's try opening the file locally
        string pathStr = Utils.GetCString((byte*)path);
        var startIndex = pathStr.IndexOf(":/");
        if (startIndex == -1)
        {
            Utils.LogError($"Unable to find root of path {pathStr} to overwrite");
            return -1;
        }
        string newPath = Path.Combine(_filesPath, pathStr.Substring(startIndex + 2)).Replace('/', '\\');
        Utils.Log($"Redirecting open of \"{pathStr}\" to \"{newPath}\"");

        var newPathPtr = Utils.WriteStr(newPath, Encoding.ASCII, out var pathAlloc);
        var newHandle = _openFileHook.OriginalFunction(newPathPtr, param_2);
        _memory.Free(pathAlloc);
        Utils.LogDebug($"Created file with handle 0x{newHandle:X}");
        return newHandle;
        // Create a new read-write file (if it exists or doesn't make a new one)
        //var newHandle = Native.CreateFileW(newPath, 0xC0000000, 1, 0, 2, 128, 0);
        //return newHandle;
    }

    private delegate nint OpenFileDelegate(char* path, nuint param_2);
}

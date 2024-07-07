using Reloaded.Hooks.Definitions;
using Reloaded.Memory;
using Reloaded.Memory.Interfaces;
using static p4g64.debugStuff.Native.Tasks;

namespace p4g64.debugStuff.DebugMenus;
internal unsafe class EnvironmentEditor
{
    private RunTaskDelegate _run;
    private IHook<RunTaskDelegate> _finishedHook;
    private IHook<RunTaskDelegate> _shadowEditFinishedHook;
    private TaskInfo* _task;

    internal EnvironmentEditor(IReloadedHooks hooks)
    {
        var memory = Memory.Instance;

        Utils.SigScan("40 57 48 83 EC 40 48 8B F9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 83 38 00", "RunEnvironmentEditor", address =>
        {
            _run = hooks.CreateWrapper<RunTaskDelegate>(address, out _);
            
            var editEnvFinished = Utils.GetGlobalAddress(address + 0x4a);
            _finishedHook = hooks.CreateHook<RunTaskDelegate>(EditEnvFinished, (long)editEnvFinished).Activate();
        });

        // =============================================
        // Fixes for some sub menus unlocking the inputs
        // =============================================

        // Fog Edit Fixes
        Utils.SigScan("83 49 ?? 10 48 8B 49 ?? 48 85 C9 75 ?? 41 FF C0 41 83 F8 03 7C ?? 48 8B C3", "FogEditLock", address =>
        {
            memory.SafeWrite((nuint)address, new byte[] { 0x90, 0x90, 0x90, 0x90 });
        });

        Utils.SigScan("83 63 ?? EF 48 8B 5B ?? 48 85 DB 75 ?? FF C2 83 FA 03 7C ?? E9 ?? ?? ?? ?? BA 20 00 00 00", "FogEditUnlock", address =>
        {
            memory.SafeWrite((nuint)address, new byte[] { 0x90, 0x90, 0x90, 0x90 });
        });

        // Light Edit Fixes
        Utils.SigScan("83 49 ?? 10 48 8B 49 ?? 48 85 C9 75 ?? 41 FF C0 41 83 F8 03 7C ?? 40 0F BE C5", "LightEditLock", address =>
        {
            memory.SafeWrite((nuint)address, new byte[] { 0x90, 0x90, 0x90, 0x90 });
        });

        Utils.SigScan("83 61 ?? EF 48 8B 49 ?? 48 85 C9 75 ?? 41 FF C0 41 83 F8 03 7C ?? FF 07", "LightEditUnlock", address =>
        {
            memory.SafeWrite((nuint)address, new byte[] { 0x90, 0x90, 0x90, 0x90 });
        });

        // Shadow Edit Fixes
        Utils.SigScan("48 8D 05 ?? ?? ?? ?? 45 33 C0 48 89 44 24 ?? 48 8D 05 ?? ?? ?? ?? 48 89 44 24 ?? 41 8D 51 ?? E8 ?? ?? ?? ?? 45 33 C9 BA D0 02 00 00", "ShadowEditFinishedPtr", address =>
        {
            var shadowEditFinished = Utils.GetGlobalAddress(address + 3);
            Utils.LogDebug($"Found ShadowEditFinished at 0x{shadowEditFinished:X}");
            _shadowEditFinishedHook = hooks.CreateHook<RunTaskDelegate>(EditShadowFinished, (long)shadowEditFinished).Activate();
        });
    }

    // Relock inputs after shadow edit is done
    // (it's special and seemingly actually uses the locking and unlocking in a somewhat useful way)
    private TaskInfo* EditShadowFinished(TaskInfo* task)
    {
        // TODO fix this locking navigation of the menu (you can at least go in and out but not up and down)
        var res = _shadowEditFinishedHook.OriginalFunction(task);
        LockTaskInputs(_task);
        return res;
    }

    internal void Run()
    {
        _task = _run((TaskInfo*)0);
        LockTaskInputs(_task);
    }

    private TaskInfo* EditEnvFinished(TaskInfo* task)
    {
        UnlockTaskInputs();
        return _finishedHook.OriginalFunction(task);
    }

}

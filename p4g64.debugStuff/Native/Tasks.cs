using Reloaded.Hooks.ReloadedII.Interfaces;
using System.Runtime.InteropServices;

namespace p4g64.debugStuff.Native;

internal unsafe class Tasks
{
    // Bad names but they're some tasks used, idk what for specifically
    private static TaskInfo**[] _mainTasks = new TaskInfo**[3];

    internal static void Initialise(IReloadedHooks hooks)
    {
        Utils.SigScan(
            "4C 8B 0D ?? ?? ?? ?? 33 C9 4C 8B 15 ?? ?? ?? ?? 44 8B C1 4C 8B 1D ?? ?? ?? ?? 41 8B D0 45 85 C0 74 ?? 83 EA 01 74 ?? 83 FA 01 75 ?? 49 8B CB EB ?? 49 8B CA EB ?? 49 8B C9 48 85 C9 74 ?? 0F 1F 80 00 00 00 00",
            "MainTasks", address =>
            {
                _mainTasks[0] = (TaskInfo**)Utils.GetGlobalAddress(address + 3);
                Utils.LogDebug($"Found MainTask1 at 0x{(nuint)_mainTasks[0]:X}");
                _mainTasks[1] = (TaskInfo**)Utils.GetGlobalAddress(address + 12);
                Utils.LogDebug($"Found MainTask2 at 0x{(nuint)_mainTasks[1]:X}");
                _mainTasks[2] = (TaskInfo**)Utils.GetGlobalAddress(address + 22);
                Utils.LogDebug($"Found MainTask3 at 0x{(nuint)_mainTasks[2]:X}");
            });
    }

    // Block all tasks other than the supplied one from taking inputs 
    // Used so you can navigate menus without Yu moving and stuff
    internal static void LockTaskInputs(TaskInfo* task)
    {
        for (int i = 0; i < 3; i++)
        {
            TaskInfo* toLock = *_mainTasks[i];
            while (toLock != (TaskInfo*)0x0)
            {
                if (toLock != task)
                {
                    toLock->LockInputs = toLock->LockInputs | 0x10;
                }

                toLock = toLock->NextTask;
            }
        }
    }

    // Lets all other tasks resume taking inputs (use some time after LockTaskInputs)
    internal static void UnlockTaskInputs()
    {
        for (int i = 0; i < 3; i++)
        {
            TaskInfo* toUnlock = *_mainTasks[i];
            while (toUnlock != (TaskInfo*)0x0)
            {
                toUnlock->LockInputs = toUnlock->LockInputs & ~0x10;
                toUnlock = toUnlock->NextTask;
            }
        }
    }


    [StructLayout(LayoutKind.Explicit)]
    internal struct TaskInfo
    {
        [FieldOffset(0x1c)] internal int LockInputs;

        [FieldOffset(0x48)] internal void* Args;

        [FieldOffset(0x50)] internal TaskInfo* NextTask;
    }

    internal delegate TaskInfo* RunTaskDelegate(TaskInfo* task);
}
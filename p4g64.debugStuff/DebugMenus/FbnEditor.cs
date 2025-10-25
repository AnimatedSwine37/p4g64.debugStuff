using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using static p4g64.debugStuff.Native.Tasks;

namespace p4g64.debugStuff.DebugMenus;
internal unsafe class FbnEditor
{
    private RunFbnEditorDelegate _run;
    private IHook<RunTaskDelegate> _finishedHook;
    private IHook<FbnEditorSaveDelegate> _saveHook;
    private IAsmHook _saveFixHook;
    private TaskInfo* _task;

    internal FbnEditor(IReloadedHooks hooks)
    {
        Utils.SigScan("40 57 48 83 EC 40 48 8B F9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 83 78 ?? 00", "RunFbnEditor", address =>
        {
            _run = hooks.CreateWrapper<RunFbnEditorDelegate>(address, out _);
        });

        Utils.SigScan("48 89 5C 24 ?? 56 48 83 EC 20 48 8B F1 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ??", "FbnEditorFinished", address =>
        {
            _finishedHook = hooks.CreateHook<RunTaskDelegate>(FbnEditorFinished, address).Activate();
        });
        
        // TODO temporary just to fix the crash, need to figure out the actual problem...
        Utils.SigScan("66 83 7B ?? 01 41 B8 14 0E 00 00", "CollisionCrashFix", address =>
        {
            string[] function = new[]
            {
                "use64",
                "mov word [rbx+0xa], 1"
            };
            hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteFirst).Activate();
        });
        
        Utils.SigScan("41 56 48 81 EC 50 01 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 44 0F B7 F1 48 8D 0D ?? ?? ?? ??", "FbnEditorSave",
            address =>
            {
                _saveHook = hooks.CreateHook<FbnEditorSaveDelegate>(FbnEditorSave, address).Activate();
            });
        
        // TODO fixes the file just not saving, need to investigate what this check is actually for and maybe change stuff
        Utils.SigScan("83 3D ?? ?? ?? ?? 01 4C 8B 05 ?? ?? ?? ??", "FbnEditorSaveFix", address =>
        {
            string[] function = new[]
            {
                "use64",
                "cmp rax, rsp", // Sets the zf to 0 so the jz fails and we actually write the file
            };
            _saveFixHook = hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteAfter).Activate();
        });
        
    }

    internal void Run()
    {
        _task = _run(0);
        LockTaskInputs(_task);
    }

    private TaskInfo* FbnEditorFinished(TaskInfo* task)
    {
        UnlockTaskInputs();
        return _finishedHook.OriginalFunction(task);
    }

    private void FbnEditorSave(short fbnId)
    {
        if (!Directory.Exists("data/field/myfolder"))
        {
            Directory.CreateDirectory("data/field/myfolder");
        }
        
        Utils.Log($"Saving fbn to {new DirectoryInfo("data/field/myfolder").FullName}");

        _saveHook.OriginalFunction(fbnId);
    }

    private delegate TaskInfo* RunFbnEditorDelegate(nuint task);
    private delegate void FbnEditorSaveDelegate(short fbnId);
}

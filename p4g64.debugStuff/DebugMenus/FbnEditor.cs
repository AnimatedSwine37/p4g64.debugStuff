using Reloaded.Hooks.Definitions;
using static p4g64.debugStuff.Native.Tasks;

namespace p4g64.debugStuff.DebugMenus;
internal unsafe class FbnEditor
{
    private RunFbnEditorDelegate _run;
    private IHook<RunTaskDelegate> _finishedHook;
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

    private delegate TaskInfo* RunFbnEditorDelegate(nuint task);
}

using System.Runtime.InteropServices;
using System.Text;
using p4g64.debugStuff.Native;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
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

    private IAsmHook _loadEnvHook;
    private IAsmHook _saveEnvLocationHook;
    private IHook<SaveEnvFileDelegate> _saveEnvHook;
    private IHook<LoadEnvironmentDelegate> _loadEnvironmentHook;
    private IHook<SaveEnvironmentDelegate> _saveEnvironmentHook;
    private IHook<EnvLoadDelegate> _envLoadHook;
    private IReverseWrapper<SetCurrentEnvDelegate> _setCurrentEnvReverseWrapper;
    private IAsmHook _envFileNameHook;

    private int _envMajor = 0;
    private int _envMinor = 0;

    private int** _fieldMajor;
    private FileInfo** _fieldArc;
    private string _fieldArcName = null;

    internal EnvironmentEditor(IReloadedHooks hooks)
    {
        var memory = Memory.Instance;

        Utils.SigScan("40 57 48 83 EC 40 48 8B F9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 83 38 00",
            "RunEnvironmentEditor", address =>
            {
                _run = hooks.CreateWrapper<RunTaskDelegate>(address, out _);

                var editEnvFinished = Utils.GetGlobalAddress(address + 0x4a);
                _finishedHook = hooks.CreateHook<RunTaskDelegate>(EditEnvFinished, (long)editEnvFinished).Activate();
            });

        // ======================================
        // Fixes for loading and saving env files
        // ======================================
        Utils.SigScan(
            "48 8B 05 ?? ?? ?? ?? 48 8B 88 ?? ?? ?? ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 89 98 ?? ?? ?? ??",
            "LoadEnvironmentForceReload",
            address =>
            {
                string[] function =
                {
                    "use64",
                    "mov dword [rax+4], 0" // Set the env to not loaded to force a reload from disc
                };

                _loadEnvHook = hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteFirst).Activate();
            });

        Utils.SigScan("E8 ?? ?? ?? ?? 48 8B C8 48 8B 78 ??", "LoadEnvironmenFile", address =>
        {
            string[] function =
            {
                "use64",
                "push rcx \n push rdx \n push r8",
                "sub rsp, 40",
                hooks.Utilities.GetAbsoluteCallMnemonics(SetCurrentEnv, out _setCurrentEnvReverseWrapper),
                "add rsp, 40",
                "pop r8 \n pop rdx \n pop rcx"
            };

            _envFileNameHook = hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteFirst).Activate();
        });

        Utils.SigScan(
            "48 89 5C 24 ?? 57 48 81 EC B0 00 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B D9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 7B ?? 48 63 07",
            "LoadEnvironment",
            address =>
            {
                _loadEnvironmentHook = hooks.CreateHook<LoadEnvironmentDelegate>(LoadEnvironment, address).Activate();
            });

        Utils.SigScan(
            "48 89 5C 24 ?? 57 48 81 EC B0 00 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B D9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 7B ?? 8B 0F",
            "SaveEnvironment",
            address =>
            {
                _saveEnvironmentHook = hooks.CreateHook<SaveEnvironmentDelegate>(SaveEnvironment, address).Activate();
            });

        Utils.SigScan("48 39 35 ?? ?? ?? ?? 75 ?? 83 7F ?? 0C", "FieldPackPtr",
            address => { _fieldArc = (FileInfo**)Utils.GetGlobalAddress(address + 3); });

        Utils.SigScan("40 55 53 56 57 41 56 48 8D AC 24 ?? ?? ?? ?? B8 E0 9E 00 00", "Env::Load",
            address => { _envLoadHook = hooks.CreateHook<EnvLoadDelegate>(EnvLoad, address).Activate(); });

        Utils.SigScan(
            "48 8B 05 ?? ?? ?? ?? 48 8D 4C 24 ?? 44 8B 00 E8 ?? ?? ?? ?? 48 8B 47 ?? 4C 8D 44 24 ?? 48 8B 48 ?? 48 8B 91 ?? ?? ?? ?? 48 85 D2 74 ?? 39 1A 74 ?? 66 0F 1F 44 ?? 00",
            "FieldMajorPtr", address => { _fieldMajor = (int**)Utils.GetGlobalAddress(address + 3); });

        Utils.SigScan(
            "E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 FF C9 0F 1F 40 00 80 79 ?? 00 48 8D 49 ?? 75 ?? 4C 8D 94 24 ?? ?? ?? ??",
            "SaveEnvFileLocation", address =>
            {
                // Change the file path to be app0:/field/fxxx_xxx.ENV
                string[] function =
                {
                    "use64",
                    "lea rcx, [rsp+0x36]", // Write the path after app0:/ (don't put in data folder)
                    hooks.Utilities.GetAbsoluteJumpMnemonics(address + 0x1b,
                        true) // skip over moving to after data, we just did this with the above instruction
                };

                _saveEnvLocationHook = hooks.CreateAsmHook(function, address, AsmHookBehaviour.DoNotExecuteOriginal)
                    .Activate();
            });

        Utils.SigScan("40 53 48 81 EC 40 01 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 8B D9",
            "SaveEnvFile",
            address => { _saveEnvHook = hooks.CreateHook<SaveEnvFileDelegate>(SaveEnvFile, address).Activate(); });

        // =============================================
        // Fixes for some sub menus unlocking the inputs
        // =============================================

        // Fog Edit Fixes
        Utils.SigScan("83 49 ?? 10 48 8B 49 ?? 48 85 C9 75 ?? 41 FF C0 41 83 F8 03 7C ?? 48 8B C3", "FogEditLock",
            address => { memory.SafeWrite((nuint)address, new byte[] { 0x90, 0x90, 0x90, 0x90 }); });

        Utils.SigScan("83 63 ?? EF 48 8B 5B ?? 48 85 DB 75 ?? FF C2 83 FA 03 7C ?? E9 ?? ?? ?? ?? BA 20 00 00 00",
            "FogEditUnlock", address => { memory.SafeWrite((nuint)address, new byte[] { 0x90, 0x90, 0x90, 0x90 }); });

        // Light Edit Fixes
        Utils.SigScan("83 49 ?? 10 48 8B 49 ?? 48 85 C9 75 ?? 41 FF C0 41 83 F8 03 7C ?? 40 0F BE C5", "LightEditLock",
            address => { memory.SafeWrite((nuint)address, new byte[] { 0x90, 0x90, 0x90, 0x90 }); });

        Utils.SigScan("83 61 ?? EF 48 8B 49 ?? 48 85 C9 75 ?? 41 FF C0 41 83 F8 03 7C ?? FF 07", "LightEditUnlock",
            address => { memory.SafeWrite((nuint)address, new byte[] { 0x90, 0x90, 0x90, 0x90 }); });

        // Shadow Edit Fixes
        Utils.SigScan(
            "48 8D 05 ?? ?? ?? ?? 45 33 C0 48 89 44 24 ?? 48 8D 05 ?? ?? ?? ?? 48 89 44 24 ?? 41 8D 51 ?? E8 ?? ?? ?? ?? 45 33 C9 BA D0 02 00 00",
            "ShadowEditFinishedPtr", address =>
            {
                var shadowEditFinished = Utils.GetGlobalAddress(address + 3);
                Utils.LogDebug($"Found ShadowEditFinished at 0x{shadowEditFinished:X}");
                _shadowEditFinishedHook =
                    hooks.CreateHook<RunTaskDelegate>(EditShadowFinished, (long)shadowEditFinished).Activate();
            });
    }

    private void SetCurrentEnv(string fileName)
    {
        _envMajor = int.Parse(fileName.Substring(1, 3));
        _envMinor = int.Parse(fileName.Substring(5, 3));
        Utils.Log($"Entered field with env {fileName}");
    }

    private bool EnvLoad(nuint envLoad, nuint fieldMinor)
    {
        if(*_fieldArc != null)
        {
            string fullArc = Encoding.ASCII.GetString((*_fieldArc)->Name, 64);
            _fieldArcName = fullArc.Split('/').Last().Trim();
            Utils.Log("Field arc is " + _fieldArcName);
        }
        
        return _envLoadHook.OriginalFunction(envLoad, fieldMinor);
    }

    private UIntPtr LoadEnvironment(EnvEditorTask* envEditorTask, UIntPtr param_2, UIntPtr param_3, UIntPtr param_4)
    {
        if (envEditorTask->Args->State == 0)
        {
            envEditorTask->Args->FieldMinor = _envMinor;
            Utils.Log($"Env is from {_fieldArcName}");
        }

        // Change the field major to the env major when running this func (it's easier than patching each place that the major global is used)
        var fieldMajor = **_fieldMajor;
        **_fieldMajor = _envMajor;
        var res = _loadEnvironmentHook.OriginalFunction(envEditorTask, param_2, param_3, param_4);
        **_fieldMajor = fieldMajor;
        return res;
    }

    private UIntPtr SaveEnvironment(EnvEditorTask* envEditorTask, UIntPtr param_2, UIntPtr param_3, UIntPtr param_4)
    {
        bool initial = false;
        if (envEditorTask->Args->State == 0)
        {
            initial = true;
            Utils.Log($"Env is from {_fieldArcName}");
            envEditorTask->Args->FieldMinor = _envMinor;
        }

        // Change the field major to the env major when running this func (it's easier than patching each place that the major global is used)
        var fieldMajor = **_fieldMajor;
        **_fieldMajor = _envMajor;
        var res = _saveEnvironmentHook.OriginalFunction(envEditorTask, param_2, param_3, param_4);
        **_fieldMajor = fieldMajor;

        // Add the arc name to the list box; this code does not work at all, it's more complex :(
        // if (initial)
        // {
        //     var listBoxArgs = (ListBox.KskListBoxArgs*)envEditorTask->Args->ListBoxTask->Args;
        //     listBoxArgs->ListBox->List.NumOptions = 2;
        //     listBoxArgs->ListBox->List.NumDisplayedOptions = 2;
        //
        //     var arcOption = (ListBox.KskListBoxOption*)Marshal.AllocHGlobal(sizeof(ListBox.KskListBoxOption));
        //     var nameBytes = Encoding.ASCII.GetBytes(_fieldArcName);
        //     for (int i = 0; i < nameBytes.Length; i++)
        //     {
        //         arcOption->Text[i] = nameBytes[i];
        //     }
        //     listBoxArgs->ListBox->Options->NextOption = arcOption;
        // }
        
        return res;
    }

    private void SaveEnvFile(uint param_1)
    {
        if (!Directory.Exists("field/env"))
            Directory.CreateDirectory("field/env");

        _saveEnvHook.OriginalFunction(param_1);
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

    [StructLayout(LayoutKind.Explicit)]
    private struct EnvEditorTask
    {
        [FieldOffset(0x48)] internal EnvEditorTaskArgs* Args;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct EnvEditorTaskArgs
    {
        [FieldOffset(0)] internal int State;

        [FieldOffset(4)] internal int FieldMinor;

        [FieldOffset(8)] internal TaskInfo* ListBoxTask;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct FileInfo
    {
        [FieldOffset(16)] internal fixed byte Name[64];
    }

    private delegate void SaveEnvFileDelegate(uint param_1);

    private delegate void SetCurrentEnvDelegate(string fileName);

    private delegate bool EnvLoadDelegate(nuint envLoad, nuint fieldMinor);

    private delegate nuint LoadEnvironmentDelegate(EnvEditorTask* envEditorTask, nuint param_2, nuint param_3,
        nuint param_4);

    private delegate nuint SaveEnvironmentDelegate(EnvEditorTask* envEditorTask, nuint param_2, nuint param_3,
        nuint param_4);
}
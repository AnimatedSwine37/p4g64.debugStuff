using p4g64.debugStuff.NuGet.templates.defaultPlus;
using Reloaded.Hooks.ReloadedII.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace p4g64.debugStuff;
internal unsafe static class Native
{

    internal static RunTaskDelegate RunTask;
    internal static StringFormatDelegate StringFormat;
    private static RenderTextHigherDelegate _renderTextHigher;

    internal static void Initialise(IReloadedHooks hooks)
    {
        Utils.SigScan("48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 56 48 83 EC 20 48 89 CF", "RunTask", address =>
        {
            RunTask = hooks.CreateWrapper<RunTaskDelegate>(address, out _);
        });

        Utils.SigScan("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 70 0F 29 74 24 ?? 48 8D 0D ?? ?? ?? ?? 0F 29 7C 24 ?? 41 8B E9 44 0F 29 44 24 ?? 0F 28 F2 44 0F 28 C0 0F 28 F9 E8 ?? ?? ?? ?? 0F B7 05 ?? ?? ?? ?? 33 DB 0F B6 B4 24 ?? ?? ?? ?? 66 83 E0 FD 48 8B 8C 24 ?? ?? ?? ?? 66 83 C8 01 48 89 5C 24 ?? 41 B9 FF FF FF FF 45 33 C0 66 89 05 ?? ?? ?? ?? 40 0F B6 D6 88 5C 24 ?? E8 ?? ?? ?? ?? 48 8B F8 0F B6 84 24 ?? ?? ?? ?? 48 8B 57 ?? 88 47 ?? 48 85 D2 74 ?? 44 0F BE C0 0F 1F 44 ?? 00 8B 4A ?? 48 8B 52 ?? 41 03 C8 03 D9 48 85 D2 75 ?? 0F B7 05 ?? ?? ?? ?? 0F 28 DE 66 83 E0 FE 89 5F ?? 66 83 C8 02 0F 28 D7 66 89 05 ?? ?? ?? ?? 41 0F 28 C8 8B 84 24 ?? ?? ?? ?? 48 8B CF 89 44 24 ?? 40 88 74 24 ?? 89 6C 24 ?? E8 ?? ?? ?? ?? B2 01", "RenderTextHigher", address =>
        {
            _renderTextHigher = hooks.CreateWrapper<RenderTextHigherDelegate>(address, out _);
        });

        Utils.SigScan("48 89 54 24 ?? 4C 89 44 24 ?? 4C 89 4C 24 ?? 53 56 57 48 83 EC 30 48 8B F9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 8D 74 24 ?? E8 ?? ?? ?? ?? 4C 8B CB", "StringFormat", address =>
        {
            StringFormat = hooks.CreateWrapper<StringFormatDelegate>(address, out _);
        });
    }

    internal static void RenderText(char* text, float xPos, float yPos, Colour colour)
    {
        _renderTextHigher(xPos, yPos, 255, colour, 0, 0xd, text, TextPositioning.Right, -2);
    }

    internal struct TaskInfo
    {

    }

    internal struct TextStruct
    {

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Colour
    {
        internal byte R;
        internal byte G;
        internal byte B;
        internal byte A;
    }

    internal enum TextPositioning : int
    {
        Right = 0,
        Left = 2,
        Center = 8,
    }

    internal delegate int RenderTextHigherDelegate(float xPos, float yPos, int param_3, Colour colour, nuint param_5, byte param_6, char* text, TextPositioning positioning, sbyte spacing);

    internal delegate void TaskFunc(void* args);
    internal delegate TaskInfo* RunTaskDelegate(char* name, float param_2, int param_3, ushort param_4, nuint runFunc, nuint finishedFunc, void* taskArgs);

    internal delegate int StringFormatDelegate(char* dest, char* formatStr, nuint arg1, nuint arg2);
}

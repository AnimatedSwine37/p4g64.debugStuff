using Reloaded.Hooks.ReloadedII.Interfaces;
using System.Runtime.InteropServices;
using static p4g64.debugStuff.Native.Tasks;
using static p4g64.debugStuff.Utils;

namespace p4g64.debugStuff.Native;
internal unsafe static partial class Text
{

    internal static RunTaskDelegate RunTask;
    internal static StringFormatDelegate StringFormat;
    internal static DrawTextDelegate Draw;

    internal static void Initialise(IReloadedHooks hooks)
    {
        Utils.SigScan("48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 56 48 83 EC 20 48 89 CF", "RunTask", address =>
        {
            RunTask = hooks.CreateWrapper<RunTaskDelegate>(address, out _);
        });

        SigScan("E8 ?? ?? ?? ?? 45 33 FF 4C 8D 2D ?? ?? ?? ?? EB ??", "Text::Draw Ptr", address =>
        {
            var funcAddr = GetGlobalAddress(address + 1);
            Draw = hooks.CreateWrapper<DrawTextDelegate>((long)funcAddr, out _);
        });

        Utils.SigScan("48 89 54 24 ?? 4C 89 44 24 ?? 4C 89 4C 24 ?? 53 56 57 48 83 EC 30 48 8B F9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 8D 74 24 ?? E8 ?? ?? ?? ?? 4C 8B CB", "StringFormat", address =>
        {
            StringFormat = hooks.CreateWrapper<StringFormatDelegate>(address, out _);
        });

        Utils.SigScan("48 89 54 24 ?? 4C 89 44 24 ?? 4C 89 4C 24 ?? 53 56 57 48 83 EC 30 48 8B F9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 8D 74 24 ?? E8 ?? ?? ?? ?? 4C 8B CB", "StringFormat", address =>
        {
            StringFormat = hooks.CreateWrapper<StringFormatDelegate>(address, out _);
        });

    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct Colour
    {
        internal byte R;
        internal byte G;
        internal byte B;
        internal byte A;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct RevColour
    {
        public byte A;
        public byte B;
        public byte G;
        public byte R;

        public RevColour(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }

    internal enum TextPositioning : int
    {
        Right = 0,
        Left = 2,
        Center = 8,
    }

    internal delegate void DrawTextDelegate(float xPos, float yPos, nuint param_3, RevColour colour, byte param_5, byte textSize, char* text, TextPositioning position);

    internal delegate TaskInfo* RunTaskDelegate(char* name, float param_2, int param_3, ushort param_4, nuint runFunc, nuint finishedFunc, void* taskArgs);

    internal delegate int StringFormatDelegate(char* dest, char* formatStr, nuint arg1, nuint arg2);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr SecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );
}

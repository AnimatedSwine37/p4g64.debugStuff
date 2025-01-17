﻿using Reloaded.Hooks.Definitions;
using System.Runtime.InteropServices;
using System.Text;
using p4g64.debugStuff.Native;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;
using static p4g64.debugStuff.Native.Text;
using static p4g64.debugStuff.Utils;

namespace p4g64.debugStuff.DebugMenus;
internal unsafe class ListBox
{

    private IHook<KskListBoxRenderTextDelegate> _renderTextHook;
    private IntPtr _hdc = IntPtr.Zero;

    internal ListBox(IReloadedHooks hooks)
    {
        SigScan("48 8B C4 48 89 58 ?? 48 89 70 ?? 48 89 78 ?? 55 48 8D A8 ?? ?? ?? ?? 48 81 EC 70 02 00 00", "KskListBoxRenderText", address =>
        {
            _renderTextHook = hooks.CreateHook<KskListBoxRenderTextDelegate>(KskListBoxRenderText, address).Activate();
        });
    }

    private Colour _textColour = new Colour { R = 255, G = 255, B = 255, A = 255 };

    private void KskListBoxRenderText(KskListBoxArgs* args, nuint param_2, nuint param_3, nuint param_4)
    {
        _renderTextHook.OriginalFunction(args, param_2, param_3, param_4);

        var listBox = args->ListBox;
        var option = listBox->Options;
        float xPos = listBox->Size.X;
        float yPos = listBox->Size.Y;

        // Get the first displayed option (go through all the ones that are off screen)
        for(int i = 0; i < listBox->OptionOffset; i++)
        {
            if (option == (KskListBoxOption*)0) break;
            option = option->NextOption;
        }

        char* text = stackalloc char[256];

        // Render all the options that should be shown
        for (int i = 0; i < listBox->List.NumDisplayedOptions; i++)
        {
            if (option == (KskListBoxOption*)0) break;
            
            var formatted = FormatListBoxValue(option, text);
            Text.Draw(xPos/2, yPos/2, 0, new RevColour(255, 255, 255, 255), 0, 5, formatted, TextPositioning.Right);
            yPos += listBox->OptionHeight;
            option = option->NextOption;
        }
    }

    char* _intFormat = Utils.WriteStr("%s %8d", Encoding.ASCII, out _);
    char* _intHexFormat = Utils.WriteStr("%s 0x%08X", Encoding.ASCII, out _);
    char* _floatFormat = Utils.WriteStr("%s %4.2f", Encoding.ASCII, out _);
    char* _strFormat = Utils.WriteStr("%s %s", Encoding.ASCII, out _);
    char* _trueFormat = Utils.WriteStr("%s TRUE", Encoding.ASCII, out _);
    char* _falseFormat = Utils.WriteStr("%s FALSE", Encoding.ASCII, out _);

    private char* FormatListBoxValue(KskListBoxOption* option, char* outStr)
    {
        var name = option->Text;
        char* format;
        nuint value = 0;
        switch(option->ValueType)
        {
            case KskListBoxValue.String:
                format = _strFormat;
                value = (nuint)(option->StrValue);
                break;
            case KskListBoxValue.Int:
                format = option->UseHex ? _intHexFormat : _intFormat;
                value = (nuint)(option->IntValue);
                break;
            case KskListBoxValue.Float:
                format = _floatFormat;
                value = (nuint)BitConverter.DoubleToUInt64Bits(option->FloatValue); // Doing it like this so it doesn't try to cast to an int
                break;
            case KskListBoxValue.Bool:
                format = option->IntValue == 0 ? _falseFormat : _trueFormat;
                break;
            default:
                return (char*)name;
        }
        
        int length = StringFormat(outStr, format, (nuint)name, value);
        outStr[length] = '\0';
        return outStr;
    }


    [StructLayout(LayoutKind.Explicit)]
    internal struct KskListBoxArgs
    {
        [FieldOffset(0x48)]
        internal KskListBox* ListBox;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct KskListBox
    {
        [FieldOffset(0x18)]
        internal KskWindowPos Size;

        [FieldOffset(0x2c)]
        internal int OptionHeight;

        [FieldOffset(0x90)]
        internal KskWindowArgs* Task;

        [FieldOffset(0xa8)]
        internal KskListBoxOption* Options;

        [FieldOffset(0x98)]
        internal int SelectedOption;

        [FieldOffset(0x9c)]
        internal int OptionOffset;

        [FieldOffset(0xe0)]
        internal KskList List;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct KskWindowPos
    {
        [FieldOffset(0)]
        internal float X;

        [FieldOffset(4)]
        internal float Y;

        [FieldOffset(8)]
        internal float Width;

        [FieldOffset(12)]
        internal float Height;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct KskWindowArgs
    {

    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct KskListBoxOption
    {
        [FieldOffset(4)]
        internal KskListBoxValue ValueType;

        [FieldOffset(8)]
        internal fixed byte Text[256];

        [FieldOffset(0x108)]
        internal fixed byte StrValue[256];

        [FieldOffset(0x208)]
        internal int IntValue;

        [FieldOffset(0x20c)]
        internal float FloatValue;

        [FieldOffset(0x218)]
        internal bool UseHex;

        [FieldOffset(0x238)]
        internal KskListBoxOption* NextOption;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct KskList
    {
        [FieldOffset(0x14)]
        internal int NumDisplayedOptions;

        [FieldOffset(0x20)]
        internal int NumOptions;

        [FieldOffset(0x24)]
        internal int SelectedOption;

        [FieldOffset(0x28)]
        internal int OptionOffset;
    }

    internal enum KskListBoxValue : int
    {
        None = 0,
        String = 1,
        Bool = 2,
        Int = 3,
        Float = 4
    }

    private delegate void KskListBoxRenderTextDelegate(KskListBoxArgs* listBox, nuint param_2, nuint param_3, nuint param_4);
}

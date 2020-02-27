﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using StringReloads.AutoInstall.Base;
using StringReloads.Engine;
using StringReloads.Hook;

using static StringReloads.Hook.Base.Extensions;

namespace StringReloads.AutoInstall
{
    unsafe class AdvHD : IAutoInstall
    {
        Config Settings => EntryPoint.SRL.Settings;

        SysAllocString Hook;
        public string Name => "AdvHD";
        public bool IsCompatible()
        {
            if (Process.GetCurrentProcess().ProcessName.ToLower() != "advhd")
                return false;

            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Rio.arc")))
                return false;

            if (Settings.HookEnabled("SysAllocString")) {
                Log.Error("The AdvHD Auto-Install require to disable the SysAllocString Hook.");
                return false;
            }

            Hook = new SysAllocString();
            Compile();

            return true;
        }

        public void Install() => Hook.Install();

        public void Uninstall() => Hook.Uninstall();
        
        //+ 0x15 = SRL; + 0x29 = RealFunc
        byte[] _HookData = null;
        private void Compile()
        {
            if (_HookData != null)
                return;

            string[] SkipList = new string[] {
                    "st", "ev", "bg", "@", "text", "char", "timer", "effect", "movie"
                };

            List<byte> Buffer = new List<byte>();
            Buffer.AddRange(HookDataBase);

            foreach (string Skip in SkipList)
                Buffer.AddRange(Encoding.Unicode.GetBytes(Skip + "\x0"));

            Buffer.Add(0x00);
            Buffer.Add(0x00);

            _HookData = Buffer.ToArray();

            Hook.HookFunction = _HookData.ToPointer();
            Hook.Compile();

            var pFunc = (uint)EntryPoint.GetDirectProcess();
            BitConverter.GetBytes(pFunc).CopyTo(_HookData, 0x15);

            pFunc = (uint)Hook.BypassFunction;
            BitConverter.GetBytes(pFunc).CopyTo(_HookData, 0x29);
            
            _HookData.DeprotectMemory();
        }        

        static byte[] HookDataBase = new byte[] {
            0x58, 0x87, 0x04, 0x24, 0x60, 0x50, 0x50, 0xE8, 0x34, 0x00, 0x00, 0x00, 0x85, 0xC0,
            0x74, 0x15, 0x90, 0x90, 0x90, 0x90, 0xB8, 0xAA, 0xAA, 0xAA, 0xAA, 0xFF, 0xD0, 0x89,
            0x44, 0x24, 0x1C, 0x61, 0xEB, 0x05, 0x90, 0x90, 0x90, 0x58, 0x61, 0x50, 0xB8, 0xBB,
            0xBB, 0xBB, 0xBB, 0xFF, 0xD0, 0xC3, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90,
            0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x58, 0x87, 0x04, 0x24, 0x60, 0x66,
            0x83, 0x38, 0x00, 0x75, 0x0C, 0x90, 0x90, 0x90, 0x90, 0x83, 0xEC, 0x04, 0xEB, 0x30,
            0x90, 0x90, 0x90, 0xBB, 0x00, 0x00, 0x00, 0x00, 0x50, 0x8B, 0x04, 0x24, 0x50, 0x53,
            0xE8, 0xC9, 0x00, 0x00, 0x00, 0x8B, 0xD8, 0x85, 0xC0, 0x74, 0x21, 0x90, 0x90, 0x90,
            0x90, 0x50, 0xE8, 0x39, 0x00, 0x00, 0x00, 0x85, 0xC0, 0x75, 0x09, 0x90, 0x90, 0x90,
            0x90, 0xEB, 0xDC, 0x90, 0x90, 0x90, 0xB8, 0x00, 0x00, 0x00, 0x00, 0xEB, 0x10, 0x90,
            0x90, 0x90, 0x83, 0xC4, 0x04, 0xB8, 0x01, 0x00, 0x00, 0x00, 0xEB, 0x03, 0x90, 0x90,
            0x90, 0x83, 0xC4, 0x04, 0x89, 0x44, 0x24, 0x1C, 0x61, 0xC3, 0x60, 0x8B, 0x5C, 0x24,
            0x24, 0x93, 0x66, 0x8B, 0x08, 0x66, 0x8B, 0x13, 0x58, 0x87, 0x04, 0x24, 0x60, 0x8B,
            0x5C, 0x24, 0x24, 0x93, 0x66, 0x8B, 0x08, 0x66, 0x8B, 0x13, 0x66, 0x85, 0xC9, 0x74,
            0x21, 0x90, 0x90, 0x90, 0x90, 0x66, 0x85, 0xD2, 0x74, 0x18, 0x90, 0x90, 0x90, 0x90,
            0x83, 0xC0, 0x02, 0x83, 0xC3, 0x02, 0x66, 0x3B, 0xD1, 0x74, 0xDD, 0x90, 0x90, 0x90,
            0x90, 0xEB, 0x1B, 0x90, 0x90, 0x90, 0x66, 0x85, 0xD2, 0x74, 0x09, 0x90, 0x90, 0x90,
            0x90, 0xEB, 0x0D, 0x90, 0x90, 0x90, 0xB8, 0x01, 0x00, 0x00, 0x00, 0xEB, 0x0D, 0x90,
            0x90, 0x90, 0xB8, 0x00, 0x00, 0x00, 0x00, 0xEB, 0x03, 0x90, 0x90, 0x90, 0x89, 0x44,
            0x24, 0x1C, 0x61, 0x83, 0xC4, 0x08, 0xFF, 0x74, 0x24, 0xF8, 0xC3, 0x90, 0x90, 0x90,
            0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x58, 0x87, 0x04, 0x24, 0x85, 0xC0,
            0x75, 0x0E, 0x90, 0x90, 0x90, 0x90, 0xE8, 0x00, 0x00, 0x00, 0x58, 0x87, 0x04, 0x24,
            0x85, 0xC0, 0x75, 0x0E, 0x90, 0x90, 0x90, 0x90, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x58,
            0x83, 0xC0, 0x3B, 0xC3, 0x66, 0x83, 0x38, 0x00, 0x74, 0x09, 0x90, 0x90, 0x90, 0x90,
            0xEB, 0x09, 0x90, 0x90, 0x90, 0xB8, 0x00, 0x00, 0x00, 0x00, 0xC3, 0x83, 0xC0, 0x02,
            0x66, 0x83, 0x38, 0x00, 0x75, 0xF7, 0x90, 0x90, 0x90, 0x90, 0x83, 0xC0, 0x02, 0x66,
            0x83, 0x38, 0x00, 0x74, 0xE4, 0x90, 0x90, 0x90, 0x90, 0xC3, 0x90, 0x90, 0x90, 0x90,
            0x90, 0x90
        };
    }
}

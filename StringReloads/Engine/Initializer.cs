﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StringReloads.Engine
{
    class Initializer
    {
        internal void Initialize(Main Engine)
        {
            if (Engine.Initialized)
                return;

            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//.net core
            Engine.Settings = new Config();

            Log.Information($"SRL - StringReloads v{Engine.Settings.SRLVersion}");
            Log.Information($"Created by Marcussacana");

            Log.Debug($"Working Directory: {Engine.Settings.WorkingDirectory}");
            Log.Debug("Initializing SRL...");
            
            if (File.Exists(Engine.Settings.CachePath)) {
                var Cache = new Cache(Engine.Settings.CachePath);
                Engine.Databases = Cache.GetDatabases().ToList();
                Engine.CharRemap = Cache.GetRemaps().ToDictionary();
            } else
                BuildCache(Engine);

            Log.Debug($"{Engine.Databases.Count} Database(s) Loaded");
            Log.Debug($"{Engine.CharRemap.Count} Remap(s) Loaded");

            ModifiersInitializer(Engine);
            HooksInitializer(Engine);
            ModsInitializer(Engine);

            AutoInstall(Engine);

            Engine.Initialized = true;
            Log.Information("SRL Initialized");
        }
        
        private void ModifiersInitializer(Main Engine) {
            var Mods = Engine.ReloadModifiers;
            var Settings = Engine.Settings.Modifiers;
            for (int i = 0; i < Mods.Length; i++) {
                if (!Settings.ContainsKey(Mods[i].Name.ToLowerInvariant())) {
                    Log.Warning($"Modifier \"{Mods[i].Name}\" is without configuration.");
                    continue;
                }

                if (!Settings[Mods[i].Name.ToLowerInvariant()]) {
                    Mods = Mods.Remove(Mods[i--]);
                    continue;
                }

                Log.Debug($"String Modifier \"{Mods[i].Name}\" Enabled.");
            }

            Engine._ReloadModifiers = Mods;
        }

        private void HooksInitializer(Main Engine) {
            var Hooks = Engine.Hooks;
            var Settings = Engine.Settings.Hooks;
            for (int i = 0; i < Hooks.Length; i++) {
                if (!Settings.ContainsKey(Hooks[i].Name.ToLowerInvariant())) {
                    Log.Warning($"Hook \"{Hooks[i].Name}\" is without configuration.");
                    continue;
                }

                if (!Settings[Hooks[i].Name.ToLowerInvariant()]) {
                    Hooks = Hooks.Remove(Hooks[i--]);
                    continue;
                }
            }

            Engine._Hooks = Hooks;

            for (int i = 0; i < Hooks.Length; i++) {
                Hooks[i].Install();

                Log.Debug($"Hook \"{Hooks[i].Name}\" Enabled.");
            }
        }

        private void ModsInitializer(Main Engine)
        {
            var Mods = Engine.Mods;
            var Settings = Engine.Settings.Mods;
            for (int i = 0; i < Mods.Length; i++)
            {
                if (!Settings.ContainsKey(Mods[i].Name.ToLowerInvariant()))
                {
                    Log.Warning($"Mod \"{Mods[i].Name}\" is without configuration.");
                    continue;
                }

                if (!Settings[Mods[i].Name.ToLowerInvariant()])
                {
                    Mods = Mods.Remove(Mods[i--]);
                    continue;
                }
            }

            Engine._Mods = Mods;

            for (int i = 0; i < Mods.Length; i++)
            {
                Mods[i].Install();

                Log.Debug($"Mod \"{Mods[i].Name}\" Enabled.");
            }
        }

        private void AutoInstall(Main Engine) {
            if (!Engine.Settings.AutoInstall)
                return;

            for (int i = 0; i < Engine.Installers.Length; i++) {
                if (!Engine.Installers[i].IsCompatible())
                    continue;

                Log.Information($"{Engine.Installers[i].Name} Engine Detected.");
                Engine.Installers[i].Install();
            }
        }

        private void BuildCache(Main Engine) {
            Log.Debug("Cache not found, Building database...");
            Engine.Databases = new List<Database>();
            Engine.CurrentDatabaseIndex = 0;
            Engine.CharRemap = new Dictionary<char, char>();

            if (!Directory.Exists(Engine.Settings.WorkingDirectory))
                Directory.CreateDirectory(Engine.Settings.WorkingDirectory);

            foreach (string Lst in Directory.GetFiles(Engine.Settings.WorkingDirectory, "*.lst"))
            {
                var Parser = new LSTParser(Lst);

                if (Parser.Name.ToLowerInvariant() == "chars")
                    continue;

                Database DB = new Database(Parser.Name);
                DB.AddRange(Parser.GetEntries());

                Log.Debug($"Database {Parser.Name} Initialized (ID: {Engine.Databases.Count})");
                Engine.Databases.Add(DB);
            }

            string CharsFile = Path.Combine(Engine.Settings.WorkingDirectory, "Chars.lst");

            if (File.Exists(CharsFile)) {
                foreach (string Line in File.ReadAllLines(CharsFile)) {
                    if (!Line.Contains("=") || string.IsNullOrWhiteSpace(Line))
                        continue;
                    string PartA = Line.Substring(0, Line.IndexOf("=")).Trim();
                    string PartB = Line.Substring(Line.IndexOf("=") + 1).Trim();

                    char A, B;
                    
                    if (PartA.ToLowerInvariant().StartsWith("0x")) {
                        PartA = PartA.Substring(2);
                        A = (char)Convert.ToInt16(PartA, 16);
                    } else
                        A = PartA.First();

                    if (PartB.ToLowerInvariant().StartsWith("0x")) {
                        PartB = PartB.Substring(2);
                        B = (char)Convert.ToInt16(PartB, 16);
                    } else
                        B = PartB.First();

                    Log.Debug($"Character Remap from {A} to {B}");
                    Engine.CharRemap[A] = B;
                }
            }

            Cache Builder = new Cache(Engine.Settings.CachePath);
            Builder.BuildDatabase(Engine.Databases.ToArray(), Engine.CharRemap.ToArray());
        }
    }

    internal static partial class Extensions {
        public static Dictionary<char, char> ToDictionary(this IEnumerable<KeyValuePair<char, char>> Pairs) {
            var Dic = new Dictionary<char, char>();
            foreach (var Pair in Pairs)
                Dic.Add(Pair.Key, Pair.Value);
            return Dic;
        }

        public static T[] Remove<T>(this T[] Arr, T Item) {
            List<T> Rst = new List<T>();
            for (int i = 0; i < Arr.Length; i++) { 
                if (Arr[i].Equals(Item))
                    continue;

                Rst.Add(Arr[i]);
            }
            return Rst.ToArray();
        }
    }
}

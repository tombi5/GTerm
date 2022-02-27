﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace GTerm
{
    internal class JsonConfig
    {
        public string[] ExclusionPatterns { get; set; }
        public bool ArchiveLogs { get; set; }
        public bool MonitorGmod { get; set; }
        public bool StartAsGmod { get; set; }
    }

    internal class Config
    {
        internal List<Regex> ExclusionPatterns { get; set; } = new List<Regex>();
        internal bool ArchiveLogs { get; set; } = true;
        internal bool MonitorGmod { get; set; } = true;
        internal bool StartAsGmod { get; set; } = false;

        internal Config(string[] args) 
        {
            JsonConfig cfg = new JsonConfig();

            string appPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string configPath = Path.Combine(appPath, "Config.json");
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                cfg = JsonConvert.DeserializeObject<JsonConfig>(json);

            }

            if (args.Length > 0)
            {
                Dictionary<string, List<string>> options = this.ParseCLIArgs(args);
                this.ProcessOptions(options, ref cfg);
            }

            this.ProcessConfig(cfg);
        }

        private void ProcessConfig(JsonConfig cfg)
        {
            this.ArchiveLogs = cfg.ArchiveLogs;
            this.MonitorGmod = cfg.MonitorGmod;
            this.StartAsGmod = cfg.StartAsGmod;

            foreach (string pattern in cfg.ExclusionPatterns)
            {
                this.ExclusionPatterns.Add(new Regex(pattern, RegexOptions.Compiled));
            }

            LocalLogger.WriteLine("Config params:");
            LocalLogger.WriteLine("Logs archiving: " + this.ArchiveLogs);
            LocalLogger.WriteLine("Gmod monitoring: " + this.MonitorGmod);
            LocalLogger.WriteLine("Start as Gmod: " + this.StartAsGmod);
            LocalLogger.WriteLine("Exclusion Patterns: \n", string.Join("\n", this.ExclusionPatterns.Select(r => r.ToString())));
        }

        private Dictionary<string, List<string>> ParseCLIArgs(string[] args)
        {
            args = args.Select(arg => arg.Trim().ToLower()).ToArray();

            List<string> knownOptionParams = null;
            Dictionary<string, List<string>> options = new Dictionary<string, List<string>>();

            string curOption = null;
            List<string> curOptionParams = new List<string>();
            foreach (string arg in args)
            {
                if (arg.StartsWith("--"))
                {
                    if (curOption != null)
                    {
                        if (options.TryGetValue(curOption, out knownOptionParams))
                        {
                            knownOptionParams.AddRange(curOptionParams);
                            curOptionParams.Clear();
                        }
                        else
                        {
                            options.Add(curOption, curOptionParams);
                            curOptionParams.Clear();
                        }
                    }

                    if (arg.Length > 3)
                        curOption = arg.Substring(2);
                }
                else
                {
                    curOptionParams.Add(arg);
                }
            }

            // for the last option
            if (options.TryGetValue(curOption, out knownOptionParams))
                knownOptionParams.AddRange(curOptionParams);
            else
                options.Add(curOption, curOptionParams);

            return options;
        }

        private void ProcessOptions(Dictionary<string, List<string>> options, ref JsonConfig curCfg)
        {
            Type baseCfgType = typeof(JsonConfig);
            PropertyInfo[] props = baseCfgType.GetProperties();
            foreach (KeyValuePair<string, List<string>> option in options)
            {
                PropertyInfo prop = props.FirstOrDefault(p => p.Name.ToLower() == option.Key);
                if (prop == null) continue;

                switch (prop.PropertyType)
                {
                    case Type t when t == typeof(bool):
                        bool value = true;
                        if (option.Value.Count > 0 && int.TryParse(option.Value.Last(), out int parsedValue))
                            value = parsedValue > 0;

                        prop.SetValue(curCfg, value);
                        break;

                    case Type t when t == typeof(string[]):
                        prop.SetValue(curCfg, option.Value.ToArray());
                        break;

                    default:
                        break;
                }
            }
        }
    }
}

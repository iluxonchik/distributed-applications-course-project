using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuppetMaster
{
    public class ConfigParser
    {
        private readonly StreamReader confFile;
        private const string LOGGING_LVL_REGEX = @"^LoggingLevel (full|light)\s*\r?$";
        private readonly Action<string, Config>[] regexParsers;
        private readonly string[] REGEX_LIST = { LOGGING_LVL_REGEX };

        public ConfigParser(string confFilePath)
        {   
            // TODO: error checking
            confFile = new StreamReader(confFilePath);
            regexParsers = new Action<string, Config>[]{
                (fileContent, config) => ParseLoggingLevel(fileContent, config),
            };
        }

        public Config Parse()
        {
            // I'm aware that this might not be the most efficient way to do it, but
            // it's a pretty clean way to do so, since the config file will only be parsed
            // once at the appication startup, this shouldn't be an issue.
            string fileCont = confFile.ReadToEnd(); // storing the whole file in memory is very efficient :)
            Config conf = new Config();
            foreach (var parser in regexParsers)
            { 
                parser.Invoke(fileCont, conf);
            }
            return conf;
        }

        private void ParseLoggingLevel(string fileContent, Config conf)
        {
            Match match = Regex.Match(fileContent, LOGGING_LVL_REGEX, RegexOptions.Multiline);
            if (match.Success)
            {
                switch(match.Groups[1].Value)
                {
                    case "full":
                        conf.LoggingLevel = LoggingLevel.FULL;
                        break;
                    case "light":
                        conf.LoggingLevel = LoggingLevel.LIGHT;
                        break;
                    default:
                        conf.LoggingLevel = LoggingLevel.LIGHT;
                        break;
                }
            } else
            {
                conf.LoggingLevel = LoggingLevel.LIGHT;
            }
        }
    }
}

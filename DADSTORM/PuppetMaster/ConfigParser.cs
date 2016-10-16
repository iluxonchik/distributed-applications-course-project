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
        /*
         * match.Groups["address_list"].Captures[1]
         */
        private readonly StreamReader confFile;
        private const string LOGGING_LVL_REGEX = @"^LoggingLevel (full|light)\s*?\r?$";
        private const string SEMANTICS_REGEX = @"^Semantics (at-least-once|at-most-once|exactly-once)\s*?\r?$";
        private const string OPERATOR_REGEX = @"^(?<operator_id>\w)+? INPUT_OPS (?<input_ops>(?<input_op>(\w\.*)+),? ?)+\r?\n" + // NOTE: accepts "," at the end
                                              @"REP_FACT (?<rep_fact>\d+) ROUTING (?<routing>\w+?)\r?\n" +
                                              @"ADDRESS (?<address_list>(?<address>tcp:\/\/\d\.\d\.\d\.\d\:(\d+)\/[\w-]+),? ?)+\r?\n" + // NOTE: accepts "," at the end
                                              @"OPERATOR_SPEC (?<operator_spec>(?<op_uniq>UNIQ (?<op_uniq_field>\d+))|(?<op_count>COUNT)|(?<op_dup>DUP)|(?<op_filter>FILTER (?<op_filter_field>\d+), (?<op_filter_cond>""(>|<|=)""), (?<op_filter_value>""[a-zA-Z0-10\.\d]+""))|(?<op_custom>CUSTOM (?<op_custom_dll>""\w+\.dll""), (?<op_custom_class>""\w+""), (?<op_custom_method>""\w+"")))\r?\n?";
        private readonly Action<string, Config>[] regexParsers;
        private readonly string[] REGEX_LIST = { LOGGING_LVL_REGEX };

        public ConfigParser(string confFilePath)
        {   
            // TODO: error checking
            confFile = new StreamReader(confFilePath);
            regexParsers = new Action<string, Config>[]{
                (fileContent, config) => ParseLoggingLevel(fileContent, config),
                (fileContent, config) => ParseSemanthics(fileContent, config),
                (fileContent, config) => ParseOperators(fileContent, config),
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
                        conf.LoggingLevel = LoggingLevel.Full;
                        break;
                    case "light":
                        conf.LoggingLevel = LoggingLevel.Light;
                        break;
                    default:
                        conf.LoggingLevel = LoggingLevel.Light;
                        break;
                }
            } else
            {
                conf.LoggingLevel = LoggingLevel.Light;
            }
        }

        private void ParseSemanthics(string fileContent, Config conf)
        {
            Match match = Regex.Match(fileContent, SEMANTICS_REGEX, RegexOptions.Multiline);
            if (match.Success)
            {
                switch (match.Groups[1].Value)
                {
                    case "at-least-once":
                        conf.Semantics = Semantics.AtLeastOnce;
                        break;
                    case "at-most-once":
                        conf.Semantics = Semantics.AtMostOnce;
                        break;
                    case "exactly-once":
                        conf.Semantics = Semantics.ExactlyOnce;
                        break;
                    default:
                        conf.Semantics = Semantics.AtLeastOnce;
                        break;
                }
            }
            else
            {
                conf.Semantics = Semantics.AtLeastOnce;
            }
        }

        private void ParseOperators(string fileContent, Config conf)
        {
           // TODO
        }
    }
}

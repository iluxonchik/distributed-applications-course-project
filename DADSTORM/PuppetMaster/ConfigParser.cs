using PuppetMaster.Exceptions;
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
        private const string OPERATOR_REGEX = @"^(?<operator_id>\w+) (input ops) (?<input_ops>(?<input_op>(\w+\.?){1,2})|((?<input_op>(\w+\.?){1,2}), ?)+(?<input_op>(\w+\.?){1,2}))+(\r?\n| )" + // you'll have to trust that this works 😂😂😂
                                              @"(rep fact) (?<rep_fact>\d+) (routing) (?<routing>(primary|random|hashing\(\d+\)))(\r?\n| )" +
                                              @"(address) (?<address_list>(?<address>tcp:\/\/\d+\.\d+\.\d+\.\d+\:(\d+)\/[\w-]+),? ?)+(\r?\n| )" + // NOTE: accepts "," at the end
                                              @"(operator spec) (?<op_spec>(?<op_uniq>UNIQ (?<op_uniq_field>\d+))|(?<op_count>COUNT)|(?<op_dup>DUP)|(?<op_filter>FILTER (?<op_filter_field>\d+), ?(?<op_filter_cond>(>|<|=)), ?(?<op_filter_value>""?[a-zA-Z0-10\.\d]+""?))|(?<op_custom>CUSTOM (?<op_custom_dll>\w+\.dll), ?(?<op_custom_class>\w+), ?(?<op_custom_method>\w+)))\r?\n?";
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
            MatchCollection mc = Regex.Matches(fileContent, OPERATOR_REGEX, RegexOptions.Multiline);
            List<OperatorSpec> operators = new List<OperatorSpec>();
            foreach (Match m in mc)
            {
                OperatorSpec os = new OperatorSpec();
                os.Id = m.Groups["operator_id"].Value;
                os.ReplicationFactor = Int32.Parse(m.Groups["rep_fact"].Value);

                os.Type = ParseOperatorType(m);
                os.Inputs = ParseOperatorInputList(m);
                os.Addrs = ParseOperatorAddrList(m);
                os.Args = ParseOperatorArgList(m, os.Type); // yeah, dependency from ParseOperatorType, but simplifies things
                os.Routing = ParseOperatorRouting(m);
                operators.Add(os);
            }
            conf.Operators = operators;
        }

        private OperatorType ParseOperatorType(Match m)
        {
            if (!String.IsNullOrEmpty(m.Groups["op_uniq"].Value))
            {
                return OperatorType.Uniq;
            }

            if (!String.IsNullOrEmpty(m.Groups["op_dup"].Value))
            {
                return OperatorType.Dup;
            }

            if (!String.IsNullOrEmpty(m.Groups["op_filter"].Value))
            {
                return OperatorType.Filer;
            }

            if (!String.IsNullOrEmpty(m.Groups["op_custom"].Value))
            {
                return OperatorType.Custom;
            }

            if (!String.IsNullOrEmpty(m.Groups["op_count"].Value))
            {
                return OperatorType.Count;
            }

            // it's better to stop than assume some default values
            throw new UnknownOperatorTypeException(String.Format("{0} is an invalid value for OPERATOR_SPEC", m.Groups["op_spec"]));        }

        private OperatorRouting ParseOperatorRouting(Match m)
        {
            string routingValue = m.Groups["routing"].Value;
            OperatorRouting or = new OperatorRouting();
            switch(routingValue)
            {
                case "random":
                     or.Type = RoutingType.Random;
                     break;
                case "primary":
                    or.Type = RoutingType.Primary;
                    break;
                default:
                    // since hashing operator routing invloves Regex and possible exceptio throwing, move it to its own method
                    ParseHashingRouting(or, routingValue);
                    break;
            }
            return or;
        }

        private void ParseHashingRouting(OperatorRouting or, string routingValue)
        {
            Match m = Regex.Match(routingValue, @"hashing\((?<arg>\d+)\)");
            if (m.Success)
            {
                or.Type = RoutingType.Hashing;
                or.Arg = Int32.Parse(m.Groups["arg"].Value);
            } else
            {
                throw new UnknownOperatorRoutingException();
            }
        }

        private List<string> ParseOperatorArgList(Match m, OperatorType opType)
        {
            List<string> argList = new List<string>();
            if (opType == OperatorType.Count || opType == OperatorType.Dup)
            {
                // do nothing, this is here for clarity 
            }
            else if (opType == OperatorType.Uniq)
            {
                argList.Add(m.Groups["op_uniq_field"].Value);
            }
            else if (opType == OperatorType.Custom) {
                argList.Add(m.Groups["op_custom_dll"].Value);
                argList.Add(m.Groups["op_custom_class"].Value);
                argList.Add(m.Groups["op_custom_method"].Value);
            }
            else if (opType == OperatorType.Filer)
            {
                argList.Add(m.Groups["op_filter_field"].Value);
                argList.Add(m.Groups["op_filter_cond"].Value);
                argList.Add(m.Groups["op_filter_value"].Value);

            }
            return argList;
        }

        private List<string> ParseOperatorAddrList(Match m)
        {
            List<string> addrList = new List<string>();
            foreach (Capture c in m.Groups["address"].Captures)
            {
                addrList.Add(c.Value);
            }
            return addrList;
        }

        private List<OperatorInput> ParseOperatorInputList(Match m)
        {
            List<OperatorInput> opInputList = new List<OperatorInput>();
            
            foreach(Capture c in m.Groups["input_op"].Captures)
            {
                OperatorInput opInput = new OperatorInput();
                opInput.Name =  c.Value;
                // lab teacher said that we can assume that an input op is a file if it contains a "."
                opInput.Type = opInput.Name.Contains(".") ? InputType.File : InputType.Operator;
                opInputList.Add(opInput);
            }
            return opInputList;
        }
    }
}

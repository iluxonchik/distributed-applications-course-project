using ConfigTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuppetMaster
{
    public class CommandParser
    {

        private readonly StreamReader configFile;
        private readonly Config conf;

        private const string START_CMD_REGEX = @"(?<cmd_start>Start (?<operator_id>\w+))\r?";
        private const string INTERVAL_CMD_REGEX = @"(?<cmd_interval>Interval (?<operator_id>\w+) (?<ms>\d+))\r?";
        private const string STATUS_CMD_REGEX = @"(?<cmd_status>Status)\r?";
        private const string CRASH_CMD_REGEX = @"(?<cmd_crash>Crash (?<operator_id>\w+) (?<rep_id>\d+))\r?";
        private const string FREEZE_CMD_REGEX = @"(?<cmd_freeze>Freeze (?<operator_id>\w+) (?<rep_id>\d+))\r?";
        private const string UNFREEZE_CMD_REGEX = @"(?<cmd_unfreeze>Unfreeze (?<operator_id>\w+) (?<rep_id>\d+))\r?";
        private const string WAIT_CMD_REGEX = @"(?<cmd_wait>Wait (?<ms>\d+))\r?";

        private readonly string CMD_REGEX = String.Format("^({0}|{1}|{2}|{3}|{4}|{5}|{6})$", START_CMD_REGEX, INTERVAL_CMD_REGEX, STATUS_CMD_REGEX,
                CRASH_CMD_REGEX, FREEZE_CMD_REGEX, UNFREEZE_CMD_REGEX, WAIT_CMD_REGEX);


        public CommandParser(string confFilePath, Config conf)
        {
            configFile = new StreamReader(confFilePath);
            this.conf = conf;
        }

        /// <summary>
        /// Parse PuppetMaster commands from the config file. NOTE: this should be run after parsing the commands (since it requires the operator
        /// list to be passed in its constructor).
        /// </summary>
        /// <returns></returns>
        public Queue<Command> Parse()
        {
            string fileContent = configFile.ReadToEnd();
            MatchCollection mc = Regex.Matches(fileContent, CMD_REGEX, RegexOptions.Multiline);
            Queue<Command> commands = new Queue<Command>();

            foreach (Match m in mc)
            {
                Command c = ParseCommandFromMatch(m);
                commands.Enqueue(c);
            }

            return commands;
        }

        private Command ParseCommandFromMatch(Match m)
        {
            CommandType ctype = ParseCommandType(m);
            return ParseCommandByType(ctype, m);

        }

        private Command ParseCommandByType(CommandType ctype, Match m)
        {
            // vars used in switch below
            string opId;
            int repId;
            int ms;

            Command c = new Command();
            c.Type = ctype;

            switch (ctype)
            {
                case CommandType.Start:
                    opId = m.Groups["operator_id"].Value; // it's all good, if this lookup fails, we want it to blow up
                    c.Operator = conf.OPnameToOpSpec[opId];
                    break;
                case CommandType.Interval:
                    opId = m.Groups["operator_id"].Value;
                    ms = Int32.Parse(m.Groups["ms"].Value);
                    c.Operator = conf.OPnameToOpSpec[opId];
                    c.MS = ms;
                    break;
                case CommandType.Status:
                    // empty
                    break;
                case CommandType.Crash:
                // empty, fallthrough: same code as in case CommandType.Unfreeze    
                case CommandType.Freeze:
                // empty, fallthrough: same code as in case CommandType.Unfreeze
                case CommandType.Unfreeze:
                    opId = m.Groups["operator_id"].Value;
                    repId = Int32.Parse(m.Groups["rep_id"].Value);
                    c.Operator = conf.OPnameToOpSpec[opId];
                    c.RepId = repId;
                    break;
                case CommandType.Wait:
                    ms = Int32.Parse(m.Groups["ms"].Value);
                    c.MS = ms;
                    break;
                default:
                    // something went wrong, but this really shouldn't happpen (unless someone modified the CommandType class)
                    throw new UnknownCommandTypeException(String.Format("'{0}' is an unknown command Type. Maybe you've done changes to the CommandType class", ctype));
            }
            return c;
        }

        private CommandType ParseCommandType(Match m)
        {

            if (!String.IsNullOrEmpty(m.Groups["cmd_start"].Value))
            {
                return CommandType.Start;
            }

            if (!String.IsNullOrEmpty(m.Groups["cmd_interval"].Value))
            {
                return CommandType.Interval;
            }

            if (!String.IsNullOrEmpty(m.Groups["cmd_status"].Value))
            {
                return CommandType.Status;
            }

            if (!String.IsNullOrEmpty(m.Groups["cmd_crash"].Value))
            {
                return CommandType.Crash;
            }

            if (!String.IsNullOrEmpty(m.Groups["cmd_freeze"].Value))
            {
                return CommandType.Freeze;
            }

            if (!String.IsNullOrEmpty(m.Groups["cmd_unfreeze"].Value))
            {
                return CommandType.Unfreeze;
            }

            if (!String.IsNullOrEmpty(m.Groups["cmd_wait"].Value))
            {
                return CommandType.Wait;
            }
            // it's better to stop than assume some default values
            throw new UnknownCommandTypeException(String.Format("Invalid CommandType"));

        }

    }
}

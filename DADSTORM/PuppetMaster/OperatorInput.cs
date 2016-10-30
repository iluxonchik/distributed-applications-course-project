using System;

namespace PuppetMaster
{
    [Serializable]
    public class OperatorInput
    {
        public string Name;
        public InputType Type { get; set; }
    }
}
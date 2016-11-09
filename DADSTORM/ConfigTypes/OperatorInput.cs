using System;
using System.Collections.Generic;

namespace ConfigTypes
{
    [Serializable]
    public class OperatorInput
    {
        public string Name;
        public List<string> Addresses { get; set; } = new List<string>(); // only applies to InputType.Operator
        public InputType Type { get; set; }

        public override string ToString()
        {
            const string BASE_FORMAT = "Name: {0}, Type: {1}";
            if (Type.Equals(InputType.File))
            {
                return string.Format(BASE_FORMAT, Name, Type);
            }
            else
            {
                return string.Format(BASE_FORMAT + ", Address: {2}", Name, Type, Addresses);
            }
        }
    }
}

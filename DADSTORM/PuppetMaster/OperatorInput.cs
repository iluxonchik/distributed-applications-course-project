namespace PuppetMaster
{
    public class OperatorInput
    {
        public string Name;
        public InputType Type { get; set; }

        public override string ToString()
        {
            return string.Format("Name: {0}, Type: {1}", Name, Type);
        }
    }
}
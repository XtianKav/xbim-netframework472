namespace libal.Domain
{
    public class PsetPropertyPair
    {
        public string psetName { get; set; }
        public string propertyName { get; set; }
        public PsetPropertyPair(string psetName, string propertyName)
        {
            this.psetName = psetName;
            this.propertyName = propertyName;
        }

    }
}
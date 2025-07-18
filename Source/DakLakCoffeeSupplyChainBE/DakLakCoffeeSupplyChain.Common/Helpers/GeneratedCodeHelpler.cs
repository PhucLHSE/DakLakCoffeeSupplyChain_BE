namespace DakLakCoffeeSupplyChain.Common.Helpers
{
    public static class GeneratedCodeHelpler
    {
        public static int GetGeneratedCodeLastNumber(string code)
        {
            string[] parts = code.Split('-');
            string numberPart = parts[^1];
            return int.Parse(numberPart);
        }
    }
}

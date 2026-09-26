namespace EfCore.Repository.Unit.Test.Entities
{
    public class OrderLine : ITestEntity
    {
        public int OrderId { get; set; }

        public int LineNumber { get; set; }

        public string Product { get; set; }
    }
}

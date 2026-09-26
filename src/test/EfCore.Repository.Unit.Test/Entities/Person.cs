namespace EfCore.Repository.Unit.Test.Entities
{
    public class Person : ITestEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public List<Basket> Baskets { get; set; } = new();
    }
}

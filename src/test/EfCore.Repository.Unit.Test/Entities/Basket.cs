namespace EfCore.Repository.Unit.Test.Entities
{
    public class Basket : ITestEntity
    {
        public int Id { get; set; }

        public string Description { get; set; }

        public int PersonId { get; set; }

        public Person Person { get; set; }
    }
}

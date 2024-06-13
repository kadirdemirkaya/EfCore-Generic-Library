namespace EfCore.Test.Dtos
{
    public class PersonDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }

        public List<BasketDto> BasketDtos { get; set; }
    }
}

namespace EfCore.Test.Dtos
{
    public class BasketDto
    {
        public int Id { get; set; }
        public string Description { get; set; }

        public int PersonId { get; set; }

        public List<ProductDto> ProductDtos { get; set; }
    }
}

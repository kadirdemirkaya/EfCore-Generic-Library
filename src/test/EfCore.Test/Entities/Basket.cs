using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EfCore.Test.Entities
{
    public class Basket
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Description { get; set; }

        public int PersonId { get; set; }
        public Person Person { get; set; }

        public List<Product> Products { get; set; }
    }
}

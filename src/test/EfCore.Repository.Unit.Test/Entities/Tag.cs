using System.ComponentModel.DataAnnotations;

namespace EfCore.Repository.Unit.Test.Entities
{
    public class Tag : ITestEntity
    {
        [Key]
        public string Code { get; set; }

        public string Label { get; set; }
    }
}

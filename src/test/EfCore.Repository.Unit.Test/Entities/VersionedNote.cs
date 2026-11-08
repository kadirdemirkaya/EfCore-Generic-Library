using System.ComponentModel.DataAnnotations;

namespace EfCore.Repository.Unit.Test.Entities
{
    public class VersionedNote
    {
        public int Id { get; set; }

        public string Text { get; set; }

        [ConcurrencyCheck]
        public int Version { get; set; }
    }
}

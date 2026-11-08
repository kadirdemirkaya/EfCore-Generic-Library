namespace EfCore.Repository.Unit.Test.Entities
{
    public class ArchivableItem
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Stock { get; set; }

        public bool IsArchived { get; set; }
    }
}

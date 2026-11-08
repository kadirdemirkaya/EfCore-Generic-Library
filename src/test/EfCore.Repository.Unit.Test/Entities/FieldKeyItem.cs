namespace EfCore.Repository.Unit.Test.Entities
{
    public class FieldKeyItem
    {
        private int _id;

        public FieldKeyItem()
        {
        }

        public FieldKeyItem(int key, string name)
        {
            _id = key;
            Name = name;
        }

        public string Name { get; set; }

        public int KeyValue() => _id;
    }
}

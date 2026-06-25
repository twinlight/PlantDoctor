using SQLite;

namespace PlantDoctor.Models
{
    [Table("DbMeta")]
    public class DbMeta
    {
        [PrimaryKey]
        public int Id { get; set; }
        public int Version { get; set; }
    }
}


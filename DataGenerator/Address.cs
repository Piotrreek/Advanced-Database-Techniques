using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataGenerator;

[Table("address")]
public sealed class Address
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [ForeignKey("Person")]
    [Column("person_id")]
    public int PersonId { get; set; }

    [Column("street")]
    public string Street { get; set; } = default!;

    [Column("city")]
    public string City { get; set; } = default!;

    [Column("state")]
    public string State { get; set; } = default!;

    [Column("zip_code")]
    public string ZipCode { get; set; } = default!;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdvancedDatabaseTechniques;

[Table("person")]
internal sealed class Person
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("first_name")]
    public string FirstName { get; set; } = default!;
    [Column("last_name")]
    public string LastName { get; set; } = default!;
    [Column("phone_number")]
    public string PhoneNumber { get; set; } = default!;
}
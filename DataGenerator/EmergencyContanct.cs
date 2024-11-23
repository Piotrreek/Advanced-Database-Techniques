using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataGenerator;

[Table("emergency_contact")]
public sealed class EmergencyContact
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [ForeignKey("Person")]
    [Column("person_id")]
    public int PersonId { get; set; }

    [Column("contact_name")]
    public string ContactName { get; set; } = default!;

    [Column("relationship")]
    public string Relationship { get; set; } = default!; // e.g., Parent, Spouse

    [Column("phone_number")]
    public string PhoneNumber { get; set; } = default!;

    [Column("email_address")]
    public string EmailAddress { get; set; } = default!;
}

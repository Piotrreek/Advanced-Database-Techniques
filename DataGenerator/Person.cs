using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataGenerator;

[Table("person")]
public sealed class Person
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
    
    public Address Address { get; set; } = default!;
    public EmergencyContact EmergencyContact { get; set; } = default!;
    public Job Job { get; set; } = default!;
    public SocialMedia SocialMedia { get; set; } = default!;
}

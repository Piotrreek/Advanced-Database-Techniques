using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataGenerator;

[Table("social_media")]
public sealed class SocialMedia
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [ForeignKey("Person")]
    [Column("person_id")]
    public int PersonId { get; set; }

    [Column("platform")]
    public string Platform { get; set; } = default!; // e.g., Twitter, LinkedIn

    [Column("profile_url")]
    public string ProfileUrl { get; set; } = default!;
}

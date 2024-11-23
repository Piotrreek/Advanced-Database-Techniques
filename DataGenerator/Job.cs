using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataGenerator;

[Table("job")]
public sealed class Job
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [ForeignKey("Person")]
    [Column("person_id")]
    public int PersonId { get; set; }

    [Column("job_title")]
    public string JobTitle { get; set; } = default!;

    [Column("company_name")]
    public string CompanyName { get; set; } = default!;

    [Column("salary")]
    public decimal Salary { get; set; }
}

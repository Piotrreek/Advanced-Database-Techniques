using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Bogus;
using DataGenerator;
using Person = DataGenerator.Person;

string[] relationShips = ["brother", "sister", "son", "father", "mother", "aunt", "uncle", "daughter"];

var addressGenerator = new Faker<Address>("pl")
    .RuleFor(x => x.City, f => f.Address.City())
    .RuleFor(x => x.State, f => f.Address.State())
    .RuleFor(x => x.ZipCode, f => f.Address.ZipCode())
    .RuleFor(x => x.Street, f => f.Address.StreetAddress());

var emergencyContactGenerator = new Faker<EmergencyContact>("pl")
    .RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber())
    .RuleFor(x => x.Relationship, f => f.PickRandom(relationShips))
    .RuleFor(x => x.ContactName, f => f.Name.FullName())
    .RuleFor(x => x.EmailAddress, f => f.Internet.Email());

var jobGenerator = new Faker<Job>("pl")
    .RuleFor(x => x.Salary, f => f.Finance.Amount())
    .RuleFor(x => x.CompanyName, f => f.Company.CompanyName())
    .RuleFor(x => x.JobTitle, f => f.Name.JobTitle());

var socialMediaGenerator = new Faker<SocialMedia>("pl")
    .RuleFor(x => x.Platform, f => f.Internet.DomainName())
    .RuleFor(x => x.ProfileUrl, f => f.Internet.Url());

var personGenerator = new Faker<Person>("pl")
    .RuleFor(x => x.FirstName, f => f.Name.FirstName())
    .RuleFor(x => x.LastName, f => f.Name.LastName())
    .RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber())
    .RuleFor(x => x.Address, _ => addressGenerator.Generate())
    .RuleFor(x => x.EmergencyContact, _ => emergencyContactGenerator.Generate())
    .RuleFor(x => x.Job, _ => jobGenerator.Generate())
    .RuleFor(x => x.SocialMedia, _ => socialMediaGenerator.Generate());

int[] counts = [1, 10, 100, 1000, 10_000];

foreach (var count in counts)
{
    await using var writer =
        new StreamWriter($"{Environment.CurrentDirectory}/../../../PeopleData/people-{count}.json", false,
            new UTF8Encoding(false));

    writer.Write("[");

    for (var i = 0; i < count; i++)
    {
        var person = personGenerator.Generate();
        writer.Write($"{JsonSerializer.Serialize(person, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        })}{(i == count - 1 ? "" : ",")}");
    }

    writer.Write("]");
}

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Bogus;
using Person = DataGenerator.Person;

var personGenerator = new Faker<Person>("pl")
    .RuleFor(x => x.FirstName, f => f.Name.FirstName())
    .RuleFor(x => x.LastName, f => f.Name.LastName())
    .RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber());

int[] counts = [1, 10, 100, 1000, 10_000, 100_000, 1_000_000, 10_000_000];

foreach (var count in counts)
{
    await using var writer =
        new StreamWriter($@"{Environment.CurrentDirectory}/../../../PeopleData/people-{count}.json", false,
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
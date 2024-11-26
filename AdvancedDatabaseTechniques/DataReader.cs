using System.Text.Json;
using DataGenerator;

namespace AdvancedDatabaseTechniques;

public static class DataReader
{
    public static List<Person> ReadPeople(int n)
    {
        using var reader =
            new StreamReader(
                $"{Environment.CurrentDirectory}/../../../../../../../../DataGenerator/PeopleData/people-{n}.json");

        return JsonSerializer.Deserialize<List<Person>>(reader.ReadToEnd())!
            .Select((x, index) =>
            {
                x.Id = index;
                x.SocialMedia.Id = index;
                x.SocialMedia.PersonId = index;
                x.Address.Id = index;
                x.Address.PersonId = index;
                x.EmergencyContact.Id = index;
                x.EmergencyContact.PersonId = index;
                x.Job.Id = index;
                x.Job.PersonId = index;

                return x;
            }).ToList();
    }
}

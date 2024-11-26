namespace AdvancedDatabaseTechniques.Postgres;

public class Queries
{
    public const string CreateTablesQuery = """
                                           CREATE TABLE IF NOT EXISTS person (
                                              id SERIAL PRIMARY KEY,
                                              first_name VARCHAR(50),
                                              last_name VARCHAR(50),
                                              phone_number VARCHAR(50)
                                           );

                                           CREATE TABLE IF NOT EXISTS address (
                                               id SERIAL PRIMARY KEY,
                                               person_id INT NOT NULL REFERENCES person(id) ON DELETE CASCADE,
                                               street VARCHAR(255) NOT NULL,
                                               city VARCHAR(100) NOT NULL,
                                               state VARCHAR(100) NOT NULL,
                                               zip_code VARCHAR(20) NOT NULL
                                           );

                                           CREATE TABLE IF NOT EXISTS job (
                                               id SERIAL PRIMARY KEY,
                                               person_id INT NOT NULL REFERENCES person(id) ON DELETE CASCADE,
                                               job_title VARCHAR(255) NOT NULL,
                                               company_name VARCHAR(255) NOT NULL,
                                               salary NUMERIC(12, 2) NOT NULL
                                           );

                                           CREATE TABLE IF NOT EXISTS social_media (
                                               id SERIAL PRIMARY KEY,
                                               person_id INT NOT NULL REFERENCES person(id) ON DELETE CASCADE,
                                               platform VARCHAR(100) NOT NULL,
                                               profile_url VARCHAR(255) NOT NULL
                                           );

                                           CREATE TABLE IF NOT EXISTS emergency_contact (
                                               id SERIAL PRIMARY KEY,
                                               person_id INT NOT NULL REFERENCES person(id) ON DELETE CASCADE,
                                               contact_name VARCHAR(255) NOT NULL,
                                               relationship VARCHAR(100) NOT NULL,
                                               phone_number VARCHAR(50) NOT NULL,
                                               email_address VARCHAR(255)
                                           );
                                           """;

    public const string TruncateTablesQuery = "TRUNCATE TABLE person CASCADE";
    
    public const string InsertEmergencyContactDataQuery =
        "INSERT INTO emergency_contact (id, person_id, contact_name, relationship, phone_number, email_address) VALUES (@Id, @PersonId, @ContactName, @Relationship, @PhoneNumber, @EmailAddress)";

    public const string InsertSocialMediaDataQuery =
        "INSERT INTO social_media (id, person_id, platform, profile_url) VALUES (@Id, @PersonId, @Platform, @ProfileUrl)";

    public const string InsertJobDataQuery =
        "INSERT INTO job (id, person_id, job_title, company_name, salary) VALUES (@Id, @PersonId, @JobTitle, @CompanyName, @Salary)";

    public const string InsertAddressDataQuery =
        "INSERT INTO address (id, person_id, street, city, state, zip_code) VALUES (@Id, @PersonId, @Street, @City, @State, @ZipCode)";

    public const string InsertPersonDataQuery =
        "INSERT INTO person (id, first_name, last_name, phone_number) VALUES (@Id, @FirstName, @LastName, @PhoneNumber)";
}

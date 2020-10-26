
namespace Demonstration
{
    class Program
    {

        static void Main(string[] args)
        {
            new Pipeline().Generate(".\\Generated Tests", new string[] { "f:\\Projects\\Visual Studio\\TestsGenerator\\TestsGeneratorLibrary\\TestsGenerator.cs",
                                                                         "f:\\=university=\\3 Курс\\СПП\\Lab1_Tracer\\TraceLib\\Tracer.cs",
                                                                         "f:\\=university=\\3 Курс\\СПП\\Lab2_Faker\\FakerLib\\Faker.cs",
                                                                         "f:\\Projects\\Visual Studio\\TestsGenerator\\TestsGeneratorUnitTests\\TestPurposeClass.cs"}, 1);
        }
    }
}

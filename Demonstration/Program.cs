
namespace Demonstration
{
    class Program
    {

        static void Main(string[] args)
        {
            new Pipeline().Generate(".\\Generated Tests", new string[] { "..\\..\\..\\..\\Demonstration\\TestPurposeClass.cs",
                                                                         "..\\..\\..\\..\\TestsGeneratorLibrary\\TestsGenerator.cs",}, 2);
        }
    }
}

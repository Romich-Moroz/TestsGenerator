
using System.Threading.Tasks;

namespace Demonstration
{
    class Program
    {

        static async Task Main(string[] args)
        {
            await new Pipeline().Generate(".\\Generated Tests", new string[] { "..\\..\\..\\..\\Demonstration\\TestPurposeClass.cs",
                                                                         "..\\..\\..\\..\\TestsGeneratorLibrary\\TestsGenerator.cs",}, 2);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace TestsGeneratorLibrary
{
    public class TestUnit
    {
        public string filename { get; }
        public string sourceCode { get; }

        public TestUnit(string filename, string sourceCode)
        {
            this.filename = filename;
            this.sourceCode = sourceCode;
        }
    }
}

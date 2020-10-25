using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TestsGeneratorLibrary;
using static TestsGeneratorLibrary.TestsGenerator;

namespace Demonstration
{
    class Pipeline
    {

        public void Generate(string destFolder, string[] filenames, int maxPipelineTasks)
        {
            TestsGenerator generator = new TestsGenerator();
            var execOptions = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxPipelineTasks };
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            
            Directory.CreateDirectory(destFolder);

            TransformBlock<string, string> loadFile = new TransformBlock<string, string>
                (
                    async path => await File.ReadAllTextAsync(path),
                    execOptions
                );
            TransformBlock<string, FileContent[]> generateTests = new TransformBlock<string, FileContent[]>
                (
                    async sourceCode => await Task.Run(() => generator.GenerateTests(sourceCode)),
                    execOptions
                );
            ActionBlock<FileContent[]> writeFile = new ActionBlock<FileContent[]>
                (
                    async filesContent =>
                    {
                        foreach (FileContent f in filesContent)
                            await File.WriteAllTextAsync(destFolder + '\\' + f.filename + ".cs", f.content);
                    },
                    execOptions
                );

            loadFile.LinkTo(generateTests, linkOptions);
            generateTests.LinkTo(writeFile, linkOptions);

            foreach (string filename in filenames)
            {
                loadFile.Post(filename);
            }

            loadFile.Complete();
            writeFile.Completion.Wait();
        }
    }
}

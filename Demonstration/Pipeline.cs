using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TestsGeneratorLibrary;

namespace Demonstration
{
    class Pipeline
    {
        struct FileContent
        {
            public string filename;
            public string content;
        }

        private static TestsGenerator generator = new TestsGenerator();

        private TransformBlock<string, FileContent> loadFile;
        private TransformBlock<FileContent, FileContent> generateTests;
        private ActionBlock<FileContent> writeFile;

        private DataflowLinkOptions linkOptions = new DataflowLinkOptions { PropagateCompletion = true };


        public void Generate(string destFolder, string[] filenames, int maxPipelineTasks)
        {

            Directory.CreateDirectory(destFolder);

            loadFile = new TransformBlock<string, FileContent>
                (
                    async path => new FileContent 
                    {
                        filename = destFolder + '\\' + Path.GetFileNameWithoutExtension(path) + "Tests.cs",
                        content = await File.ReadAllTextAsync(path) 
                    },
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxPipelineTasks }
                );
            generateTests = new TransformBlock<FileContent, FileContent>
                (
                    async fileContent => new FileContent 
                    { 
                        filename = fileContent.filename,
                        content = await Task.Run( () => generator.GenerateTests(fileContent.content) ) 
                    },
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxPipelineTasks }
                );
            writeFile = new ActionBlock<FileContent>
                (
                    async fileContent => await File.WriteAllTextAsync(fileContent.filename, fileContent.content),
                    new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxPipelineTasks }
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

using TestsGeneratorLibrary;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using System.Diagnostics;

namespace TestsGeneratorTests
{
    public class TestsGeneratorTests
    {

        const string sourceCode = @"
            using System;
            using System.Collections.Generic;
            using System.Text;

            namespace CustomNamespace1
            {
                
                public interface IFoo
                {

                }

                public class Custom1
                {
                    public void Method1() 
                    {

                    }
                    
                    public int Method2(int arg)
                    {
                        return 42;
                    }
                    
                    public Custom1(int a, string b, IFoo c) 
                    {
                        
                    }
                }
            }

            namespace CustomNamespace2
            {
                public class Custom2
                {
                    public string Method1() 
                    {
                        return null;
                    }
                    
                    public void Method2(int arg, char b)
                    {

                    }
                }
            }";

        [Test]
        public void CheckFileCount()
        {
            int actual = TestsGenerator.GenerateTests(sourceCode).Result.Length;
            int expected = 2;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void CheckUsings()
        {
            TestUnit[] actualUnits = TestsGenerator.GenerateTests(sourceCode).Result;
            
            string[] actual1 = CSharpSyntaxTree.ParseText(actualUnits[0].sourceCode).
                                                GetRoot().
                                                DescendantNodes().OfType<UsingDirectiveSyntax>().
                                                Select(use => use.Name.ToString()).
                                                ToArray();
            string[] actual2 = CSharpSyntaxTree.ParseText(actualUnits[1].sourceCode).
                                                GetRoot().
                                                DescendantNodes().OfType<UsingDirectiveSyntax>().
                                                Select(use => use.Name.ToString()).
                                                ToArray();

            string[] expected1 = { "CustomNamespace1", "System", "System.Collections.Generic", "System.Text" };
            string[] expected2 = { "CustomNamespace2", "System", "System.Collections.Generic", "System.Text" };

            Assert.AreEqual(expected1[0], actual1[0]);
            Assert.AreEqual(expected1[1], actual1[1]);
            Assert.AreEqual(expected1[2], actual1[2]);
            Assert.AreEqual(expected1[3], actual1[3]);

            Assert.AreEqual(expected2[0], actual2[0]);
            Assert.AreEqual(expected2[1], actual2[1]);
            Assert.AreEqual(expected2[2], actual2[2]);
            Assert.AreEqual(expected2[3], actual2[3]);
        }
        [Test]
        public void CheckNamespaceGeneration()
        {
            TestUnit[] actualUnits = TestsGenerator.GenerateTests(sourceCode).Result;

            string[] actual1 = CSharpSyntaxTree.ParseText(actualUnits[0].sourceCode).
                                                GetRoot().
                                                DescendantNodes().OfType<NamespaceDeclarationSyntax>().
                                                Select(space => space.Name.ToString()).
                                                ToArray();
            string[] actual2 = CSharpSyntaxTree.ParseText(actualUnits[1].sourceCode).
                                                GetRoot().
                                                DescendantNodes().OfType<NamespaceDeclarationSyntax>().
                                                Select(space => space.Name.ToString()).
                                                ToArray();

            string[] expected1 = { "Custom1UnitTests"};
            string[] expected2 = { "Custom2UnitTests" };

            Assert.AreEqual(expected1[0], actual1[0]);
            Assert.AreEqual(expected2[0], actual2[0]);
        }
        [Test]
        public void CheckClassGeneration()
        {
            TestUnit[] actualUnits = TestsGenerator.GenerateTests(sourceCode).Result;

            string actual1 = CSharpSyntaxTree.ParseText(actualUnits[0].sourceCode).
                                                GetRoot().
                                                DescendantNodes().OfType<ClassDeclarationSyntax>().
                                                First().Identifier.ValueText;
            string actual2 = CSharpSyntaxTree.ParseText(actualUnits[1].sourceCode).
                                                GetRoot().
                                                DescendantNodes().OfType<ClassDeclarationSyntax>().
                                                First().Identifier.ValueText;

            string expected1 = "Custom1Tests";
            string expected2 = "Custom2Tests";

            Assert.AreEqual(expected1, actual1);
            Assert.AreEqual(expected2, actual2);
        }
        [Test]
        public void CheckPrivateFieldsGeneration()
        {
            TestUnit[] actualUnits = TestsGenerator.GenerateTests(sourceCode).Result;

            string[] actual1 = CSharpSyntaxTree.ParseText(actualUnits[0].sourceCode).
                                                GetRoot().
                                                DescendantNodes().OfType<FieldDeclarationSyntax>().
                                                Select(f => f.Declaration.Variables[0].Identifier.ValueText).
                                                ToArray();
            string[] actual2 = CSharpSyntaxTree.ParseText(actualUnits[1].sourceCode).
                                                GetRoot().
                                                DescendantNodes().OfType<FieldDeclarationSyntax>().
                                                Select(f => f.Declaration.Variables[0].Identifier.ValueText).
                                                ToArray();

            string[] expected1 = { "_Custom1Instance", "_cDependency" };
            string[] expected2 = { "_Custom2Instance" };

            Assert.AreEqual(expected1[0], actual1[0]);
            Assert.AreEqual(expected1[1], actual1[1]);

            Assert.AreEqual(expected2[0], actual2[0]);
        }
        [Test]
        public void CheckMethodsGeneration()
        {
            TestUnit[] actualUnits = TestsGenerator.GenerateTests(sourceCode).Result;

            int[] actual1 = CSharpSyntaxTree.ParseText(actualUnits[0].sourceCode).
                                                GetRoot().
                                                DescendantNodes().OfType<MethodDeclarationSyntax>().
                                                Select(m => m.Body.Statements.Count).
                                                ToArray();
            int[] actual2 = CSharpSyntaxTree.ParseText(actualUnits[1].sourceCode).
                                                GetRoot().
                                                DescendantNodes().OfType<MethodDeclarationSyntax>().
                                                Select(m => m.Body.Statements.Count).
                                                ToArray();

            int[] expected1 = { 4,2,5 };
            int[] expected2 = { 1,4,4 };

            Assert.AreEqual(expected1[0], actual1[0]);
            Assert.AreEqual(expected1[1], actual1[1]);
            Assert.AreEqual(expected1[2], actual1[2]);

            Assert.AreEqual(expected2[0], actual2[0]);
            Assert.AreEqual(expected2[1], actual2[1]);
            Assert.AreEqual(expected2[2], actual2[2]);
        }
    }
}
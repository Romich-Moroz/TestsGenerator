﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Collections.Generic;

namespace TestsGeneratorLibrary
{
    public class TestsGenerator
    {
        private SyntaxNode root;        

        private readonly SyntaxToken publicModifier = SyntaxFactory.Token(SyntaxKind.PublicKeyword);
        private readonly TypeSyntax voidReturnType = SyntaxFactory.ParseTypeName("void");
        private readonly AttributeSyntax setupAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("SetUp"));
        private readonly AttributeSyntax testAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("Test"));

        private UsingDirectiveSyntax[] GenerateTestingUsings()
        {
            return root.DescendantNodes().OfType<UsingDirectiveSyntax>().
                   Append(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("NUnit.Framework"))).
                   Append(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Moq"))).
                   ToArray();            
        }
        private NamespaceDeclarationSyntax GenerateTestingNamespace()
        {
            return SyntaxFactory.NamespaceDeclaration
                (
                    SyntaxFactory.ParseName
                    (
                        root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().
                        First().Name + ".Tests"
                    )
                ).
                AddMembers(GenerateTestingClass());
        }

        private ClassDeclarationSyntax GenerateTestingClass()
        {
            return SyntaxFactory.ClassDeclaration("Tests").
                AddModifiers(publicModifier).
                AddMembers(GenerateTestingMethods());
        }

        private MethodDeclarationSyntax[] GenerateTestingMethods()
        {
            return root.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(method => method.Modifiers.Any(SyntaxKind.PublicKeyword)).
                Select(publicMethod => SyntaxFactory.MethodDeclaration
                (
                    voidReturnType, 
                    publicMethod.Identifier.ValueText + "Test"
                ).
                AddModifiers(publicModifier).
                AddAttributeLists(SyntaxFactory.AttributeList
                (
                    SyntaxFactory.AttributeList().Attributes.Add(testAttribute))
                ).
                AddBodyStatements(GenerateTestingMethodStatements())).
                ToArray();
        }

        private StatementSyntax[] GenerateTestingMethodStatements()
        {
            return new StatementSyntax[] { SyntaxFactory.ParseStatement("Assert.Fail(\"autogenerated\");") };
        }

        private CompilationUnitSyntax GenerateTestUnit()
        {
            CompilationUnitSyntax testUnit = SyntaxFactory.CompilationUnit();
            testUnit = testUnit.AddUsings(GenerateTestingUsings());
            return testUnit.AddMembers(GenerateTestingNamespace());
        }


        public string GenerateTests(string sourceCode)
        {
            root = CSharpSyntaxTree.ParseText(sourceCode).GetRoot();

            return GenerateTestUnit().NormalizeWhitespace().ToFullString();
        }
   
    }
}

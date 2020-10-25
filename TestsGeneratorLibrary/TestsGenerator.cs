using Microsoft.CodeAnalysis;
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
        private readonly SyntaxToken privateModifier = SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
        private readonly TypeSyntax voidReturnType = SyntaxFactory.ParseTypeName("void");
        private readonly AttributeSyntax setupAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("SetUp"));
        private readonly AttributeSyntax testAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("Test"));

        private UsingDirectiveSyntax[] GenerateUsings()
        {
            return root.DescendantNodes().OfType<UsingDirectiveSyntax>().
                   Prepend(SyntaxFactory.UsingDirective(root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First().Name)).
                   Append(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("NUnit.Framework"))).
                   Append(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Moq"))).
                   ToArray();            
        }
        private NamespaceDeclarationSyntax GenerateNamespace(ClassDeclarationSyntax context)
        {
            return SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(context.Identifier.ValueText + ".Tests")).
                   AddMembers(GenerateClass(context));
        }

        private ClassDeclarationSyntax GenerateClass(ClassDeclarationSyntax context)
        {
            return SyntaxFactory.ClassDeclaration(context.Identifier.ValueText + "Tests").
                AddModifiers(publicModifier).
                AddMembers(GenerateFields(context)).
                AddMembers(GenerateMethods());
        }

        private string GetPrivateDependencyName(string name)
        {
            return string.Format("_{0}Dependency", name);
        }

        private string GetPrivateClassName(string name)
        {
            return string.Format("_{0}Instance", name);
        }

        private FieldDeclarationSyntax[] GenerateFields(ClassDeclarationSyntax context)
        {
            List<FieldDeclarationSyntax> privateFields = new List<FieldDeclarationSyntax>();
            privateFields.Add(SyntaxFactory.FieldDeclaration
            (
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList().Add(privateModifier),
                SyntaxFactory.VariableDeclaration
                (
                    SyntaxFactory.ParseTypeName(context.Identifier.ValueText),
                    SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>().
                    Add(SyntaxFactory.VariableDeclarator(GetPrivateClassName(context.Identifier.ValueText)))
                )
            ));
            FieldDeclarationSyntax[] dependencies = context.Members.
                OfType<ConstructorDeclarationSyntax>().
                Where(ctor => ctor.Modifiers.
                Any(SyntaxKind.PublicKeyword))?.
                FirstOrDefault()?.ParameterList?.Parameters.
                Where(parameter => parameter.Type.ToString()[0] == 'I').
                Select(parameter => SyntaxFactory.FieldDeclaration
                (
                    SyntaxFactory.List<AttributeListSyntax>(),
                    SyntaxFactory.TokenList().Add(privateModifier),
                    SyntaxFactory.VariableDeclaration
                    (
                        SyntaxFactory.ParseTypeName("Mock<" + parameter?.Type.ToString() + ">"),
                        SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>().
                        Add(SyntaxFactory.VariableDeclarator(GetPrivateDependencyName(parameter?.Type.ToString())))
                    )
                ))?.
                ToArray();
            if (dependencies != null)
                privateFields.AddRange(dependencies);           
            return privateFields.ToArray();
        }

        private MethodDeclarationSyntax[] GenerateMethods()
        {
            return root.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(method => method.Modifiers.Any(SyntaxKind.PublicKeyword)).
                Select(publicMethod => SyntaxFactory.MethodDeclaration
                (
                    voidReturnType, 
                    publicMethod.Identifier.ValueText + "Test"
                ).
                AddModifiers(publicModifier).
                AddAttributeLists
                (
                    SyntaxFactory.AttributeList
                    (
                        SyntaxFactory.AttributeList().Attributes.Add(testAttribute)
                    )
                ).
                AddBodyStatements(GenerateMethodStatements())).
                Prepend
                (
                    SyntaxFactory.MethodDeclaration
                    (
                        voidReturnType,
                        "Setup"
                    ).
                    AddModifiers(publicModifier).
                    AddAttributeLists
                    (
                        SyntaxFactory.AttributeList
                        (
                            SyntaxFactory.AttributeList().Attributes.Add(setupAttribute)
                        )
                    ).
                    AddBodyStatements(GenerateSetupStatements())                   
                ).                
                ToArray();
        }

        private StatementSyntax[] GenerateSetupStatements()
        {
            return new StatementSyntax[] { SyntaxFactory.ParseStatement("") };
        }

        private StatementSyntax[] GenerateMethodStatements()
        {
            return new StatementSyntax[] { SyntaxFactory.ParseStatement("Assert.Fail(\"autogenerated\");") };
        }

        private CompilationUnitSyntax[] GenerateTestUnits()
        {
            return root.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Select(classDecl => SyntaxFactory.CompilationUnit().
                AddUsings(GenerateUsings()).
                AddMembers(GenerateNamespace(classDecl))).
                ToArray();
        }

        public struct FileContent
        {
            public string filename;
            public string content;
        }

        public FileContent[] GenerateTests(string sourceCode)
        {
            root = CSharpSyntaxTree.ParseText(sourceCode).GetRoot();

            return GenerateTestUnits().
                   Select(unit => new FileContent 
                   { 
                       filename = unit.DescendantNodes().OfType<ClassDeclarationSyntax>().First().Identifier.ValueText,
                       content = unit.NormalizeWhitespace().ToFullString() 
                   }).
                   ToArray();
        }
   
    }
}

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

            ClassDeclarationSyntax generatedClass = SyntaxFactory.ClassDeclaration(context.Identifier.ValueText + "Tests").
                                                                  AddModifiers(publicModifier);
            FieldDeclarationSyntax[] privateFields = GenerateFields(context);

            return generatedClass.AddMembers(privateFields).
                                  AddMembers(GenerateMethods(context, privateFields));
        }

        private string GetPrivateDependencyName(string name)
        {
            return string.Format("_{0}Dependency", name);
        }

        private string GetPrivateClassName(string name)
        {
            return string.Format("_{0}Instance", name);
        }

        private string GetMockTypeName(string name)
        {
            return string.Format("Mock<{0}>", name);
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
                        SyntaxFactory.ParseTypeName(GetMockTypeName(parameter?.Type.ToString())),
                        SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>().
                        Add(SyntaxFactory.VariableDeclarator(GetPrivateDependencyName(parameter?.Identifier.ValueText)))
                    )
                ))?.
                ToArray();
            if (dependencies != null)
                privateFields.AddRange(dependencies);           
            return privateFields.ToArray();
        }

        private MethodDeclarationSyntax[] GenerateMethods(ClassDeclarationSyntax classContext, FieldDeclarationSyntax[] fieldContext)
        {
            return classContext.Members.OfType<MethodDeclarationSyntax>().
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
                AddBodyStatements(GenerateMethodStatements(publicMethod,fieldContext))).
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
                    AddBodyStatements(GenerateSetupStatements(classContext,fieldContext))                   
                ).                
                ToArray();
        }

        private StatementSyntax[] GenerateSetupStatements(ClassDeclarationSyntax classContext, FieldDeclarationSyntax[] fieldContext)
        {
            List<StatementSyntax> statements = new List<StatementSyntax>();
            string ctorArgs = "";
            if (fieldContext.Length > 1)
            {
                foreach(ParameterSyntax parameter in classContext.Members.OfType<ConstructorDeclarationSyntax>().
                                                                          Where(ctor => ctor.Modifiers.
                                                                          Any(SyntaxKind.PublicKeyword))?.
                                                                          FirstOrDefault()?.
                                                                          ParameterList?.
                                                                          Parameters)
                {
                    string identifier;
                    if (parameter.Type.ToString()[0] != 'I')
                    {
                        identifier = parameter.Identifier.ValueText;
                        statements.Add(SyntaxFactory.ParseStatement
                            (
                                string.Format
                                (
                                    "{0} {1} = {2};",
                                    parameter.Type.ToString(),
                                    identifier,
                                    "default"
                                )
                            ));
                    }         
                    else
                    {
                        identifier = GetPrivateDependencyName(parameter.Identifier.ValueText);
                        statements.Add(SyntaxFactory.ParseStatement
                            (
                                string.Format
                                (
                                    "{0} = new {1}();",
                                    identifier,
                                    GetMockTypeName(parameter.Type.ToString())
                                )
                            ));
                    }
                    ctorArgs += identifier + ',';
                }
                if (ctorArgs.Length != 0)
                    ctorArgs = ctorArgs.Remove(ctorArgs.Length - 1, 1);
            }            
            statements.Add(SyntaxFactory.ParseStatement
                (
                    string.Format("{0} = new {1}({2});",
                    fieldContext[0].Declaration.Variables[0].Identifier.ValueText,
                    fieldContext[0].Declaration.Type.ToString(),ctorArgs)
                ));
            return statements.ToArray();       
        }

        private StatementSyntax[] GenerateMethodStatements(MethodDeclarationSyntax methodContext, FieldDeclarationSyntax[] fieldContext)
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

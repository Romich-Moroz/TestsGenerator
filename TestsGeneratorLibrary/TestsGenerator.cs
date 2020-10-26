using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestsGeneratorLibrary
{
    public static class TestsGenerator
    {
        private static readonly SyntaxToken publicModifier = SyntaxFactory.Token(SyntaxKind.PublicKeyword);
        private static readonly SyntaxToken privateModifier = SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
        private static readonly TypeSyntax voidReturnType = SyntaxFactory.ParseTypeName("void");
        private static readonly AttributeSyntax setupAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("SetUp"));
        private static readonly AttributeSyntax testAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("Test"));

        private static UsingDirectiveSyntax[] GenerateUsings(SyntaxNode root)
        {
            return root.DescendantNodes().OfType<UsingDirectiveSyntax>().
                   Prepend(SyntaxFactory.UsingDirective(root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First().Name)).
                   Append(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("NUnit.Framework"))).
                   Append(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Moq"))).
                   ToArray();            
        }
        private static NamespaceDeclarationSyntax GenerateNamespace(ClassDeclarationSyntax context)
        {
            return SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(context.Identifier.ValueText + "Tests")).
                   AddMembers(GenerateClass(context));
        }

        private static ClassDeclarationSyntax GenerateClass(ClassDeclarationSyntax context)
        {

            ClassDeclarationSyntax generatedClass = SyntaxFactory.ClassDeclaration(context.Identifier.ValueText + "Tests").
                                                                  AddModifiers(publicModifier);
            FieldDeclarationSyntax[] privateFields = GenerateFields(context);

            return generatedClass.AddMembers(privateFields).
                                  AddMembers(GenerateMethods(context, privateFields));
        }

        private static string GetPrivateDependencyName(string name)
        {
            return string.Format("_{0}Dependency", name);
        }

        private static string GetPrivateClassName(string name)
        {
            return string.Format("_{0}Instance", name);
        }

        private static string GetMockTypeName(string name)
        {
            return string.Format("Mock<{0}>", name);
        }

        private static FieldDeclarationSyntax GenerateFieldFromTemplate(SyntaxToken modifier, string typename, string identifier)
        {
            return SyntaxFactory.FieldDeclaration
                (
                    SyntaxFactory.List<AttributeListSyntax>(),
                    SyntaxFactory.TokenList().Add(modifier),
                    SyntaxFactory.VariableDeclaration
                    (
                        SyntaxFactory.ParseTypeName(typename),
                        SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>().
                        Add(SyntaxFactory.VariableDeclarator(identifier))
                    )
                );
        }

        private static FieldDeclarationSyntax[] GenerateFields(ClassDeclarationSyntax context)
        { 
            List<FieldDeclarationSyntax> privateFields = new List<FieldDeclarationSyntax>();
            if (!context.Modifiers.Any(SyntaxKind.StaticKeyword))
                privateFields.Add(GenerateFieldFromTemplate(privateModifier, context.Identifier.ValueText, GetPrivateClassName(context.Identifier.ValueText)));

            FieldDeclarationSyntax[] dependencies = context.Members.
                OfType<ConstructorDeclarationSyntax>().
                Where(ctor => ctor.Modifiers.
                Any(SyntaxKind.PublicKeyword))?.
                FirstOrDefault()?.ParameterList?.Parameters.
                Where(parameter => parameter.Type.ToString()[0] == 'I').
                Select(parameter => GenerateFieldFromTemplate
                (
                    privateModifier,
                    GetMockTypeName(parameter?.Type.ToString()),
                    GetPrivateDependencyName(parameter?.Identifier.ValueText)
                ))?.
                ToArray();               
            if (dependencies != null)
                privateFields.AddRange(dependencies);           
            return privateFields.ToArray();
        }

        private static MethodDeclarationSyntax GenerateMethodFromTemplate(AttributeSyntax attribute, SyntaxToken modifier, TypeSyntax returnType, string identifier, StatementSyntax[] statements)
        {
            return SyntaxFactory.MethodDeclaration(returnType, identifier).
                    AddModifiers(modifier).
                    AddAttributeLists
                    (
                        SyntaxFactory.AttributeList(SyntaxFactory.AttributeList().Attributes.Add(attribute))
                    ).
                    AddBodyStatements(statements);
        }

        private static MethodDeclarationSyntax[] GenerateMethods(ClassDeclarationSyntax classContext, FieldDeclarationSyntax[] fieldContext)
        {
            return classContext.Members.OfType<MethodDeclarationSyntax>().
                Where(method => method.Modifiers.Any(SyntaxKind.PublicKeyword)).
                Select(publicMethod => GenerateMethodFromTemplate
                (
                    testAttribute,
                    publicModifier,
                    voidReturnType,
                    publicMethod.Identifier.ValueText + "UnitTest",
                    GenerateMethodStatements(classContext, publicMethod, fieldContext)
                )).
                Prepend(GenerateMethodFromTemplate
                (
                    setupAttribute,
                    publicModifier,
                    voidReturnType,
                    "Setup",
                    GenerateSetupStatements(classContext, fieldContext)
                )).
                ToArray();
        }

        private static StatementSyntax GenerateAssignStatementFromTemplate(string typename, string varname, bool newKeyword, string variable, string invokeArgs = "")
        {
            return SyntaxFactory.ParseStatement(string.Format
                                (
                                    "{0} {1} = {2} {3}{4};",
                                    typename,
                                    varname,
                                    newKeyword ? "new" : "",
                                    variable,
                                    newKeyword ? string.Format("({0})",invokeArgs) : ""
                                ));
        }

        private static StatementSyntax[] GenerateSetupStatements(ClassDeclarationSyntax classContext, FieldDeclarationSyntax[] fieldContext)
        {
            List<StatementSyntax> statements = new List<StatementSyntax>();
            string ctorArgs = "";
            if (!classContext.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                foreach (ParameterSyntax parameter in classContext.Members.OfType<ConstructorDeclarationSyntax>().
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
                        statements.Add(GenerateAssignStatementFromTemplate
                            (
                                parameter.Type.ToString(),
                                identifier,
                                false,
                                parameter.Type.ToString() == "string" ? "\"\"" : "default"
                            ));
                    }
                    else
                    {
                        identifier = GetPrivateDependencyName(parameter.Identifier.ValueText) + ".Object";
                        statements.Add(GenerateAssignStatementFromTemplate
                            (
                                "",
                                GetPrivateDependencyName(parameter.Identifier.ValueText),
                                true,
                                GetMockTypeName(parameter.Type.ToString())
                            ));
                    }
                    ctorArgs += identifier + ',';
                }
                if (ctorArgs.Length != 0)
                    ctorArgs = ctorArgs.Remove(ctorArgs.Length - 1, 1);

                statements.Add(GenerateAssignStatementFromTemplate
                    (
                        "",
                        fieldContext[0].Declaration.Variables[0].Identifier.ValueText,
                        true,
                        fieldContext[0].Declaration.Type.ToString(),
                        ctorArgs
                    ));
            }
            
            return statements.ToArray();       
        }

        private static StatementSyntax GenerateCallStatementFromTemplate(bool returnsVoid, string typename, string varname, string objectname, string methodname, string args="")
        {
            return SyntaxFactory.ParseStatement
                    (
                        string.Format
                        (
                            "{0} {1}.{2}({3});",
                            !returnsVoid ? string.Format("{0} {1} = ",typename,varname) : "",
                            objectname,
                            methodname,
                            args
                        )
                    );
        }

        private static StatementSyntax[] GenerateMethodStatements(ClassDeclarationSyntax classContext, MethodDeclarationSyntax methodContext, FieldDeclarationSyntax[] fieldContext)
        {
            List<StatementSyntax> statements = new List<StatementSyntax>();

            string actMethodArguments = "";
            foreach (ParameterSyntax parameter in methodContext.ParameterList.Parameters)
            {
                if (parameter.Type.ToString()[0] != 'I')
                {
                    statements.Add(GenerateAssignStatementFromTemplate
                        (
                            parameter.Type.ToString(),
                            parameter.Identifier.ValueText,
                            false,
                            parameter.Type.ToString() == "string" ? "\"\"" : "default"
                        ));
                    actMethodArguments += parameter.Identifier.ValueText + ',';
                }
                else
                {
                    actMethodArguments += GetPrivateDependencyName(parameter.Identifier.ValueText) + ".Object,";
                }
            }

            if (actMethodArguments.Length != 0)
                actMethodArguments = actMethodArguments.Remove(actMethodArguments.Length - 1, 1);

            

            if (methodContext.ReturnType.ToString() != "void")
            {
                if (!methodContext.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    statements.Add(GenerateCallStatementFromTemplate
                    (
                        false,
                        methodContext.ReturnType.ToString(),
                        "actual",
                        fieldContext[0].Declaration.Variables[0].Identifier.ValueText,
                        methodContext.Identifier.ValueText,
                        actMethodArguments
                    ));
                }
                else
                {
                    statements.Add(GenerateCallStatementFromTemplate
                    (
                        false,
                        methodContext.ReturnType.ToString(),
                        "actual",
                        classContext.Identifier.ValueText,
                        methodContext.Identifier.ValueText,
                        actMethodArguments
                    ));
                }

                statements.Add(GenerateAssignStatementFromTemplate
                    (
                        methodContext.ReturnType.ToString(),
                        "expected",
                        false,
                        methodContext.ReturnType.ToString() == "string" ? "\"\"" : "default"
                    ));
                statements.Add(SyntaxFactory.ParseStatement("Assert.That(actual,Is.EqualTo(expected));"));
            }               
            else
            {
                if (!methodContext.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    statements.Add(GenerateCallStatementFromTemplate
                    (
                        true,
                        "",
                        "",
                        fieldContext[0].Declaration.Variables[0].Identifier.ValueText,
                        methodContext.Identifier.ValueText,
                        actMethodArguments
                    ));
                }
                else
                {
                    statements.Add(GenerateCallStatementFromTemplate
                    (
                        true,
                        "",
                        "",
                        classContext.Identifier.ValueText,
                        methodContext.Identifier.ValueText,
                        actMethodArguments
                    ));
                }
                
            }
                
            statements.Add(SyntaxFactory.ParseStatement("Assert.Fail(\"autogenerated\");"));
            return statements.ToArray();
        }

        private static CompilationUnitSyntax[] GenerateTestUnits(SyntaxNode root)
        {
            return root.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Select(classDecl => SyntaxFactory.CompilationUnit().
                AddUsings(GenerateUsings(root)).
                AddMembers(GenerateNamespace(classDecl))).
                ToArray();
        }


        public static Task<TestUnit[]> GenerateTests(string sourceCode)
        {

            return Task.Run(() => GenerateTestUnits(CSharpSyntaxTree.ParseText(sourceCode).GetRoot()).
                        Select(unit => new TestUnit
                        (
                            unit.DescendantNodes().OfType<ClassDeclarationSyntax>().First().Identifier.ValueText,
                            unit.NormalizeWhitespace().ToFullString()
                        )).
                        ToArray());
        }
   
    }
}

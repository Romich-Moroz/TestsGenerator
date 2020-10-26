using System;
using System.Collections.Generic;
using System.Text;

namespace CustomNamespace
{
    public class Custom
    {

    }
}

namespace TestsGeneratorLibrary
{

    public interface IFoo
    {

    }

    public class Foo : IFoo
    {

        public static class StaticFoo
        {
            static int a;
            static StaticFoo()
            {
                a = 5;
            }

            public static void Bar()
            {

            }
        }

        public static int Bar()
        {
            return 42;
        }

        public Foo(int a)
        {

        }

        public char FooBar(int a)
        {
            return 'c';
        }

    }
    public class TestPurposeClass
    {
        private int a;
        private char b;
        private string d;
        private IFoo c;


        


        public int NoFoo(IFoo c, int asd, char dms, string vbn)
        {
            return 42;
        }
        public void voidMethodNoArgs()
        {
            return;
        }
        public void voidMethodArgs(int a, IFoo c)
        {
            return;
        }

        public string GetString()
        {
            return "asd";
        }

        public TestPurposeClass(int a, char b, string d, IFoo c)
        {
            this.a = a;
            this.b = b;
            this.d = d;
            this.c = c;

        }
    }
}

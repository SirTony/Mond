﻿using System;
using Mond.Binding;
using NUnit.Framework;

namespace Mond.Tests.Binding
{
    [TestFixture]
    public partial class FunctionOverloadingTests
    {
        private MondState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new MondState
            {
                Options =
                {
                    DebugInfo = MondDebugInfoLevel.Full,
                },
                Libraries =
                {
                    new OverloadedModule.Library(),
                },
            };
        }

        [Test]
        public void Resolution()
        {
            Assert.True(_state.Run("return Ov.test();") == 0, "0.0");

            Assert.True(_state.Run("return Ov.test(true);") == 0, "0.1");
            Assert.True(_state.Run("return Ov.test(false);") == 0, "0.2");

            Assert.True(_state.Run("return Ov.test('hi');") == 1, "1.0");

            Assert.True(_state.Run("return Ov.test(10);") == 2, "2.0");
            Assert.True(_state.Run("return Ov.test(10, 20);") == 2, "2.1");

            Assert.True(_state.Run("return Ov.test(undefined, 20);") == 3, "3.0");
            Assert.True(_state.Run("return Ov.test(null, 20);") == 3, "3.1");
            Assert.True(_state.Run("return Ov.test([], 20);") == 3, "3.2");

            Assert.True(_state.Run("return Ov.test('hi', 20);") == 4, "4.0");
            Assert.True(_state.Run("return Ov.test('hi', 20, 30);") == 4, "4.1");

            Assert.True(_state.Run("return Ov.test(10, 20, 30);") == 5, "5.0");
            Assert.True(_state.Run("return Ov.test(10, 20, 30, 40);") == 5, "5.1");

            Assert.True(_state.Run("return Ov.test([], 'hi');") == 6, "6.0");
            Assert.True(_state.Run("return Ov.test('hi', 'hi');") == 6, "6.1");
            Assert.True(_state.Run("return Ov.test('hi', 20, 'hi');") == 6, "6.2");
        }

        [MondModule("Ov")]
        public static partial class OverloadedModule
        {
            [MondFunction]
            public static int Test(bool x = true)
            {
                return 0;
            }

            [MondFunction]
            public static int Test(string x)
            {
                return 1;
            }

            [MondFunction]
            public static int Test(int x, int y = 1)
            {
                return 2;
            }

            [MondFunction]
            public static int Test(MondValue x, int y)
            {
                return 3;
            }

            [MondFunction]
            public static int Test(string x, int y, int z = 2)
            {
                return 4;
            }

            [MondFunction]
            public static int Test(int x, int y, params Span<MondValue> args)
            {
                return 5;
            }

            [MondFunction]
            public static int Test(params Span<MondValue> args)
            {
                return 6;
            }
        }

#if false
        // This should generate a compiler error
        [MondModule]
        public static partial class HiddenOverloadsModule
        {
            [MondFunction]
            public static int Add(int x, int y)
            {
                return x + y;
            }

            [MondFunction]
            public static double Add(double x, double y)
            {
                return x + y;
            }
        }
#endif
    }
}

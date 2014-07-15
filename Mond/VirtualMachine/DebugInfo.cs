﻿using System;
using System.Collections.Generic;

namespace Mond.VirtualMachine
{
    class DebugInfo
    {
        public struct Function
        {
            public readonly int Address;
            public readonly int FileName;
            public readonly int Name;

            public Function(int address, int fileName, int name)
            {
                Address = address;
                FileName = fileName;
                Name = name;
            }
        }

        public struct Line
        {
            public readonly int Address;
            public readonly int FileName;
            public readonly int LineNumber;

            public Line(int address, int fileName, int lineNumber)
            {
                Address = address;
                FileName = fileName;
                LineNumber = lineNumber;
            }
        }

        private readonly List<Function> _functions;
        private readonly List<Line> _lines;

        public DebugInfo(List<Function> functions, List<Line> lines)
        {
            _functions = functions;
            _lines = lines;
        }

        public Function? FindFunction(int address)
        {
            var idx = Search(_functions, new Function(address, 0, 0), FunctionAddressComparer);
            Function? result = null;

            if (idx >= 0 && idx < _functions.Count)
                result = _functions[idx];

            return result;
        }

        public Line? FindLine(int address)
        {
            var idx = Search(_lines, new Line(address, 0, 0), LineAddressComparer);
            Line? result = null;

            if (idx >= 0 && idx < _lines.Count)
                result = _lines[idx];

            return result;
        }

        private static int Search<T>(List<T> list, T key, IComparer<T> comparer)
        {
            var idx = list.BinarySearch(key, comparer);

            if (idx < 0)
                idx = ~idx - 1;

            return idx;
        }

        private static readonly GenericComparer<Function> FunctionAddressComparer =
            new GenericComparer<Function>((x, y) => x.Address - y.Address);

        private static readonly GenericComparer<Line> LineAddressComparer =
            new GenericComparer<Line>((x, y) => x.Address - y.Address);
    }

    class GenericComparer<T> : IComparer<T>
    {
        private Func<T, T, int> _comparer;

        public GenericComparer(Func<T, T, int> comparer)
        {
            _comparer = comparer;
        }

        public int Compare(T x, T y)
        {
            return _comparer(x, y);
        }
    }
}
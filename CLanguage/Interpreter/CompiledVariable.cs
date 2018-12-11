﻿using System;
using CLanguage.Types;

namespace CLanguage.Interpreter
{
    public class CompiledVariable
    {
        public string Name { get; }
        public CType VariableType { get; }

        public int Offset { get; set; }
        public Value[] InitialValue { get; set; }

        public CompiledVariable (string name, int offset, CType type)
        {
            Name = name ?? throw new ArgumentNullException (nameof (name));
            Offset = offset;
            VariableType = type ?? throw new ArgumentNullException (nameof (type));
        }

        public override string ToString ()
        {
            return string.Format ("{0}: {1}", Name, VariableType);
        }
    }
}

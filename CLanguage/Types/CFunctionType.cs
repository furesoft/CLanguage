﻿using System;
using System.Collections.Generic;
using CLanguage.Interpreter;

namespace CLanguage.Types
{
    public class CFunctionType : CType
    {
        public static readonly CFunctionType VoidProcedure = new CFunctionType (CType.Void, isInstance: false);

        public class Parameter
        {
            public string Name { get; set; }
            public CType ParameterType { get; set; }
            public Parameter(string name, CType parameterType)
            {
                Name = name;
                ParameterType = parameterType;
            }
            public override string ToString()
            {
                return ParameterType + " " + Name;
            }
        }

        public CType ReturnType { get; private set; }
        public List<Parameter> Parameters { get; private set; }
        public bool IsInstance { get; private set; }

        public CFunctionType(CType returnType, bool isInstance)
        {
            ReturnType = returnType;
            Parameters = new List<Parameter>();
            IsInstance = isInstance;
        }

        public override int GetSize(EmitContext c)
        {
            return c.MachineInfo.PointerSize;
        }

        public override string ToString()
        {
            var s = "(Function " + ReturnType + " (";
            var head = "";
            foreach (var p in Parameters)
            {
                s += head;
                s += p;
                head = " ";
            }
            s += "))";
            return s;
        }

        public bool ParameterTypesMatchArgs (CType[] argTypes)
        {
            if (Parameters.Count != argTypes.Length)
                return false;

            for (var i = 0; i < Parameters.Count; i++) {
                var ft = argTypes[i];
                var tt = Parameters[i].ParameterType;

                if (!ft.CanCastTo (tt))
                    return false;
            }

            return true;
        }

        public bool ParameterTypesEqual (CFunctionType otherType)
        {
            if (Parameters.Count != otherType.Parameters.Count)
                return false;

            for (var i = 0; i < Parameters.Count; i++) {
                var ft = otherType.Parameters[i].ParameterType;
                var tt = Parameters[i].ParameterType;

                if (!ft.Equals (tt))
                    return false;
            }

            return true;
        }
    }


}

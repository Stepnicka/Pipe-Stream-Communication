using System;
using System.Reflection;

namespace DataSharingLibrary
{
    internal class RegisteredMethod
    {
        public RegisteredMethod(MethodInfo method, Type parameterType ,object owner)
        {
            MethodInfo = method;
            Owner = owner;
            ParameterType = parameterType;
        }

        public object Owner { get;  }

        public Type ParameterType { get;  }

        public MethodInfo MethodInfo { get;  }
    }
}

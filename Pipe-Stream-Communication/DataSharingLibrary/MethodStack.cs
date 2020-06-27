using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace DataSharingLibrary
{
    /// <summary>
    ///     Stack of methods that are called by external client
    /// </summary>
    public class MethodStack
    {
        ConcurrentDictionary<string, RegisteredMethod> _methods;

        public MethodStack()
        {
            _methods = new ConcurrentDictionary<string, RegisteredMethod>();
        }

        /// <summary>
        ///     Add method to stack
        /// </summary>
        public bool TryAddMethod<T,U,J>(Func<U,J> method) where T : PipeRequest<U>
        {
            var methodInfo = method.GetMethodInfo();
            var owner = method.Target;

            return _methods.TryAdd(methodInfo.Name, new RegisteredMethod(methodInfo, typeof(T) , owner));
        }

        /// <summary>
        ///     Run method in the stack
        /// </summary>
        public bool TryRunMethod(string methodName, object parameter, out object response )
        {
            response = null;

            if (_methods.TryGetValue(methodName, out RegisteredMethod method))
            {
                try
                {
                    response = method.MethodInfo.Invoke(method.Owner, new object[] { parameter });
                }
                catch (Exception)
                {
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        ///     Find method parameter
        /// </summary>
        public bool TryGetMethodParameterType(string methodName, out Type parameterType)
        {
            parameterType = null;

            if(_methods.TryGetValue(methodName, out RegisteredMethod method))
            {
                parameterType = method.ParameterType;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;
using ConfigTypes;

namespace Operator
{
    public class CustomOperator : OperatorImpl
    {
        /// <summary>
        /// save the dll name to be opened
        /// </summary>
        private string dll_;

        /// <summary>
        /// Save the class name to be used
        /// </summary>
        private string class_;

        /// <summary>
        /// save the method name to be invoked
        /// </summary>
        private string method_;

        public CustomOperator(OperatorSpec spec,string dll_d, string class_c, string method_m) : base(spec)
        {
            dll_ = dll_d;
            class_ = class_c;
            method_ = method_m;
        }
        public CustomOperator(string dll_d, string class_c, string method_m) : base()
        {
            dll_ = dll_d;
            class_ = class_c;
            method_ = method_m;
        }

        public override OperatorTuple Operation(OperatorTuple tuple)
        {

            Assembly assembly = Assembly.LoadFile(dll_);
            if (assembly != null)
            {

                Type type = assembly.GetType(class_);
                if (type != null)
                {

                    var methodInfo = type.GetMethod(method_, new Type[] { typeof(List<string>) });
                    if (methodInfo != null)
                    {
                        var o = Activator.CreateInstance(type);
                        object[] params_ = new object[] { tuple.Tuple };
                        var result = methodInfo.Invoke(o, params_);

                        /* maybe to heavy or expensive ? */
                        List<string> res = new List<string>(((IEnumerable)result).Cast<object>()
                                         .Select(x => x.ToString())
                                         .ToArray());

                        return new OperatorTuple(res);
                    }
                }
            }

            return null;
        }

        public override void Status()
        {
            generalStatus();
            Console.WriteLine("Dll: " + dll_ + " | Class: " + class_ + " | Method: " + method_);
        }
    }
}

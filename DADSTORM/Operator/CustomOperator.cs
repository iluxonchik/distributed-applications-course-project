using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OperatorProxys;
using ConfigTypes;
using System.IO;

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

        public CustomOperator(OperatorSpec spec,string dll_d, string class_c, string method_m, string myAddr, int repId) : base(spec, myAddr, repId)
        {
            dll_ = Directory.GetCurrentDirectory() + "\\" + dll_d;
             class_ = class_c; // should be handled in parser to include the namespace FIX
            //class_ =  "LibCustomOperator." + class_c;
            method_ = method_m;
        }

        /* Test constructor ONLY */
        public CustomOperator(string dll_d, string class_c, string method_m) : base()
        {
            dll_ = dll_d;
            class_ = class_c;
            method_ = method_m;
        }

        public override List<OperatorTuple> Operation(OperatorTuple tuple)
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
                        //TODO: return List of tuples, cada tuple é uma lista de string
                        var result = methodInfo.Invoke(o, params_);

                        List<OperatorTuple> theRes = new List<OperatorTuple>();

                        foreach (List<string> t in ((IEnumerable)result))
                        {
                            theRes.Add(new OperatorTuple(t, tuple.Id, MyAddr));
                        }
                        /*
                        Console.Write("FOR tuple: ");
                        foreach (string a in tuple.Tuple)
                            Console.Write(a + " ");
                        Console.WriteLine("DLL returned:");
                        foreach(OperatorTuple op in theRes)
                            foreach (string a in op.Tuple
                                Console.Write(a + " ");
                            Console.WriteLine();
                         */

                        return theRes;
                    }
                    else
                        throw new NullReferenceException("No method " + method_ + "END");
                }
                else
                    throw new NullReferenceException("No type " + class_ + "END");
            }
            else
                throw new NullReferenceException("No assembly");

            //return  new List<OperatorTuple>();
        }

        public override void Status()
        {
            generalStatus();
            Console.WriteLine("Dll: " + dll_ + " | Class: " + class_ + " | Method: " + method_);
        }


        // FIX
        public void setDll(string s)
        {
            dll_ = s;
        }
    }
}

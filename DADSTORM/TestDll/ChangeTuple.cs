using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDll
{
    public class ChangeTuple
    {

        /// <summary>
        /// Simple method for test
        /// </summary>
        /// <returns>
        /// new List<String> with all the elements duplicated in the same order
        /// </returns>
        public List<String> DuplicateTuple(List<String> tuples)
        {
            List<string> aux = new List<string> (tuples);
             
            foreach (string tuple_slice in tuples)
            {
                aux.Add(tuple_slice);
            }
            return aux;
        }

        /// Add more methods if necessary for more tests
        /// 
    }
}

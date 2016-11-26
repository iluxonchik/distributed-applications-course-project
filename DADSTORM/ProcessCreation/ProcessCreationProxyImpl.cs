using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessCreationProxy;
using ConfigTypes;
using System.Diagnostics;
using System.IO;

namespace ProcessCreation
{
    public class ProcessCreationProxyImpl : MarshalByRefObject, IProcessCreationProxy
    {

        private FileInfo operatorExecFile;
        public ProcessCreationProxyImpl(FileInfo operatorExecFile)
        {
            this.operatorExecFile = operatorExecFile;
        }
        public void CreateOperator(OperatorSpec opSpec, string myAddr, int repId)
        {
            Console.WriteLine("CreateOperator called");
            Directory.SetCurrentDirectory(operatorExecFile.Directory.FullName);
           
            FileInfo opFile = new FileInfo(Directory.GetCurrentDirectory() + "/operator/" + opSpec.Id);

            if (!opFile.Exists)
            {
               if(!Directory.Exists(opFile.Directory.FullName))
                {
                    Directory.CreateDirectory(opFile.Directory.FullName);
                }
                var myFile = File.Create(opFile.FullName);
                myFile.Close(); 
               
            }

            WriteToBinaryFile<OperatorSpec>(opFile.FullName, opSpec);
            Process.Start(operatorExecFile.FullName, opFile.FullName + String.Format(" {0} {1}", myAddr, repId));
        }
        private static void WriteToBinaryFile<T>(string filePath, T opSpec)
        {
            //TODO: FIX remove short if from file open, since it is always
            using (Stream stream = File.Open(filePath, false ? FileMode.Append : FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, opSpec);
            }
        }

        public void Crash()
        {
            System.Environment.Exit(1);
        }
    }
}

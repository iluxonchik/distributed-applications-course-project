using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessCreationProxy;
using PuppetMaster;
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
        public void CreateOperator(OperatorSpec opSpec)
        {
            Console.WriteLine("CreateOperator colled");
            ProcessStartInfo start = new ProcessStartInfo(operatorExecFile.FullName);

            string opSpecFile = Directory.GetCurrentDirectory() + "/operator" + opSpec.Id;
            WriteToBinaryFile<OperatorSpec>(opSpecFile, opSpec);
            start.Arguments = opSpecFile;

            using (Process proc = Process.Start(start))
            {
                //on this point foward all possible comunication beteen should be by remote services,
                //so we can just clean all process references
            }
        }
        private static void WriteToBinaryFile<T>(string filePath, T opSpec)
        {
            using (Stream stream = File.Open(filePath, false ? FileMode.Append : FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, opSpec);
            }
        }
    }
}

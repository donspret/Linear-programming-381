using System;
using System.IO;
using System.Text;

namespace LPR381Proj.Output
{
    public class MultiWriter : TextWriter
    {
        private readonly TextWriter _consoleOut;
        private readonly StreamWriter _fileOut;

        public MultiWriter(string filePath)
        {
            _consoleOut = Console.Out;
            _fileOut = new StreamWriter(filePath, false, Encoding.UTF8);
            // Ensures automatic flushing whenever a line is written
            _fileOut.AutoFlush = true;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string value)
        {
            _consoleOut.WriteLine(value);
            _fileOut.WriteLine(value);
        }

        public override void Write(string value)
        {
            _consoleOut.Write(value);
            _fileOut.Write(value);
        }

        public override void Flush()
        {
            _consoleOut.Flush();
            _fileOut.Flush();
        }

        public override void Close()
        {
            Flush();
            _fileOut.Close();
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Flush();
                _fileOut?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
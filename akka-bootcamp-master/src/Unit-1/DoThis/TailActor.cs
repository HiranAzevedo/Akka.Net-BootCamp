using Akka.Actor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinTail
{
    /// <summary>
    /// Monitors the file at <see cref="_filePath"/> for changes and sends file updates to console.
    /// </summary>
    public class TailActor : UntypedActor
    {
        #region Message Types

        public class FileWrite
        {
            /// <summary>
            /// Signal that the file has changed, and we need to 
            /// read the next line of the file.
            /// </summary>
            public FileWrite(string fileName)
            {
                FileName = fileName;
            }

            public string FileName { get; set; }
        }

        /// <summary>
        /// Signal that the OS had an error accessing the file.
        /// </summary>
        public class FileError
        {
            public FileError(string fileName, string reason)
            {
                FileName = fileName;
                Reason = reason;
            }

            public string FileName { get; private set; }

            public string Reason { get; private set; }
        }

        public class InitalRead
        {
            public InitalRead(string fileName, string text)
            {
                FileName = fileName;
                Text = text;
            }

            public string FileName { get; set; }
            public string Text { get; set; }
        }

        #endregion


        private readonly string _filePath;
        private readonly IActorRef _reporterActor;
        private readonly FileObserver _observer;
        private readonly Stream _fileStream;
        private readonly StreamReader _fileStreamReader;

        public TailActor(IActorRef reporterActor, string filePath)
        {
            _reporterActor = reporterActor;
            _filePath = filePath;

            _observer = new FileObserver(Self, Path.GetFullPath(_filePath));
            _observer.Start();

            _fileStream = new FileStream(Path.GetFullPath(_filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _fileStreamReader = new StreamReader(_fileStream, Encoding.UTF8);


            var text = _fileStreamReader.ReadToEnd();
            Self.Tell(new InitalRead(_filePath, text));
        }

        protected override void OnReceive(object message)
        {
            if (message is FileWrite)
            {
                var text = _fileStreamReader.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _reporterActor.Tell(text);
                }
            }
            else if (message is FileError)
            {
                var fe = message as FileError;
                _reporterActor.Tell($"Tail error:{fe.Reason}");
            }
            else if (message is InitalRead)
            {
                var ir = message as InitalRead;
                _reporterActor.Tell(ir.Text);
            }
        }
    }
}

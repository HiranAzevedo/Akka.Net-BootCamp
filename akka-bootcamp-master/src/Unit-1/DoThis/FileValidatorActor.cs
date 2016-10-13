using Akka.Actor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinTail
{
    public class FileValidatorActor : UntypedActor
    {

        private readonly IActorRef _consoleWriterActor;
        private readonly IActorRef _tailCoordinatorActor;

        public FileValidatorActor(IActorRef consoleWriterActor, IActorRef tailCoordinatorActor)
        {
            _consoleWriterActor = consoleWriterActor;
            _tailCoordinatorActor = tailCoordinatorActor;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as string;
            if (string.IsNullOrWhiteSpace(msg))
            {
                _consoleWriterActor.Tell(new NullInputError("input was blank. Please try again. \n"));

                Sender.Tell(new ContinueProcessing());
            }
            else
            {
                var valid = IsFileUri(msg);
                if (valid)
                {
                    _consoleWriterActor.Tell(new InputSuccess($"starting processing for {msg}"));

                    // start coordinator
                    _tailCoordinatorActor.Tell(new TailCoordinatorActor.StartTail(msg, _consoleWriterActor));
                }
                else
                {
                    _consoleWriterActor.Tell(new ValidationError($"{msg} is not a existing Uri on disk"));

                    Sender.Tell(new ContinueProcessing());
                }
            }
        }

        private static bool IsFileUri(string path)
        {
            return File.Exists(path);
        }
    }
}

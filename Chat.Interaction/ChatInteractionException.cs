using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Chat.Common;
using Chat.Interaction.Xml;

namespace Chat.Interaction
{
    [Serializable]
    public class ChatInteractionException : Exception, IErrorInfoCarrier
    {
        public ErrorInfoType Info { get; private set; }

        public ChatInteractionException(ErrorInfoType info) { this.Setup(info); }
        public ChatInteractionException(ErrorInfoType info, string message) : base(message) { this.Setup(info); }
        public ChatInteractionException(ErrorInfoType info, string message, Exception inner) : base(message, inner) { this.Setup(info); }
        protected ChatInteractionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
            this.Info = (ErrorInfoType)info.GetValue("Info", typeof(ErrorInfoType));
        }

        private void Setup(ErrorInfoType info)
        {
            this.Info = info;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Info", this.Info);
            base.GetObjectData(info, context);
        }

        string IErrorInfoCarrier.FormatErrorInfo()
        {
            return this.Info.FormatErrorInfo();
        }
    }
}

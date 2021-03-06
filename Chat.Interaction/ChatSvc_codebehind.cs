
using System;

namespace Chat.Interaction.Xml
{
    public interface IErrorInfoContainer
    {
        ErrorInfoType Item { get; }
    }

    public partial class ServerErrorInfoType : IErrorInfoContainer
    {

    }

    public partial class ClientErrorInfoType : IErrorInfoContainer
    {

    }

    public interface IEnvelop<TContent>
    {
        TContent Item { get; set; }
        long Id { get; set; }
    }

    public partial class ServerEnvelopType : IEnvelop<ServerEnvelopContentType>
    {

    }

    public partial class ClientEnvelopType : IEnvelop<ClientEnvelopContentType>
    {

    }
}


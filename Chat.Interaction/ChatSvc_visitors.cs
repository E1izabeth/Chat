
using System;

namespace Chat.Interaction.Xml
{
    
    
  public interface IServerEnvelopContentTypeVisitor<T>
  {
    
    T VisitPingResponseType(PingResponseType node);
      
    T VisitUserProfileInfoType(UserProfileInfoType node);
      
    T VisitServerErrorInfoType(ServerErrorInfoType node);
      
    T VisitChatMessageInfoType(ChatMessageInfoType node);
      
    T VisitChatVoiceMessageDataType(ChatVoiceMessageDataType node);
      
    T VisitOkType(OkType node);
      
  }

  abstract partial class ServerEnvelopContentType
  {
    public T Apply<T>(IServerEnvelopContentTypeVisitor<T> visitor)
    {
      return this.ApplyImpl<T>(visitor);
    }
      
    protected abstract T ApplyImpl<T>(IServerEnvelopContentTypeVisitor<T> visitor);
  }

    
  partial class PingResponseType
  {
    protected override T ApplyImpl<T>(IServerEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitPingResponseType(this);
    }
  }
      
  partial class UserProfileInfoType
  {
    protected override T ApplyImpl<T>(IServerEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitUserProfileInfoType(this);
    }
  }
      
  partial class ServerErrorInfoType
  {
    protected override T ApplyImpl<T>(IServerEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitServerErrorInfoType(this);
    }
  }
      
  partial class ChatMessageInfoType
  {
    protected override T ApplyImpl<T>(IServerEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitChatMessageInfoType(this);
    }
  }
      
  partial class ChatVoiceMessageDataType
  {
    protected override T ApplyImpl<T>(IServerEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitChatVoiceMessageDataType(this);
    }
  }
      
  partial class OkType
  {
    protected override T ApplyImpl<T>(IServerEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitOkType(this);
    }
  }
      
    
  public interface IClientEnvelopContentTypeVisitor<T>
  {
    
    T VisitStartChatSpecType(StartChatSpecType node);
      
    T VisitPingRequestType(PingRequestType node);
      
    T VisitClientErrorInfoType(ClientErrorInfoType node);
      
    T VisitPostMessageSpecType(PostMessageSpecType node);
      
    T VisitPostVoiceMessageSpecType(PostVoiceMessageSpecType node);
      
    T VisitRegisterSpecType(RegisterSpecType node);
      
    T VisitRequestActivationSpecType(RequestActivationSpecType node);
      
    T VisitActivateSpecType(ActivateSpecType node);
      
    T VisitChangePasswordSpecType(ChangePasswordSpecType node);
      
    T VisitChangeEmailSpecType(ChangeEmailSpecType node);
      
    T VisitResetPasswordSpecType(ResetPasswordSpecType node);
      
    T VisitLoginSpecType(LoginSpecType node);
      
  }

  abstract partial class ClientEnvelopContentType
  {
    public T Apply<T>(IClientEnvelopContentTypeVisitor<T> visitor)
    {
      return this.ApplyImpl<T>(visitor);
    }
      
    protected abstract T ApplyImpl<T>(IClientEnvelopContentTypeVisitor<T> visitor);
  }

    
  partial class StartChatSpecType
  {
    protected override T ApplyImpl<T>(IClientEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitStartChatSpecType(this);
    }
  }
      
  partial class PingRequestType
  {
    protected override T ApplyImpl<T>(IClientEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitPingRequestType(this);
    }
  }
      
  partial class ClientErrorInfoType
  {
    protected override T ApplyImpl<T>(IClientEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitClientErrorInfoType(this);
    }
  }
      
  partial class PostMessageSpecType
  {
    protected override T ApplyImpl<T>(IClientEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitPostMessageSpecType(this);
    }
  }
      
  partial class PostVoiceMessageSpecType
  {
    protected override T ApplyImpl<T>(IClientEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitPostVoiceMessageSpecType(this);
    }
  }
      
  partial class RegisterSpecType
  {
    protected override T ApplyImpl<T>(IClientEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitRegisterSpecType(this);
    }
  }
      
  partial class RequestActivationSpecType
  {
    protected override T ApplyImpl<T>(IClientEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitRequestActivationSpecType(this);
    }
  }
      
  partial class ActivateSpecType
  {
    protected override T ApplyImpl<T>(IClientEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitActivateSpecType(this);
    }
  }
      
  partial class ChangePasswordSpecType
  {
    protected override T ApplyImpl<T>(IClientEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitChangePasswordSpecType(this);
    }
  }
      
  partial class ChangeEmailSpecType
  {
    protected override T ApplyImpl<T>(IClientEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitChangeEmailSpecType(this);
    }
  }
      
  partial class ResetPasswordSpecType
  {
    protected override T ApplyImpl<T>(IClientEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitResetPasswordSpecType(this);
    }
  }
      
  partial class LoginSpecType
  {
    protected override T ApplyImpl<T>(IClientEnvelopContentTypeVisitor<T> visitor)
    {
      return visitor.VisitLoginSpecType(this);
    }
  }
      
  partial class PingRequestType
  {
      
    public DateTime Stamp
    {
        get { return new DateTime(this.StampTicks); }
        set { this.StampTicks = value.Ticks; }
    }
      
  }
  
  partial class PingResponseType
  {
      
    public DateTime RequestStamp
    {
        get { return new DateTime(this.RequestStampTicks); }
        set { this.RequestStampTicks = value.Ticks; }
    }
      
  }
  
  partial class ChatMessageInfoType
  {
      
    public DateTime Stamp
    {
        get { return new DateTime(this.StampTicks); }
        set { this.StampTicks = value.Ticks; }
    }
      
  }
  
  partial class ChatVoiceMessageDataType
  {
      
    public DateTime Stamp
    {
        get { return new DateTime(this.StampTicks); }
        set { this.StampTicks = value.Ticks; }
    }
      
  }
  
}
  
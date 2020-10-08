using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Chat.Common;
using Chat.Interaction.Xml;
using Chat.UI.Common;
using Chat.UI.Util;
using Concentus.Enums;
using Concentus.Oggfile;
using Concentus.Structs;
using NAudio.Wave;

namespace Chat.UI.ViewModel
{
    public interface ISessionContext
    {
        void OnMessage(ChatMessageInfoType node);
        void OnUserStatus(UserProfileInfoType node);
        void OnVoiceMessage(ChatVoiceMessageDataType node);
    }

    public class SessionViewModel : DependencyObject, ISessionContext
    {
        #region ObservableCollection<UserProfileInfoType> Contacts 

        public ObservableCollection<UserProfileInfoType> Contacts
        {
            get { return (ObservableCollection<UserProfileInfoType>)this.GetValue(ContactsProperty); }
            set { this.SetValue(ContactsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Contacts.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContactsProperty =
            DependencyProperty.Register("Contacts", typeof(ObservableCollection<UserProfileInfoType>), typeof(SessionViewModel), new UIPropertyMetadata(default(ObservableCollection<UserProfileInfoType>)));

        #endregion

        #region string MessageToSendText 

        public string MessageToSendText
        {
            get { return (string)this.GetValue(MessageToSendTextProperty); }
            set { this.SetValue(MessageToSendTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MessageToSendText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageToSendTextProperty =
            DependencyProperty.Register("MessageToSendText", typeof(string), typeof(SessionViewModel), new UIPropertyMetadata(default(string)));

        #endregion

        #region bool IsActivationRequired 

        public bool IsActivationRequired
        {
            get { return (bool)this.GetValue(IsActivationRequiredProperty); }
            set { this.SetValue(IsActivationRequiredProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsActivationRequired.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsActivationRequiredProperty =
            DependencyProperty.Register("IsActivationRequired", typeof(bool), typeof(SessionViewModel), new UIPropertyMetadata(default(bool)));

        #endregion

        #region bool IsRecording 

        public bool IsRecording
        {
            get { return (bool)this.GetValue(IsRecordingProperty); }
            set { this.SetValue(IsRecordingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsRecording.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsRecordingProperty =
            DependencyProperty.Register("IsRecording", typeof(bool), typeof(SessionViewModel), new UIPropertyMetadata(default(bool)));

        #endregion

        #region ObservableCollection<DependencyObject> Messages 

        public ObservableCollection<DependencyObject> Messages
        {
            get { return (ObservableCollection<DependencyObject>)this.GetValue(MessagesProperty); }
            set { this.SetValue(MessagesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Messages.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessagesProperty =
            DependencyProperty.Register("Messages", typeof(ObservableCollection<DependencyObject>), typeof(SessionViewModel), new UIPropertyMetadata(default(ObservableCollection<DependencyObject>)));

        #endregion

        public DelegatedCommand SendMessageCommand { get; }
        public DelegatedCommand RequestActivationCommand { get; }
        public DelegatedCommand ActivateCommand { get; }

        public DelegatedCommand RecordMessageCommand { get; }
        public DelegatedCommand StopRecordingMessageCommand { get; }

        private readonly IChatClientSession _session;
        private readonly OggPlayer _player = new OggPlayer();
        private readonly OggRecorder _recorder = new OggRecorder();

        public SessionViewModel(IChatClientSession session, UserProfileInfoType self = null)
        {
            _session = session;

            this.Messages = new ObservableCollection<DependencyObject>();

            _recorder.OnRecordFinished += async data => {
                try
                {
                    this.IsRecording = false;
                    // this.OnVoiceMessage(new ChatVoiceMessageDataType() { AuthorUserInfo = new UserProfileInfoType() { Login = "me", IsOnline = true }, Stamp = DateTime.Now, OggData = data });
                    await _session.SendVoiceMessage(data);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            };

            this.IsActivationRequired = self?.IsActivated == false;

            this.Contacts = new ObservableCollection<UserProfileInfoType>();

            this.SendMessageCommand = new DelegatedCommand(async o => {
                await _session.SendMessage(this.MessageToSendText);
                // this.OnMessage(new ChatMessageInfoType() { AuthorUserInfo = new UserProfileInfoType() { Login = "me", IsOnline = true }, Stamp = DateTime.Now, Text = this.MessageToSendText });
                this.MessageToSendText = string.Empty;
            });
            this.ActivateCommand = new DelegatedCommand(async o => {
                await _session.Activate(this.MessageToSendText);
                this.MessageToSendText = string.Empty;
                this.IsActivationRequired = false;
            });
            this.RequestActivationCommand = new DelegatedCommand(async o => {
                await _session.RequestActivation();
            });

            this.RecordMessageCommand = new DelegatedCommand(async o => {
                this.IsRecording = true;
                _recorder.StartRecording();
            });
            this.StopRecordingMessageCommand = new DelegatedCommand(async o => {
                _recorder.StopRecording();
            });
        }

        public void OnMessage(ChatMessageInfoType msg)
        {
            this.InvokeAction(() => {
                this.Messages.Add(new MessageItem(msg));
            });
            this.OnUserStatus(msg.AuthorUserInfo);
        }

        public void OnUserStatus(UserProfileInfoType node)
        {
            this.InvokeAction(() => {
                var item = this.Contacts.FirstOrDefault(p => p.Login == node.Login);
                if (node.IsOnline)
                {
                    if (item != null)
                    {
                        var index = this.Contacts.IndexOf(item);
                        this.Contacts.RemoveAt(index);
                        this.Contacts.Insert(index, node);
                    }
                    else
                    {
                        this.Contacts.Add(node);
                    }
                }
                else
                {
                    if (item != null)
                        this.Contacts.Remove(item);
                }
            });
        }

        public void OnVoiceMessage(ChatVoiceMessageDataType node)
        {
            this.InvokeAction(() => {
                this.Messages.Add(new VoiceMessageItem(node, _player));
            });
        }
    }

    public class MessageItem : DependencyObject
    {
        public ChatMessageInfoType Data { get; }

        public string Stamp { get; }

        public MessageItem(ChatMessageInfoType data)
        {
            this.Stamp = data.Stamp.ToLocalTime().ToLongTimeString();
            this.Data = data;
        }
    }

    public class VoiceMessageItem : DependencyObject
    {
        #region bool IsPlaying 

        public bool IsPlaying
        {
            get { return (bool)this.GetValue(IsPlayingProperty); }
            set { this.SetValue(IsPlayingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsPlaying.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register("IsPlaying", typeof(bool), typeof(VoiceMessageItem), new UIPropertyMetadata(default(bool)));

        #endregion

        #region bool IsQueued 

        public bool IsQueued
        {
            get { return (bool)this.GetValue(IsQueuedProperty); }
            set { this.SetValue(IsQueuedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsQueued.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsQueuedProperty =
            DependencyProperty.Register("IsQueued", typeof(bool), typeof(VoiceMessageItem), new UIPropertyMetadata(default(bool)));

        #endregion

        #region bool IsFresh 

        public bool IsFresh
        {
            get { return (bool)GetValue(IsFreshProperty); }
            set { SetValue(IsFreshProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsFresh.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsFreshProperty =
            DependencyProperty.Register("IsFresh", typeof(bool), typeof(VoiceMessageItem), new UIPropertyMetadata(default(bool)));

        #endregion

        public ChatVoiceMessageDataType Data { get; }

        public string Duration { get; }
        public string Stamp { get; }

        private readonly OggPlayer _player;

        public DelegatedCommand PlayCommand { get; }

        public VoiceMessageItem(ChatVoiceMessageDataType data, OggPlayer player)
        {
            _player = player;

            this.Data = data;
            this.Stamp = data.Stamp.ToLocalTime().ToLongTimeString();

            var decoder = new OpusDecoder(48000, 1);
            var oggIn = new OpusOggReadStream(decoder, new MemoryStream(data.OggData));
            this.Duration = oggIn.TotalTime.ToString(@"hh\:mm\:ss");
            this.IsFresh = true;
            
            this.PlayCommand = new DelegatedCommand(o => {
                this.IsQueued = true;
                _player.PlayOggAudio(this);
                this.PlayCommand.RaizeCanExecuteChanged();
                return Task.CompletedTask;
            }, checker: o => !this.IsQueued && !this.IsPlaying);
        }

        internal void OnStared()
        {
            this.InvokeAction(() => {
                this.IsFresh = false;
                this.IsPlaying = true;
                this.IsQueued = false;
                this.PlayCommand.RaizeCanExecuteChanged();
            });
        }

        internal void OnStopped()
        {
            this.InvokeAction(() => {
                this.IsQueued = false;
                this.IsPlaying = false;
                this.PlayCommand.RaizeCanExecuteChanged();
            });
        }
    }
}

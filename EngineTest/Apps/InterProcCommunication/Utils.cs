using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO.Pipes;

namespace InterProcCommunication
{
    #region EVENTS
    public class TChangedEventArgs : EventArgs
    {
        public const string TOKEN_RECEIVED_REQUEST = "RECEIVED_REQUEST";
        public const string TOKEN_RECEIVED_ANSWER = "RECEIVED_ANSWER";
        public const string TOKEN_TERMINATED = "TERMINATED";

        public static readonly TChangedEventArgs TERMINATED = new TChangedEventArgs(TOKEN_TERMINATED, null);
        public static TChangedEventArgs TChangedEventArgsRequest(string _data)
        {
            return new TChangedEventArgs(TOKEN_RECEIVED_REQUEST, _data);
        }
        public static TChangedEventArgs TChangedEventArgsAnswer(string _data)
        {
            return new TChangedEventArgs(TOKEN_RECEIVED_ANSWER, _data);
        }

        public string Token { get; private set; }
        public string Data { get; private set; }
        private TChangedEventArgs(string _token, string _data)
        {
            this.Token = _token;
            this.Data = _data;
        }
    }

    public delegate void ThreadChangedEventHandler(object seder, TChangedEventArgs e);

    #endregion

    #region EXTENSIONS

    public static class PipeExtensions
    {
        public static void WaitForConnectionEx(this NamedPipeServerStream stream, ManualResetEvent cancelEvent)
        {
            Exception e = null;
            AutoResetEvent connectEvent = new AutoResetEvent(false);
            stream.BeginWaitForConnection(ar =>
            {
                try
                {
                    stream.EndWaitForConnection(ar);
                }
                catch (Exception er)
                {
                    e = er;
                }
                connectEvent.Set();
            }, null);
            if (WaitHandle.WaitAny(new WaitHandle[] { connectEvent, cancelEvent }) == 1)
                stream.Close();
            if (e != null)
                throw e; // rethrow exception
        }
    }

    #endregion

    #region ENUMS
    public enum CommMessageType
    {
        UNKNOWN = 0,
        OK = 1,         // to be used to confirm that a message was received
        UPDATE = 2,     // to be used by the client or server to indicate that the sent component is to be updated
        EDIT = 3,       // to be used by the client to indicate that a component is being sent for editing
        SYNCH = 4,      // to be used by the client to indicate the sent ID is to be used for selection synchronization
        REF_UPDATE = 5, // to be used by the client to indicate the sent Component has to update only its references to other components
    }

    #endregion

    public static class CommMessageUtils
    {

        #region CommMessageType

        public const string CMT_OK = "OK";
        public const string CMT_UPDATE = "UP";
        public const string CMT_EDIT = "ED";
        public const string CMT_SYNCH = "SS"; // for synchronizing selection
        public const string CMT_REF_UPDATE = "RU";

        public const string STR_ARG_SEPARATOR = "][";

        public static string CommMessageTypeToString(CommMessageType _type)
        {
            switch(_type)
            {
                case CommMessageType.OK:
                    return CommMessageUtils.CMT_OK;
                case CommMessageType.UPDATE:
                    return CommMessageUtils.CMT_UPDATE;
                case CommMessageType.EDIT:
                    return CommMessageUtils.CMT_EDIT;
                case CommMessageType.SYNCH:
                    return CommMessageUtils.CMT_SYNCH;
                case CommMessageType.REF_UPDATE:
                    return CommMessageUtils.CMT_REF_UPDATE;
                default:
                    return string.Empty;
            }
        }

        public static CommMessageType StringToCommMessageType(string _type)
        {
            if (string.IsNullOrEmpty(_type)) return CommMessageType.UNKNOWN;

            switch(_type)
            {
                case CommMessageUtils.CMT_OK:
                    return CommMessageType.OK;
                case CommMessageUtils.CMT_UPDATE:
                    return CommMessageType.UPDATE;
                case CommMessageUtils.CMT_EDIT:
                    return CommMessageType.EDIT;
                case CommMessageUtils.CMT_SYNCH:
                    return CommMessageType.SYNCH;
                case CommMessageUtils.CMT_REF_UPDATE:
                    return CommMessageType.REF_UPDATE;
                default:
                    return CommMessageType.UNKNOWN;
            }
        }

        #endregion

        #region Request Assembly and Parsing

        public static string ComposeMessage(CommMessageType _type, string _message)
        {
            string msg = CommMessageUtils.CommMessageTypeToString(_type);
            msg += _message;
            return msg;
        }

        public static void DecomposeMessage(string _msg, out CommMessageType type, out string message)
        {
            type = CommMessageType.UNKNOWN;
            message = string.Empty;

            if (string.IsNullOrEmpty(_msg)) return;
            if (_msg.Length < CommMessageUtils.CMT_OK.Length) return;

            // get the type
            string msg_head = _msg.Substring(0, CommMessageUtils.CMT_OK.Length);
            type = CommMessageUtils.StringToCommMessageType(msg_head);

            // get the message
            if (_msg.Length > CommMessageUtils.CMT_OK.Length)
            {
                message = _msg.Substring(CommMessageUtils.CMT_OK.Length);
            }
        }

        #endregion
    }
}
